using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CipherLibrary.DTOs;
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.FileDecryptionService;
using CipherLibrary.Services.FileEncryptionService;
using CipherLibrary.Services.FileListeningService;
using CipherLibrary.Services.PasswordService;
using CipherLibrary.Services.SecureConfigManager;
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
        private readonly ISecureConfigManager _secureConfig;
        private readonly IEventLoggerService _eventLoggerService;

        private readonly Queue<string> _bigFilesToEncrypt = new Queue<string>();
        private readonly Queue<string> _bigFilesToDecrypt = new Queue<string>();
        private readonly Queue<string> _smallFilesToEncrypt = new Queue<string>();
        private readonly Queue<string> _smallFilesToDecrypt = new Queue<string>();

        private string _encryptedFilesPath;
        private string _decryptedFilesPath;

        public FileCryptorService(
            IFileEncryptionService fileEncryptionService,
            IFileDecryptionService fileDecryptionService,
            IEventLoggerService eventLoggerService,
            IFileListeningService fileEncryptListeningService,
            IFileListeningService fileDecryptListeningService,
            IPasswordService passwordService,
            ISecureConfigManager secureConfig
        )
        {
            _fileEncryptionService = fileEncryptionService;
            _fileDecryptionService = fileDecryptionService;
            _eventLoggerService = eventLoggerService;
            _fileEncryptListeningService = fileEncryptListeningService;
            _fileDecryptListeningService = fileDecryptListeningService;
            _passwordService = passwordService;
            _secureConfig = secureConfig;

            _encryptedFilesPath = _secureConfig.GetSetting(AppConfigKeys.WorkFolder) + "\\EncryptedFiles";
            _decryptedFilesPath = _secureConfig.GetSetting(AppConfigKeys.WorkFolder) + "\\DecryptedFiles";

            Setup();
        }

        public void Setup()
        {
            _eventLoggerService.WriteInfo("FileCryptorService setup");
            Console.WriteLine("FileCryptorService setup");

            if (string.IsNullOrEmpty(_secureConfig.GetSetting(AppConfigKeys.PublicKey)) ||
                string.IsNullOrEmpty(_secureConfig.GetSetting(AppConfigKeys.PrivateKey)))
            {
                // Create encryption key
                Console.WriteLine("Start creating key");
                using (var rsa = new RSACryptoServiceProvider(4096))
                {
                    _secureConfig.SaveSetting(AppConfigKeys.PublicKey, rsa.ToXmlString(false));
                    _secureConfig.SaveSetting(AppConfigKeys.PrivateKey, rsa.ToXmlString(true));
                }

                Console.WriteLine("End creating key");
                _eventLoggerService.WriteDebug($"Klucz publiczny {_secureConfig.GetSetting(AppConfigKeys.PublicKey)}");
                _eventLoggerService.WriteDebug($"Klucz prywatny {_secureConfig.GetSetting(AppConfigKeys.PrivateKey)}");
            }

            if (string.IsNullOrEmpty(_secureConfig.GetSetting(AppConfigKeys.WorkFolder)))
            {
                _secureConfig.SaveSetting(AppConfigKeys.WorkFolder,
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
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
            _secureConfig.SaveSetting(AppConfigKeys.WorkFolder, workingDirectory);
            _encryptedFilesPath = workingDirectory + "\\EncryptedFiles";
            _decryptedFilesPath = workingDirectory + "\\DecryptedFiles";

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
            var string2 = _secureConfig.GetSetting(AppConfigKeys.PublicKey);
            return string2;
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

        public Task EncryptFilesAsync(List<FileEntry> fileEntries, byte[] password)
        {
            foreach (var fileEntry in fileEntries)
            {
                _eventLoggerService.WriteDebug($"EncryptFiles started on {fileEntry.Name} {DateTime.Now}");
                Console.WriteLine($"EncryptFiles started on {fileEntry.Name} {DateTime.Now}");

                if (!File.Exists(fileEntry.Path)) throw new FileNotFoundException();
                var info = new FileInfo(fileEntry.Path);

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

            var tasks = new Task[2];

            tasks[0] = _fileEncryptionService.EncryptFilesInQueueAsync(_smallFilesToEncrypt, plainPassword);
            tasks[1] = _fileEncryptionService.EncryptFilesInParallelAsync(_bigFilesToEncrypt, plainPassword);

            return Task.WhenAll(tasks);
        }

        public void DecryptFiles(List<FileEntry> fileEntries, byte[] password)
        {
            foreach (var fileEntry in fileEntries)
            {
                _eventLoggerService.WriteDebug($"DecryptFiles started on {fileEntry.Name} {DateTime.Now}");
                Console.WriteLine($"DecryptFiles started on {fileEntry.Name} {DateTime.Now}");

                if (!File.Exists(fileEntry.Path)) throw new FileNotFoundException();
                var info = new FileInfo(fileEntry.Path);

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