using System.IO;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.FileWatcherService;
using System.Collections.Specialized;
using System.Configuration;

namespace CipherLibrary.Services.FileEncryptionService
{
    public class FileEncryptionService : IFileEncryptionService
    {
        private IEventLoggerService _eventLoggerService;

        private readonly NameValueCollection _allAppSettings = ConfigurationManager.AppSettings;
        private Configuration _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private readonly string _decryptedFilesPath;
        private readonly string _encryptedFilesPath;
        private readonly string _workDirectoryPath;
        private readonly Queue<string> _bigFilesQueue = new Queue<string>();
        private readonly Queue<string> _smallFilesQueue = new Queue<string>();
        public List<string> EncryptedFiles = new List<string>();

        public FileEncryptionService(IEventLoggerService eventLoggerService)
        {
            _eventLoggerService = eventLoggerService;

            if (_allAppSettings["WorkFolder"] == "") throw new ArgumentException("WorkFolder is empty");
            _workDirectoryPath = _allAppSettings["WorkFolder"];
            _encryptedFilesPath = Path.Combine(_workDirectoryPath, "Encrypted");
            _decryptedFilesPath = Path.Combine(_workDirectoryPath, "Decrypted");
        }

        /// <summary>
        /// Asynchronously encrypts the specified source file and writes the encrypted data to a new file.
        /// </summary>
        /// <param name="sourceFile">The file to be encrypted.</param>
        /// <param name="destFile">The file to write the encrypted data to. The ".enc" extension will be appended to this filename.</param>
        /// <param name="password">The password used to generate a key for encryption.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method uses the AES encryption algorithm. The key is derived from the provided password.
        /// A random salt and IV are generated for each encryption operation and are prepended to the encrypted file.
        /// The source file is deleted after encryption.
        /// </remarks>
        public async Task EncryptFileAsync(string sourceFile, string destFile, string password)
        {
            // Add .enc extension to encrypted file
            destFile += ".enc";

            // Generate a derived key from the password
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                byte[] key = deriveBytes.GetBytes(16);

                // Open the original file
                using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                {
                    // Create a new encrypted file
                    using (var destinationStream = new FileStream(destFile, FileMode.Create, FileAccess.Write))
                    {
                        // Write the salt to the beginning of the encrypted file
                        await destinationStream.WriteAsync(salt, 0, salt.Length).ConfigureAwait(true);

                        // Create an instance of Aes
                        using (var aes = Aes.Create())
                        {
                            aes.Key = key;
                            aes.GenerateIV();

                            // Write the IV to the encrypted file
                            await destinationStream.WriteAsync(aes.IV, 0, aes.IV.Length).ConfigureAwait(true);

                            // Create an encryptor from the Aes instance
                            ICryptoTransform encryptor = aes.CreateEncryptor();

                            // Create an encrypting stream
                            using (var cryptoStream = new CryptoStream(destinationStream, encryptor, CryptoStreamMode.Write))
                            {
                                // Copy the contents of the original file to the encrypted file asynchronously
                                await sourceStream.CopyToAsync(cryptoStream).ConfigureAwait(true);
                            }
                        }
                    }
                }
            }
            Console.WriteLine($"Encrypted file {sourceFile}");
            File.Delete(sourceFile);
        }

        public async Task EncryptFilesInQueueAsync(Queue<string> filesQueue, string password)
        {
            while (filesQueue.Count > 0)
            {
                var file = filesQueue.Dequeue();
                var filePath = Path.Combine(_decryptedFilesPath, file);
                var destPath = Path.Combine(_encryptedFilesPath, file + ".enc");
                await EncryptFileAsync(filePath, destPath, password).ConfigureAwait(true);
                EncryptedFiles.Add(file + ".enc");
            }
        }

        public Task EncryptFilesInParallelAsync(Queue<string> filesQueue, string password)
        {
            var size = filesQueue.Count;
            var encryptTasks = new Task[size];
            for (var i = 0; i < size; i++)
            {
                var file = filesQueue.Dequeue();
                var filePath = Path.Combine(_decryptedFilesPath, file);
                var destPath = Path.Combine(_encryptedFilesPath, file + ".enc");
                encryptTasks[i] = EncryptFileAsync(filePath, destPath, password);
                EncryptedFiles.Add(file + ".enc");
            }

            return Task.WhenAll(encryptTasks);
        }
    }
}