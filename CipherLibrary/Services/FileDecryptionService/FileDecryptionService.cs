using System.IO;
using System.Security.Cryptography;
using System;
using System.Threading.Tasks;
using CipherLibrary.Services.EventLoggerService;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;

namespace CipherLibrary.Services.FileDecryptionService
{
    public class FileDecryptionService : IFileDecryptionService
    {
        private readonly IEventLoggerService _eventLoggerService;


        public FileDecryptionService(IEventLoggerService eventLoggerService)
        {
            _eventLoggerService = eventLoggerService;
        }

        public async Task DecryptFileAsync(string sourceFile, string destFile, string password)
        {
            // Remove .enc extension from encrypted file
            destFile = destFile.Replace(".enc", string.Empty);

            // Read the salt from the beginning of the encrypted file
            byte[] salt = new byte[16];
            try
            {
                using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                {
                    await sourceStream.ReadAsync(salt, 0, salt.Length).ConfigureAwait(true);

                    // Generate a derived key from the password
                    using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000))
                    {
                        byte[] key = deriveBytes.GetBytes(16);

                        // Read the IV from the encrypted file
                        byte[] iv = new byte[16];
                        await sourceStream.ReadAsync(iv, 0, iv.Length).ConfigureAwait(true);

                        // Create a new decrypted file
                        using (var destinationStream = new FileStream(destFile, FileMode.Create, FileAccess.Write))
                        {
                            // Create an instance of Aes
                            using (var aes = Aes.Create())
                            {
                                aes.Key = key;
                                aes.IV = iv;

                                // Create a decryptor from the Aes instance
                                ICryptoTransform decryptor = aes.CreateDecryptor();

                                // Create a decrypting stream
                                using (var cryptoStream =
                                       new CryptoStream(sourceStream, decryptor, CryptoStreamMode.Read))
                                {
                                    // Copy the contents of the encrypted file to the decrypted file asynchronously
                                    await cryptoStream.CopyToAsync(destinationStream).ConfigureAwait(true);
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

            _eventLoggerService.WriteDebug($"Decrypted file {sourceFile}");
            File.Delete(sourceFile);
        }

        public async Task DecryptFilesInQueueAsync(Queue<string> filesQueue, string password)
        {
            while (filesQueue.Count > 0)
            {
                var fullFilePath = filesQueue.Dequeue();

                var fileName = Path.GetFileName(fullFilePath);
                var directoryPath = Path.GetDirectoryName(fullFilePath);
                var rootPath = Directory.GetParent(directoryPath)?.FullName;

                var newDirectoryPath = Path.Combine(rootPath, "DecryptedFiles");
                var destPath = Path.Combine(newDirectoryPath, fileName);

                await DecryptFileAsync(fullFilePath, destPath, password).ConfigureAwait(true);
            }
        }

        public Task DecryptFilesInParallelAsync(Queue<string> filesQueue, string password)
        {
            var size = filesQueue.Count;
            var decryptTasks = new Task[size];
            for (var i = 0; i < size; i++)
            {
                var fullFilePath = filesQueue.Dequeue();

                var fileName = Path.GetFileName(fullFilePath);
                var directoryPath = Path.GetDirectoryName(fullFilePath);
                var rootPath = Directory.GetParent(directoryPath)?.FullName;

                var newDirectoryPath = Path.Combine(rootPath, "DecryptedFiles");
                var destPath = Path.Combine(newDirectoryPath, fileName);

                decryptTasks[i] = DecryptFileAsync(fullFilePath, destPath, password);
            }

            return Task.WhenAll(decryptTasks);
        }
    }
}