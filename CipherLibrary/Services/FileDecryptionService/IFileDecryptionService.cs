using System.Collections.Generic;
using System.Threading.Tasks;

namespace CipherLibrary.Services.FileDecryptionService
{
    public interface IFileDecryptionService
    {
        Task DecryptFileAsync(string sourceFile, string destFile, string password);
        Task DecryptFilesInQueueAsync(Queue<string> filesQueue, string password);
        Task DecryptFilesInParallelAsync(Queue<string> filesQueue, string password);

    }
}