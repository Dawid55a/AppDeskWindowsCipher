using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.FileDecryptionService;
using CipherLibrary.Services.FileEncryptionService;
using CipherLibrary.Services.FileListeningService;

namespace CipherLibrary.Services.FileCryptorService
{
    public class FileCryptorService : IFileCryptorService
    {
        private readonly IFileEncryptionService _fileEncryptionService;
        private readonly IFileDecryptionService _fileDecryptionService;
        private readonly IFileListeningService _fileListeningService;
        private readonly IEventLoggerService _eventLoggerService;

        private readonly NameValueCollection _allAppSettings = ConfigurationManager.AppSettings;
        private readonly Configuration _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        public FileCryptorService(IFileEncryptionService fileEncryptionService, IFileDecryptionService fileDecryptionService, IEventLoggerService eventLoggerService, IFileListeningService fileListeningService)
        {
            _fileEncryptionService = fileEncryptionService;
            _fileDecryptionService = fileDecryptionService;
            _eventLoggerService = eventLoggerService;
            _fileListeningService = fileListeningService;
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
                _fileListeningService.StartListenOnFolder(_allAppSettings["WorkFolder"], OnCreated, OnDeleted);
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine(e.Name);
            //DecryptedFiles.Add(e.Name);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine(e.Name);
        }
    }
}