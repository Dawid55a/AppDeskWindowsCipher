using System.IO;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CipherLibrary.Services.EventLoggerService;

namespace CipherLibrary.Services.FileEncryptionService
{
    public class FileEncryptionService : IFileEncryptionService
    {
        private readonly IEventLoggerService _eventLoggerService;

        public FileEncryptionService(IEventLoggerService eventLoggerService)
        {
            _eventLoggerService = eventLoggerService;
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

            try
            {
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
                                using (var cryptoStream =
                                       new CryptoStream(destinationStream, encryptor, CryptoStreamMode.Write))
                                {
                                    // Copy the contents of the original file to the encrypted file asynchronously
                                    await sourceStream.CopyToAsync(cryptoStream).ConfigureAwait(true);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _eventLoggerService.WriteError(e.Message);
                throw;
            }

            _eventLoggerService.WriteDebug($"Encrypted file {sourceFile}");
            File.Delete(sourceFile);
        }

        public async Task EncryptFilesInQueueAsync(Queue<string> filesQueue, string password)
        {
            while (filesQueue.Count > 0)
            {
                var fullFilePath = filesQueue.Dequeue();

                var fileName = Path.GetFileName(fullFilePath);
                var directoryPath = Path.GetDirectoryName(fullFilePath);
                var rootPath = Directory.GetParent(directoryPath)?.FullName;

                var newDirectoryPath = Path.Combine(rootPath, "EncryptedFiles");
                var destPath = Path.Combine(newDirectoryPath, fileName);
                await EncryptFileAsync(fullFilePath, destPath, password).ConfigureAwait(true);
            }
        }

        public Task EncryptFilesInParallelAsync(Queue<string> filesQueue, string password)
        {
            var size = filesQueue.Count;
            var encryptTasks = new Task[size];
            for (var i = 0; i < size; i++)
            {
                var fullFilePath = filesQueue.Dequeue();

                var fileName = Path.GetFileName(fullFilePath);
                var directoryPath = Path.GetDirectoryName(fullFilePath);
                var rootPath = Directory.GetParent(directoryPath)?.FullName;

                var newDirectoryPath = Path.Combine(rootPath, "EncryptedFiles");
                var destPath = Path.Combine(newDirectoryPath, fileName);
                encryptTasks[i] = EncryptFileAsync(fullFilePath, destPath, password);
            }

            return Task.WhenAll(encryptTasks);
        }
    }
}