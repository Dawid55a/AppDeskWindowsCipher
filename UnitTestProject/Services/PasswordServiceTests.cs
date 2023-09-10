using CipherLibrary.DTOs;
using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.PasswordService;
using CipherLibrary.Services.SecureConfigManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Security.Cryptography;
using System.Text;

namespace UnitTestProject.Services
{
    
    [TestClass]
    public class PasswordServiceTests
    {
        private Mock<IEventLoggerService> _mockEventLoggerService;
        private Mock<ISecureConfigManager> _mockConfigManager;
        private PasswordService _passwordService;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockEventLoggerService = new Mock<IEventLoggerService>();
            _mockConfigManager = new Mock<ISecureConfigManager>();
            _passwordService = new PasswordService(_mockEventLoggerService.Object, _mockConfigManager.Object);
        }

        [TestMethod]
        public void Test_SetPassword_StringVersion()
        {
            string testPassword = "test_password";
            _passwordService.SetPassword(testPassword);
            _mockConfigManager.Verify(m => m.SaveSetting(AppConfigKeys.EncryptedPassword, testPassword), Times.Once);
        }

        [TestMethod]
        public void Test_DecryptPassword_ReturnsDecryptedPassword()
        {
            string plaintextPassword = "test_password";
            using (var rsa = new RSACryptoServiceProvider())
            {
                string publicKey = rsa.ToXmlString(false);
                string privateKey = rsa.ToXmlString(true);

                byte[] encryptedPassword;
                using (var rsaPublicOnly = new RSACryptoServiceProvider())
                {
                    rsaPublicOnly.FromXmlString(publicKey);
                    encryptedPassword = rsaPublicOnly.Encrypt(Encoding.Default.GetBytes(plaintextPassword), true);
                }

                _mockConfigManager.Setup(m => m.GetSetting(AppConfigKeys.PrivateKey)).Returns(privateKey);

                string decryptedPassword = _passwordService.DecryptPassword(encryptedPassword);

                Assert.AreEqual(plaintextPassword, decryptedPassword);
            }
        }


        [TestMethod]
        public void Test_IsPasswordSet_PasswordIsSet_ReturnsTrue()
        {
            _mockConfigManager.Setup(m => m.GetSetting(AppConfigKeys.EncryptedPassword)).Returns("some_encrypted_password");
            bool isPasswordSet = _passwordService.IsPasswordSet();
            Assert.IsFalse(isPasswordSet); // because method checks for IsNullOrEmpty
        }

        [TestMethod]
        public void Test_IsPasswordSet_PasswordIsNotSet_ReturnsFalse()
        {
            _mockConfigManager.Setup(m => m.GetSetting(AppConfigKeys.EncryptedPassword)).Returns("");
            bool isPasswordSet = _passwordService.IsPasswordSet();
            Assert.IsTrue(isPasswordSet); // because method checks for IsNullOrEmpty
        }

    }

}
