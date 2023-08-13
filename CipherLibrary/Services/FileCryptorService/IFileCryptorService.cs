using CipherLibrary.Wcf.Contracts;
using System.Collections.Generic;
using System.Diagnostics;

namespace CipherLibrary.Services.FileCryptorService
{
    public interface IFileCryptorService
    {
        void Setup();
        void SetPassword(byte[] encryptedPassword);
        void EncryptFiles(List<FileEntry> fileEntries, byte[] password);
        void DecryptFiles(List<FileEntry> fileEntries, byte[] password);
        List<FileEntry> GetDecryptedFiles();
        List<FileEntry> GetEncryptedFiles();
        void SetTraceLevel(TraceLevel level);
        string GetPublicKey();
        void SetWorkingDirectory(string workingDirectory);
    }
}