using System;
using System.Collections.Generic;
using System.Diagnostics;
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.FileCryptorService;

namespace CipherLibrary.Wcf.Contracts
{
    public class CipherService: ICipherService
    {
        private readonly IFileCryptorService _fileCryptorService;
        private readonly IEventLoggerService _eventLoggerService;

        public CipherService(IFileCryptorService fileCryptorService, IEventLoggerService eventLoggerService)
        {
            _fileCryptorService = fileCryptorService;
            _eventLoggerService = eventLoggerService;
        }

        public List<FileEntry> GetEncryptedFiles()
        {
            _eventLoggerService.WriteDebug("Pobrano zaszyfrowane pliki");
            return _fileCryptorService.GetEncryptedFiles();
        }

        public List<FileEntry> GetDecryptedFiles()
        {
            _eventLoggerService.WriteDebug("Pobrano odszyfrowane pliki");
            return _fileCryptorService.GetDecryptedFiles();
        }

        public void EncryptFiles(List<FileEntry> fileEntry, byte[] password)
        {
            _fileCryptorService.EncryptFiles(fileEntry, password);
            _eventLoggerService.WriteDebug("Zaszyfrowano pliki");
        }

        public void DecryptFiles(List<FileEntry> fileEntry, byte[] password)
        {
            _fileCryptorService.DecryptFiles(fileEntry, password);
            _eventLoggerService.WriteDebug("Odszyfrowano pliki");
        }

        public void SetTraceLevel(TraceLevel level)
        {
            _eventLoggerService.WriteDebug($"Ustawiono poziom logowania na {level}");
            _fileCryptorService.SetTraceLevel(level);
        }

        public string GetPublicKey()
        {
            return _fileCryptorService.GetPublicKey();
        }

        public void ChangeOperationDirectory(string directoryPath)
        {
            _eventLoggerService.WriteInfo($"Zmieniono głowny katalog na {directoryPath}");
            _fileCryptorService.SetWorkingDirectory(directoryPath);
        }
    }
}