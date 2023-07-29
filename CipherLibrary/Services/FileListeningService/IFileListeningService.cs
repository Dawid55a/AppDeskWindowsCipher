using System.Collections.Generic;
using System.IO;

namespace CipherLibrary.Services.FileListeningService
{
    public interface IFileListeningService
    {
        void StartListenOnFolder(string path);
        List<string> GetFiles();
    }
}