using System.Threading.Tasks;

namespace CipherLibrary.Services.FileEncryptionService
{
    public interface IFileEncryptionService
    {
        Task EncryptFileAsync(string sourceFile, string destFile, string password);
        void EncryptFiles(string[] files, string password);
        Task EncryptSmallFilesAsync(string password);
        Task EncryptLargeFilesAsync(string password);
    }
}