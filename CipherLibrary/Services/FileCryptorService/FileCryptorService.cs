using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CipherLibrary.DTOs;
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.FileDecryptionService;
using CipherLibrary.Services.FileEncryptionService;
using CipherLibrary.Services.FileListeningService;
using CipherLibrary.Services.PasswordService;
using CipherLibrary.Wcf.Contracts;

namespace CipherLibrary.Services.FileCryptorService
{
    public class FileCryptorService : IFileCryptorService
    {
        private readonly IFileEncryptionService _fileEncryptionService;
        private readonly IFileDecryptionService _fileDecryptionService;
        private readonly IFileListeningService _fileEncryptListeningService;
        private readonly IFileListeningService _fileDecryptListeningService;
        private readonly IPasswordService _passwordService;
        private readonly IEventLoggerService _eventLoggerService;

        private readonly NameValueCollection _allAppSettings = ConfigurationManager.AppSettings;
        private readonly Configuration _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        private readonly Queue<string> _bigFilesToEncrypt = new Queue<string>();
        private readonly Queue<string> _bigFilesToDecrypt = new Queue<string>();
        private readonly Queue<string> _smallFilesToEncrypt = new Queue<string>();
        private readonly Queue<string> _smallFilesToDecrypt = new Queue<string>();

        private string _encryptedFilesPath;
        private string _decryptedFilesPath;
        private byte[] _password;

        public FileCryptorService(
            IFileEncryptionService fileEncryptionService,
            IFileDecryptionService fileDecryptionService,
            IEventLoggerService eventLoggerService,
            IFileListeningService fileEncryptListeningService,
            IFileListeningService fileDecryptListeningService,
            IPasswordService passwordService
        )
        {
            _fileEncryptionService = fileEncryptionService;
            _fileDecryptionService = fileDecryptionService;
            _eventLoggerService = eventLoggerService;
            _fileEncryptListeningService = fileEncryptListeningService;
            _fileDecryptListeningService = fileDecryptListeningService;
            _passwordService = passwordService;

            _encryptedFilesPath = _allAppSettings[AppConfigKeys.WorkFolder] + "\\EncryptedFiles";
            _decryptedFilesPath = _allAppSettings[AppConfigKeys.WorkFolder] + "\\DecryptedFiles";

            Setup();
        }

        public void Setup()
        {
            _eventLoggerService.WriteInfo("FileCryptorService setup");
            Console.WriteLine("FileCryptorService setup");

            if (string.IsNullOrEmpty(_allAppSettings[AppConfigKeys.PublicKey]) ||
                string.IsNullOrEmpty(_allAppSettings[AppConfigKeys.PrivateKey]))
            {
                // Create encryption key
                Console.WriteLine("Start creating key");
                using (var rsa = new RSACryptoServiceProvider(4096))
                {
                    _allAppSettings.Set(AppConfigKeys.PublicKey, rsa.ToXmlString(false));
                    _allAppSettings.Set(AppConfigKeys.PrivateKey, rsa.ToXmlString(true));
                }
                Console.WriteLine("End creating key");

                _config.Save(ConfigurationSaveMode.Modified);
            }

            if (string.IsNullOrEmpty(_allAppSettings[AppConfigKeys.WorkFolder]))
            {
                _allAppSettings.Set(AppConfigKeys.WorkFolder, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                _config.Save(ConfigurationSaveMode.Modified);
            }

            // Create folders if not exist
            if (!Directory.Exists(_encryptedFilesPath))
            {
                Directory.CreateDirectory(_encryptedFilesPath);
            }

            if (!Directory.Exists(_decryptedFilesPath))
            {
                Directory.CreateDirectory(_decryptedFilesPath);
            }

            _fileEncryptListeningService.StartListenOnFolder(_encryptedFilesPath);
            _fileDecryptListeningService.StartListenOnFolder(_decryptedFilesPath);
        }

        public void SetWorkingDirectory(string workingDirectory)
        {
            _allAppSettings.Set(AppConfigKeys.WorkFolder, workingDirectory);
            _encryptedFilesPath = workingDirectory + "\\EncryptedFiles";
            _decryptedFilesPath = workingDirectory + "\\DecryptedFiles";
            _config.Save(ConfigurationSaveMode.Modified);

            if (!Directory.Exists(_encryptedFilesPath))
            {
                Directory.CreateDirectory(_encryptedFilesPath);
            }

            if (!Directory.Exists(_decryptedFilesPath))
            {
                Directory.CreateDirectory(_decryptedFilesPath);
            }

            _fileEncryptListeningService.StartListenOnFolder(_encryptedFilesPath);
            _fileDecryptListeningService.StartListenOnFolder(_decryptedFilesPath);
        }

        public List<FileEntry> GetEncryptedFiles()
        {
            var files = _fileEncryptListeningService.GetFiles();
            var fileEntries = files.ConvertAll(x => new FileEntry
            {
                Name = Path.GetFileName(x),
                Path = x,
                IsEncrypted = true,
                IsDecrypted = false
            });

            return fileEntries;
        }

        public void SetTraceLevel(TraceLevel level)
        {
            _eventLoggerService.SetTraceLevel(level);
        }

        public string GetPublicKey()
        {
            return _allAppSettings.Get(AppConfigKeys.PublicKey);
        }

        public List<FileEntry> GetDecryptedFiles()
        {
            var files = _fileDecryptListeningService.GetFiles();

            // write to console each file
            foreach (var file in files)
            {
                Console.WriteLine(file);
            }

            var fileEntries = files.ConvertAll(x => new FileEntry
            {
                Name = Path.GetFileName(x),
                Path = x,
                IsEncrypted = false,
                IsDecrypted = true
            });

            return fileEntries;
        }

        public void EncryptFiles(List<FileEntry> fileEntries, byte[] password)
        {
            foreach (var fileEntry in fileEntries)
            {
                _eventLoggerService.WriteDebug($"EncryptFiles started on {fileEntry.Name} {DateTime.Now}");
                Console.WriteLine($"EncryptFiles started on {fileEntry.Name} {DateTime.Now}");
                var path = Path.Combine(_encryptedFilesPath, fileEntry.Path);
                if (!File.Exists(path)) throw new FileNotFoundException();
                var info = new FileInfo(path);

                if (fileEntry.IsEncrypted)
                {
                    _eventLoggerService.WriteWarning("File " + fileEntry.Name + " is already encrypted");
                    Console.WriteLine("File " + fileEntry.Name + " is already encrypted");
                    continue;
                }

                // If size of file is above 100MB
                if (info.Length >= 100 * 1024 * 1024)
                {
                    _bigFilesToEncrypt.Enqueue(fileEntry.Path);
                }
                else
                {
                    _smallFilesToEncrypt.Enqueue(fileEntry.Path);
                }
            }

            var plainPassword = _passwordService.DecryptPassword(password);

            _fileEncryptionService.EncryptFilesInQueueAsync(_smallFilesToEncrypt, plainPassword);
            _fileEncryptionService.EncryptFilesInParallelAsync(_bigFilesToEncrypt, plainPassword);
        }

        public void DecryptFiles(List<FileEntry> fileEntries, byte[] password)
        {
            foreach (var fileEntry in fileEntries)
            {
                _eventLoggerService.WriteDebug($"DecryptFiles started on {fileEntry.Name} {DateTime.Now}");
                Console.WriteLine($"DecryptFiles started on {fileEntry.Name} {DateTime.Now}");

                var path = Path.Combine(_decryptedFilesPath, fileEntry.Path);
                if (!File.Exists(path)) throw new FileNotFoundException();
                var info = new FileInfo(path);

                if (fileEntry.IsDecrypted)
                {
                    _eventLoggerService.WriteWarning("File " + fileEntry.Name + " is already decrypted");
                    Console.WriteLine("File " + fileEntry.Name + " is already decrypted");
                    continue;
                }

                // If size of file is above 100MB
                if (info.Length >= 100 * 1024 * 1024)
                {
                    _bigFilesToDecrypt.Enqueue(fileEntry.Path);
                }
                else
                {
                    _smallFilesToDecrypt.Enqueue(fileEntry.Path);
                }
            }

            var plainPassword = _passwordService.DecryptPassword(password);

            _fileDecryptionService.DecryptFilesInQueueAsync(_smallFilesToDecrypt, plainPassword);
            _fileDecryptionService.DecryptFilesInParallelAsync(_bigFilesToDecrypt, plainPassword);
        }
    }
}