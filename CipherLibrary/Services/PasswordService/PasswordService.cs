using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO.Compression;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CipherLibrary.DTOs;
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.SecureConfigManager;

namespace CipherLibrary.Services.PasswordService
{
    public class PasswordService : IPasswordService
    {
        private readonly IEventLoggerService _eventLoggerService;
        private readonly ISecureConfigManager _configManager;

        public PasswordService(IEventLoggerService eventLoggerService, ISecureConfigManager configManager)
        {
            _eventLoggerService = eventLoggerService;
            _configManager = configManager;
        }

        public void SetPassword(string password)
        {
            _configManager.SaveSetting(AppConfigKeys.EncryptedPassword, password);
        }

        public void SetPassword(byte[] password)
        {
            _configManager.SaveSetting(AppConfigKeys.EncryptedPassword, Encoding.Default.GetString(password));
        }

        public string GetPassword()
        {
            return DecryptPassword(
                Encoding.Default.GetBytes(_configManager.GetSetting(AppConfigKeys.EncryptedPassword)));
        }


        public string DecryptPassword(byte[] password)
        {
            byte[] decryptedPassword;
            using (var rsaPublicOnly = new RSACryptoServiceProvider())
            {
                rsaPublicOnly.FromXmlString(_configManager.GetSetting(AppConfigKeys.PrivateKey));
                decryptedPassword = rsaPublicOnly.Decrypt(password, true);
            }

            return Encoding.Default.GetString(decryptedPassword);
        }

        public bool IsPasswordSet()
        {
            return string.IsNullOrEmpty(_configManager.GetSetting(AppConfigKeys.EncryptedPassword));
        }

        public bool IsPasswordCorrect(string password)
        {
            _eventLoggerService.WriteDebug("CheckPassword started");
            Console.WriteLine("CheckPassword started");

            var passedPassword = DecryptPassword(Encoding.Default.GetBytes(password));
            var savedPassword =
                DecryptPassword(Encoding.Default.GetBytes(_configManager.GetSetting(AppConfigKeys.EncryptedPassword)));

            return string.Equals(passedPassword, savedPassword);
        }

        public bool IsPasswordCorrect(byte[] password)
        {
            _eventLoggerService.WriteDebug("CheckPassword started");
            Console.WriteLine("CheckPassword started");

            var passedPassword = DecryptPassword(password);
            var savedPassword =
                DecryptPassword(Encoding.Default.GetBytes(_configManager.GetSetting(AppConfigKeys.EncryptedPassword)));

            return string.Equals(passedPassword, savedPassword);
        }
    }
}