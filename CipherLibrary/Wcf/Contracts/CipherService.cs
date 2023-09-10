using System;
using System.Collections.Generic;
using System.Diagnostics;
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.FileCryptorService;
using CipherLibrary.Services.PasswordService;

namespace CipherLibrary.Wcf.Contracts
{
    public class CipherService: ICipherService
    {
        private readonly IFileCryptorService _fileCryptorService;
        private readonly IEventLoggerService _eventLoggerService;
        private readonly IPasswordService _passwordService;

        public CipherService(IFileCryptorService fileCryptorService, IEventLoggerService eventLoggerService, IPasswordService passwordService)
        {
            _fileCryptorService = fileCryptorService;
            _eventLoggerService = eventLoggerService;
            _passwordService = passwordService;
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
            _fileCryptorService.EncryptFilesAsync(fileEntry, password);
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

        public bool CheckPassword(byte[] password)
        {
            _eventLoggerService.WriteDebug("Sprawdzanie hasła podanego przez użytkownika");
            return _passwordService.IsPasswordCorrect(password);
        }

        public void SetPassword(byte[] password)
        {
            _eventLoggerService.WriteDebug("Ustawianie hasła");
            _passwordService.SetPassword(password);
        }
    }
}