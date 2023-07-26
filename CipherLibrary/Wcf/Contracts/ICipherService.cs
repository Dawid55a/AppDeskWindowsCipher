using System.Collections.Generic;
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
        void EncryptFiles(List<FileEntry> fileEntry);
        [OperationContract]
        void DecryptFiles(List<FileEntry> fileEntry);
    }
}