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
        private IEventLoggerService _eventLoggerService;

        private readonly NameValueCollection _allAppSettings = ConfigurationManager.AppSettings;
        private Configuration _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private readonly string _decryptedFilesPath;
        private readonly string _encryptedFilesPath;
        public List<string> DecryptedFiles = new List<string>();

        public FileDecryptionService(IEventLoggerService eventLoggerService)
        {
            _eventLoggerService = eventLoggerService;
            if (_allAppSettings["WorkFolder"] == "") throw new ArgumentException("WorkFolder is empty");
            var workDirectoryPath = _allAppSettings["WorkFolder"];
            _encryptedFilesPath = Path.Combine(workDirectoryPath, "EncryptedFiles");
            _decryptedFilesPath = Path.Combine(workDirectoryPath, "DecryptedFiles");
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
                                using (var cryptoStream = new CryptoStream(sourceStream, decryptor, CryptoStreamMode.Read))
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
                Console.WriteLine(e);
                throw;
            }
            Console.WriteLine($"Decrypted file {sourceFile}");
            _eventLoggerService.WriteInfo($"Decrypted file {sourceFile}");
            File.Delete(sourceFile);
        }

        public async Task DecryptFilesInQueueAsync(Queue<string> filesQueue, string password)
        {
            while (filesQueue.Count > 0)
            {
                var file = filesQueue.Dequeue();
                var sourceFile = Path.Combine(_encryptedFilesPath, file);
                var destFile = Path.Combine(_decryptedFilesPath, file);
                await DecryptFileAsync(sourceFile, destFile, password).ConfigureAwait(true);
                DecryptedFiles.Add(file);
            }
        }

        public Task DecryptFilesInParallelAsync(Queue<string> filesQueue, string password)
        {
            var size = filesQueue.Count;
            var tasks = new Task[size];
            for (var i = 0; i < size; i++)
            {
                var file = filesQueue.Dequeue();
                var sourceFile = Path.Combine(_encryptedFilesPath, file);
                var destFile = Path.Combine(_decryptedFilesPath, file);
                tasks[i] = DecryptFileAsync(sourceFile, destFile, password);
                DecryptedFiles.Add(file);
            }
            return Task.WhenAll(tasks);
        }
    }
}