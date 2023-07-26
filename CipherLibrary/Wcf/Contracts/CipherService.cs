using CipherWpfApp.Models;
using System.Collections.Generic;
using CipherLibrary.Services.FileEncryptionService;

namespace CipherLibrary.Wcf.Contracts
{
    public class CipherService: ICipherService
    {
        private readonly IFileEncryptionService _fileEncryptionService;

        public CipherService(IFileEncryptionService fileEncryptionService)
        {
            _fileEncryptionService = fileEncryptionService;
        }

        public List<FileEntry> GetEncryptedFiles()
        {
            var fileEntries = new List<FileEntry>
            {
                new FileEntry { Path = "C:\\Path\\To\\File1.txt", Name = "File1.txt", IsEncrypted = true, IsDecrypted = false }
            };

            return  fileEntries;
        }

        public List<FileEntry> GetDecryptedFiles()
        {
            throw new System.NotImplementedException();
        }

        public void EncryptFiles(List<FileEntry> fileEntry)
        {
            throw new System.NotImplementedException();
        }

        public void DecryptFiles(List<FileEntry> fileEntry)
        {
            throw new System.NotImplementedException();
        }
    }
}