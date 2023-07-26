using System.IO;

namespace CipherLibrary.Services.FileListeningService
{
    public class FileListeningService : IFileListeningService
    {
        private FileSystemWatcher _watcher;

        public FileListeningService()
        {
        }

        public void StartListenOnFolder(string path, FileSystemEventHandler createdEventHandler, FileSystemEventHandler deletedEventHandler)
        {
            _watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _watcher.Created += createdEventHandler;
            _watcher.Deleted += deletedEventHandler;

        }
    }
}