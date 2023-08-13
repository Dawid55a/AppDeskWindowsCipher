using System;
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

            _files.Clear();

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
            Console.WriteLine(path);
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("File created: {0}", e.FullPath);
            _files.Add(e.FullPath);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("File deleted: {0}", e.FullPath);
            _files.Remove(e.FullPath);
        }

        public List<string> GetFiles()
        {
            Console.WriteLine("GetFiles");
            // write to console each file
            foreach (var file in _files)
            {
                Console.WriteLine(file);
            }
            return new List<string>(_files);
        }
    }
}