using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.FileDecryptionService;
using CipherLibrary.Services.FileEncryptionService;
using CipherLibrary.Services.FileListeningService;
using CipherLibrary.Wcf.Contracts;

namespace CipherLibrary.Services.FileCryptorService
{
    public class FileCryptorService : IFileCryptorService
    {
        private readonly IFileEncryptionService _fileEncryptionService;
        private readonly IFileDecryptionService _fileDecryptionService;
        private readonly IFileListeningService _fileEncryptListeningService;
        private readonly IFileListeningService _fileDecryptListeningService;
        private readonly IEventLoggerService _eventLoggerService;

        private readonly NameValueCollection _allAppSettings = ConfigurationManager.AppSettings;
        private readonly Configuration _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        private readonly Queue<string> _bigFilesToEncrypt = new Queue<string>();
        private readonly Queue<string> _bigFilesToDecrypt = new Queue<string>();
        private readonly Queue<string> _smallFilesToEncrypt = new Queue<string>();
        private readonly Queue<string> _smallFilesToDecrypt = new Queue<string>();

        private readonly string _encryptedFilesPath;
        private readonly string _decryptedFilesPath;
        private byte[] _password;

        public FileCryptorService(IFileEncryptionService fileEncryptionService,
            IFileDecryptionService fileDecryptionService, IEventLoggerService eventLoggerService,
            IFileListeningService fileEncryptListeningService, IFileListeningService fileDecryptListeningService)
        {
            _fileEncryptionService = fileEncryptionService;
            _fileDecryptionService = fileDecryptionService;
            _eventLoggerService = eventLoggerService;
            _fileEncryptListeningService = fileEncryptListeningService;
            _fileDecryptListeningService = fileDecryptListeningService;

            _encryptedFilesPath = _allAppSettings["WorkFolder"] + "\\EncryptedFiles";
            _decryptedFilesPath = _allAppSettings["WorkFolder"] + "\\DecryptedFiles";
        }

        public void Setup()
        {
            _eventLoggerService.WriteInfo("FileCryptorService setup");
            Console.WriteLine("FileCryptorService setup");

            if (_allAppSettings["PublicKey"] == "" || _allAppSettings["PrivateKey"] == "")
            {
                // Create encryption key
                using (var rsa = new RSACryptoServiceProvider(4096))
                {
                    _allAppSettings.Set("PublicKey", rsa.ToXmlString(false));
                    _allAppSettings.Set("PrivateKey", rsa.ToXmlString(true));
                }

                _config.Save(ConfigurationSaveMode.Modified);
            }

            if (_allAppSettings["WorkFolder"] != "")
            {
                _fileEncryptListeningService.StartListenOnFolder(_encryptedFilesPath);
                _fileDecryptListeningService.StartListenOnFolder(_decryptedFilesPath);
            }
        }

        public void SetPassword(byte[] encryptedPassword)
        {
            _password = encryptedPassword;
        }

        private string DecryptPassword(byte[] password)
        {
            byte[] decryptedPassword;
            using (var rsaPublicOnly = new RSACryptoServiceProvider())
            {
                rsaPublicOnly.FromXmlString(_allAppSettings["PrivateKey"]);
                decryptedPassword = rsaPublicOnly.Decrypt(password, true);
            }

            return Encoding.Default.GetString(decryptedPassword);
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
            return _allAppSettings.Get("PublicKey");
        }

        public List<FileEntry> GetDecryptedFiles()
        {
            var files = _fileDecryptListeningService.GetFiles();
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
            _eventLoggerService.WriteDebug($"EncryptFiles started {DateTime.Now}");
            Console.WriteLine("EncryptFiles started");

            foreach (var fileEntry in fileEntries)
            {
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
                    _bigFilesToEncrypt.Enqueue(fileEntry.Name);
                }
                else
                {
                    _smallFilesToEncrypt.Enqueue(fileEntry.Name);
                }
            }

            var plainPassword = DecryptPassword(password);

            _fileEncryptionService.EncryptFilesInQueueAsync(_smallFilesToEncrypt, plainPassword);
            _fileEncryptionService.EncryptFilesInParallelAsync(_bigFilesToEncrypt, plainPassword);
        }

        public void DecryptFiles(List<FileEntry> fileEntries, byte[] password)
        {
            _eventLoggerService.WriteDebug($"DecryptFiles started {DateTime.Now}");
            Console.WriteLine("DecryptFiles started");

            foreach (var fileEntry in fileEntries)
            {
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
                    _bigFilesToDecrypt.Enqueue(fileEntry.Name);
                }
                else
                {
                    _smallFilesToDecrypt.Enqueue(fileEntry.Name);
                }
            }

            var plainPassword = DecryptPassword(password);

            _fileDecryptionService.DecryptFilesInQueueAsync(_smallFilesToDecrypt, plainPassword);
            _fileDecryptionService.DecryptFilesInParallelAsync(_bigFilesToDecrypt, plainPassword);
        }

    }
}