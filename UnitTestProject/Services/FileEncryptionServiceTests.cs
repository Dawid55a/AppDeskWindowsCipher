using CipherLibrary.Services.EventLoggerService;
using CipherLibrary.Services.FileEncryptionService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTestProject.Services
{
    [TestClass]
    public class FileEncryptionServiceTests
    {
        private MockRepository _mockRepository;
        private Mock<IEventLoggerService> _mockEventLoggerService;

        private const string _fileName = "test_file";

        [TestInitialize]
        public void TestInitialize()
        {
            this._mockRepository = new MockRepository(MockBehavior.Strict);

            this._mockEventLoggerService = this._mockRepository.Create<IEventLoggerService>();

            File.Create(_fileName).Close();
        }

        private FileEncryptionService CreateService()
        {
            return new FileEncryptionService(
                this._mockEventLoggerService.Object);
        }

        [TestMethod]
        public async Task EncryptFileAsync_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var fileEncryptionService = this.CreateService();
            string testDestFile = _fileName;
            string testPassword = "test_password";

            _mockEventLoggerService.Setup(m => m.WriteDebug(It.IsAny<string>()));

            // Act
            await fileEncryptionService.EncryptFileAsync(_fileName, testDestFile, testPassword);

            // Assert
            _mockEventLoggerService.Verify(m => m.WriteDebug(It.IsAny<string>()), Times.Once);
            Assert.IsTrue(File.Exists(testDestFile + ".enc"));
            Assert.IsFalse(File.Exists(_fileName));
        }


        [TestMethod]
        public async Task EncryptFileAsync_WhenExceptionOccurs_LogsError_ThrowsException()
        {
            // Arrange
            var fileEncryptionService = this.CreateService();
            string nonExistingFile = "non_existing_file";
            string testDestFile = _fileName;
            string testPassword = "test_password";

            _mockEventLoggerService.Setup(m => m.WriteError(It.IsAny<string>()));

            // Act
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(() =>
                fileEncryptionService.EncryptFileAsync(nonExistingFile, testDestFile, testPassword));

            // Assert
            _mockEventLoggerService.Verify(m => m.WriteError(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task EncryptFilesInQueueAsync__StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var fileEncryptionService = this.CreateService();
            var filesQueue = new Queue<string>();
            string password = "test_password";

            string file1 = "file1.txt";
            string file2 = "file2.txt";

            var directory = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "DecryptedFiles"));

            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "EncryptedFiles"));

            string file1Path = Path.Combine(directory.FullName, file1);
            File.Create(file1Path).Close();
            filesQueue.Enqueue(file1Path);

            string file2Path = Path.Combine(directory.FullName, file2);
            File.Create(file2Path).Close();
            filesQueue.Enqueue(file2Path);


            _mockEventLoggerService.Setup(m => m.WriteDebug(It.IsAny<string>()));

            var encryptedFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "EncryptedFiles");


            // Act
            await fileEncryptionService.EncryptFilesInQueueAsync(filesQueue, password);

            // Assert
            Assert.IsFalse(filesQueue.Any());
            _mockEventLoggerService.Verify(m => m.WriteDebug(It.IsAny<string>()), Times.Exactly(2));

            // Clean
            if (Directory.Exists(encryptedFolderPath))
            {
                Directory.Delete(encryptedFolderPath, true);
            }

            if (Directory.Exists(directory.FullName))
            {
                Directory.Delete(directory.FullName, true);
            }
        }


        [TestMethod]
        public async Task EncryptFilesInParallelAsync_WhenCalled_EncryptsAllFilesInParallel()
        {
            // Arrange
            var fileEncryptionService = this.CreateService();
            var filesQueue = new Queue<string>();
            string password = "test_password";

            string file1 = "file1.txt";
            string file2 = "file2.txt";

            var directory = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "DecryptedFiles"));
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "EncryptedFiles"));

            string file1Path = Path.Combine(directory.FullName, file1);
            using (var fileBig = File.Create(file1Path))
            {
                fileBig.Seek(2048L * 1024 * 1024, SeekOrigin.Begin);
                fileBig.WriteByte(0);
                fileBig.Close();
            }

            
            filesQueue.Enqueue(file1Path);

            string file2Path = Path.Combine(directory.FullName, file2);
            File.Create(file2Path).Close();
            filesQueue.Enqueue(file2Path);

            List<string> loggedMessages = new List<string>();

            _mockEventLoggerService.Setup(m => m.WriteDebug(It.IsAny<string>()))
                .Callback<string>(msg => loggedMessages.Add(msg));


            var encryptedFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "EncryptedFiles");

            // Act
            await fileEncryptionService.EncryptFilesInParallelAsync(filesQueue, password);

            // Assert
            Assert.IsFalse(filesQueue.Any());
            _mockEventLoggerService.Verify(m => m.WriteDebug(It.IsAny<string>()), Times.Exactly(2));
            _mockEventLoggerService.Verify(m => m.WriteDebug(It.Is<string>(msg => msg.Contains($"Encrypted file {file1Path}"))), Times.Once());
            _mockEventLoggerService.Verify(m => m.WriteDebug(It.Is<string>(msg => msg.Contains($"Encrypted file {file2Path}"))), Times.Once());

            // Second smaller file should be encrypted first
            Assert.IsTrue(loggedMessages.First().Contains(file2Path));

                // Clean
            if (Directory.Exists(encryptedFolderPath))
            {
                Directory.Delete(encryptedFolderPath, true);
            }

            if (Directory.Exists(directory.FullName))
            {
                Directory.Delete(directory.FullName, true);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            File.Delete(_fileName);
        }
    }
}