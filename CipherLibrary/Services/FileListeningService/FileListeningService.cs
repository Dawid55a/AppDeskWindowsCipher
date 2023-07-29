using System.IO;
using System.Collections.Generic;

namespace CipherLibrary.Services.FileListeningService
{
    public class FileListeningService : IFileListeningService
    {
        private FileSystemWatcher _watcher;
        private readonly List<string> _files;

        public FileListeningService()
        {
            _files = new List<string>();
        }

        public void StartListenOnFolder(string path)
        {
            _watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnCreated;
            _watcher.Deleted += OnDeleted;

            // Initialize the list with the current files in the directory
            _files.AddRange(Directory.GetFiles(path));
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            _files.Add(e.FullPath);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            _files.Remove(e.FullPath);
        }

        public List<string> GetFiles()
        {
            return new List<string>(_files);
        }
    }
}