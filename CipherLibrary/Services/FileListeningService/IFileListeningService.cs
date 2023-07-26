using System.IO;

namespace CipherLibrary.Services.FileListeningService
{
    public interface IFileListeningService
    {
        void StartListenOnFolder(string path, FileSystemEventHandler createdEventHandler,
            FileSystemEventHandler deletedEventHandler);
    }
}