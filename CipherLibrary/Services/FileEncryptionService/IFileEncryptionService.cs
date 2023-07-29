using System.Collections.Generic;
using System.Threading.Tasks;

namespace CipherLibrary.Services.FileEncryptionService
{
    public interface IFileEncryptionService
    {
        Task EncryptFileAsync(string sourceFile, string destFile, string password);
        Task EncryptFilesInQueueAsync(Queue<string> filesQueue, string password);
        Task EncryptFilesInParallelAsync(Queue<string> filesQueue, string password);
    }
}