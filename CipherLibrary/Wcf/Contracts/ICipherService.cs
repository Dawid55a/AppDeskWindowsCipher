using System.Collections.Generic;
using System.Diagnostics;
using CipherWpfApp.Models;
using System.ServiceModel;

namespace CipherLibrary.Wcf.Contracts
{
    [ServiceContract]

    public interface ICipherService
    {
        [OperationContract]
        List<FileEntry> GetEncryptedFiles();
        [OperationContract]
        List<FileEntry> GetDecryptedFiles();

        [OperationContract]
        void EncryptFiles(List<FileEntry> fileEntry, byte[] password);

        [OperationContract]
        void DecryptFiles(List<FileEntry> fileEntry, byte[] password);

        [OperationContract]
        void SetTraceLevel(TraceLevel level);

        [OperationContract]
        string GetPublicKey();

        [OperationContract]
        void ChangeOperationDirectory(string directoryPath);

        [OperationContract]
        bool CheckPassword(byte[] password);

        [OperationContract]
        void SetPassword(byte[] password);
    }
}