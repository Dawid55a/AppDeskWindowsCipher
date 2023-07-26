using System.IO;

namespace CipherLibrary.Services.FileWatcherService
{
    public interface IFileWatcherService
    {
        void StartWatching();
        event FileSystemEventHandler FileCreated;
    }
}