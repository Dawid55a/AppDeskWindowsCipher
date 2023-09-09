using System.IO.IsolatedStorage;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using CipherLibrary.DTOs;

namespace CipherLibrary.Services.SecureConfigManager
{
    public class SecureConfigManager : ISecureConfigManager
    {
        private const string FileName = "config.dat";


        public void SaveSetting(string key, string value)
        {
            var entries = LoadAllEntries();

            byte[] encryptedValue =
                ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);

            var existingEntry = entries.FirstOrDefault(e => e.Key == key);
            if (existingEntry != null)
            {
                existingEntry.EncryptedValue = encryptedValue;
            }
            else
            {
                entries.Add(new ConfigEntry {Key = key, EncryptedValue = encryptedValue});
            }

            SaveAllEntries(entries);
        }

        public string GetSetting(string key)
        {
            var entries = LoadAllEntries();
            var entry = entries.FirstOrDefault(e => e.Key == key);
            if (entry != null)
            {
                byte[] decryptedValue =
                    ProtectedData.Unprotect(entry.EncryptedValue, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedValue);
            }

            return null;
        }

        private List<ConfigEntry> LoadAllEntries()
        {
            var entries = new List<ConfigEntry>();
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                if (isolatedStorage.FileExists(FileName))
                {
                    using (var stream = new IsolatedStorageFileStream(FileName, FileMode.Open, isolatedStorage))
                    using (var reader = new BinaryReader(stream))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            var key = reader.ReadString();
                            var encryptedValueLength = reader.ReadInt32();
                            var encryptedValue = reader.ReadBytes(encryptedValueLength);

                            entries.Add(new ConfigEntry {Key = key, EncryptedValue = encryptedValue});
                        }
                    }
                }
            }

            return entries;
        }

        private void SaveAllEntries(List<ConfigEntry> entries)
        {
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            using (var stream = new IsolatedStorageFileStream(FileName, FileMode.Create, isolatedStorage))
            using (var writer = new BinaryWriter(stream))
            {
                foreach (var entry in entries)
                {
                    writer.Write(entry.Key);
                    writer.Write(entry.EncryptedValue.Length);
                    writer.Write(entry.EncryptedValue);
                }
            }
        }
    }
}