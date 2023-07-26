using System.IO;

namespace CipherLibrary.Services.FileWatcherService
{
    public class FileWatcherService : IFileWatcherService
    {
        private readonly FileSystemWatcher _fileSystemWatcher;

        public FileWatcherService(string path)
        {
            _fileSystemWatcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
        }

        public event FileSystemEventHandler FileCreated
        {
            add => _fileSystemWatcher.Created += value;
            remove => _fileSystemWatcher.Created -= value;
        }

        public void StartWatching()
        {
            // logika do rozpoczęcia monitorowania
        }
    }
}