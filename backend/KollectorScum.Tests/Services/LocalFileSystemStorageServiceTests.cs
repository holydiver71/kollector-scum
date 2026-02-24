using KollectorScum.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for LocalFileSystemStorageService covering path safety, extension validation, and failure scenarios.
    /// </summary>
    public class LocalFileSystemStorageServiceTests : IDisposable
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<ILogger<LocalFileSystemStorageService>> _mockLogger;
        private readonly LocalFileSystemStorageService _service;
        private readonly string _testWebRootPath;
        private readonly Guid _testUserId = Guid.NewGuid();

        public LocalFileSystemStorageServiceTests()
        {
            // Create a temporary directory for testing
            _testWebRootPath = Path.Combine(Path.GetTempPath(), $"test_wwwroot_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testWebRootPath);

            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(e => e.WebRootPath).Returns(_testWebRootPath);

            _mockLogger = new Mock<ILogger<LocalFileSystemStorageService>>();

            _service = new LocalFileSystemStorageService(_mockEnvironment.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            // Clean up test directory
            if (Directory.Exists(_testWebRootPath))
            {
                Directory.Delete(_testWebRootPath, recursive: true);
            }
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var service = new LocalFileSystemStorageService(_mockEnvironment.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullEnvironment_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LocalFileSystemStorageService(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LocalFileSystemStorageService(_mockEnvironment.Object, null!));
        }

        #endregion

        #region UploadFileAsync - Valid Files

        [Theory]
        [InlineData("test.jpg", "image/jpeg")]
        [InlineData("test.jpeg", "image/jpeg")]
        [InlineData("test.png", "image/png")]
        [InlineData("test.webp", "image/webp")]
        [InlineData("test.gif", "image/gif")]
        public async Task UploadFileAsync_WithValidExtension_UploadsSuccessfully(string fileName, string contentType)
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var content = "fake image data"u8.ToArray();
            using var stream = new MemoryStream(content);

            // Act
            var result = await _service.UploadFileAsync(bucketName, userId, fileName, stream, contentType);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith($"/{bucketName}/{userId}/", result);
            Assert.Contains(".jpg", result, StringComparison.OrdinalIgnoreCase);

            // Verify file was created
            var expectedDir = Path.Combine(_testWebRootPath, bucketName, userId);
            Assert.True(Directory.Exists(expectedDir));
            var files = Directory.GetFiles(expectedDir);
            Assert.Single(files);
            Assert.True(File.Exists(files[0]));
        }

        [Fact]
        public async Task UploadFileAsync_CreatesDirectoryIfNotExists()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var fileName = "test.jpg";
            var content = "fake image data"u8.ToArray();
            using var stream = new MemoryStream(content);

            var expectedDir = Path.Combine(_testWebRootPath, bucketName, userId);
            Assert.False(Directory.Exists(expectedDir));

            // Act
            var result = await _service.UploadFileAsync(bucketName, userId, fileName, stream, "image/jpeg");

            // Assert
            Assert.True(Directory.Exists(expectedDir));
        }

        [Fact]
        public async Task UploadFileAsync_GeneratesUniqueFileName()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var fileName = "test.jpg";
            var content = "fake image data"u8.ToArray();

            // Act - Upload same file twice
            using var stream1 = new MemoryStream(content);
            var result1 = await _service.UploadFileAsync(bucketName, userId, fileName, stream1, "image/jpeg");

            using var stream2 = new MemoryStream(content);
            var result2 = await _service.UploadFileAsync(bucketName, userId, fileName, stream2, "image/jpeg");

            // Assert - Should generate different file names
            Assert.NotEqual(result1, result2);

            var expectedDir = Path.Combine(_testWebRootPath, bucketName, userId);
            var files = Directory.GetFiles(expectedDir);
            Assert.Equal(2, files.Length);
        }

        #endregion

        #region UploadFileAsync - Invalid Extensions

        [Theory]
        [InlineData("malicious.exe")]
        [InlineData("script.sh")]
        [InlineData("document.pdf")]
        [InlineData("archive.zip")]
        [InlineData("file.txt")]
        [InlineData("file")]
        [InlineData(".gitignore")]
        public async Task UploadFileAsync_WithInvalidExtension_ThrowsArgumentException(string fileName)
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var content = "fake content"u8.ToArray();
            using var stream = new MemoryStream(content);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadFileAsync(bucketName, userId, fileName, stream, "application/octet-stream"));

            Assert.Contains("not allowed", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region UploadFileAsync - Path Safety

        [Theory]
        [InlineData("../../../etc/passwd")]
        [InlineData("..\\..\\..\\windows\\system32\\config\\sam")]
        [InlineData("../../sneaky.jpg")]
        [InlineData("../test.jpg")]
        [InlineData("subdir/../../../etc/passwd")]
        public async Task UploadFileAsync_WithDirectoryTraversalAttempt_SanitizesPath(string maliciousFileName)
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var content = "fake image data"u8.ToArray();
            using var stream = new MemoryStream(content);

            // Act
            var result = await _service.UploadFileAsync(bucketName, userId, maliciousFileName, stream, "image/jpeg");

            // Assert - Should only contain bucket name and user ID in path
            Assert.StartsWith($"/{bucketName}/{userId}/", result);
            Assert.DoesNotContain("..", result);
            Assert.DoesNotContain("etc", result);
            Assert.DoesNotContain("windows", result);

            // Verify file is in expected directory only
            var expectedDir = Path.Combine(_testWebRootPath, bucketName, userId);
            Assert.True(Directory.Exists(expectedDir));
            var files = Directory.GetFiles(expectedDir);
            Assert.Single(files);

            // Verify file is not outside the expected directory
            var parentDirs = Directory.GetDirectories(_testWebRootPath);
            Assert.Single(parentDirs); // Only "cover-art" directory should exist
        }

        [Theory]
        [InlineData("/absolute/path/image.jpg")]
        [InlineData("C:\\Windows\\image.jpg")]
        [InlineData("\\\\network\\share\\image.jpg")]
        public async Task UploadFileAsync_WithAbsolutePath_ExtractsFileNameOnly(string absolutePath)
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var content = "fake image data"u8.ToArray();
            using var stream = new MemoryStream(content);

            // Act
            var result = await _service.UploadFileAsync(bucketName, userId, absolutePath, stream, "image/jpeg");

            // Assert - Should only use the file name
            Assert.StartsWith($"/{bucketName}/{userId}/", result);
            Assert.DoesNotContain("absolute", result);
            Assert.DoesNotContain("Windows", result);
            Assert.DoesNotContain("network", result);

            var expectedDir = Path.Combine(_testWebRootPath, bucketName, userId);
            var files = Directory.GetFiles(expectedDir);
            Assert.Single(files);
        }

        #endregion

        #region UploadFileAsync - File Size

        [Fact]
        public async Task UploadFileAsync_WithFileTooLarge_ThrowsArgumentException()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var fileName = "huge.jpg";
            
            // Create a stream larger than 5MB
            var largeContent = new byte[6 * 1024 * 1024]; // 6MB
            using var stream = new MemoryStream(largeContent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadFileAsync(bucketName, userId, fileName, stream, "image/jpeg"));

            Assert.Contains("exceeds maximum", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UploadFileAsync_WithFileSizeExactlyAtLimit_UploadsSuccessfully()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var fileName = "max-size.jpg";
            
            // Create a stream exactly 5MB
            var content = new byte[5 * 1024 * 1024]; // 5MB
            using var stream = new MemoryStream(content);

            // Act
            var result = await _service.UploadFileAsync(bucketName, userId, fileName, stream, "image/jpeg");

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith($"/{bucketName}/{userId}/", result);
        }

        #endregion

        #region UploadFileAsync - Edge Cases

        [Fact]
        public async Task UploadFileAsync_WithEmptyFileName_ThrowsArgumentException()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var content = "fake image data"u8.ToArray();
            using var stream = new MemoryStream(content);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadFileAsync(bucketName, userId, "", stream, "image/jpeg"));
        }

        [Fact]
        public async Task UploadFileAsync_WithNullStream_ThrowsArgumentNullException()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var fileName = "test.jpg";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.UploadFileAsync(bucketName, userId, fileName, null!, "image/jpeg"));
        }

        [Fact]
        public async Task UploadFileAsync_WithEmptyStream_ThrowsArgumentException()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var fileName = "test.jpg";
            using var stream = new MemoryStream();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadFileAsync(bucketName, userId, fileName, stream, "image/jpeg"));

            Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region DeleteFileAsync

        [Fact]
        public async Task DeleteFileAsync_WithExistingFile_DeletesSuccessfully()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var fileName = "test.jpg";
            var content = "fake image data"u8.ToArray();
            
            // First upload a file
            using var uploadStream = new MemoryStream(content);
            var uploadResult = await _service.UploadFileAsync(bucketName, userId, fileName, uploadStream, "image/jpeg");
            
            // Extract the actual file name (which includes GUID)
            var actualFileName = Path.GetFileName(uploadResult);
            
            // Verify file exists
            var filePath = Path.Combine(_testWebRootPath, bucketName, userId, actualFileName);
            Assert.True(File.Exists(filePath));

            // Act
            await _service.DeleteFileAsync(bucketName, userId, actualFileName);

            // Assert
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public async Task DeleteFileAsync_WithNonExistentFile_DoesNotThrow()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var fileName = "nonexistent.jpg";

            // Act & Assert - Should not throw
            await _service.DeleteFileAsync(bucketName, userId, fileName);
        }

        [Theory]
        [InlineData("../../../etc/passwd")]
        [InlineData("../../sneaky.jpg")]
        public async Task DeleteFileAsync_WithDirectoryTraversalAttempt_OnlyDeletesInUserDirectory(string maliciousFileName)
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            
            // Create a file to protect
            var protectedDir = Path.Combine(_testWebRootPath, "protected");
            Directory.CreateDirectory(protectedDir);
            var protectedFile = Path.Combine(protectedDir, "important.txt");
            await File.WriteAllTextAsync(protectedFile, "important data");

            // Act - Try to delete with traversal path
            await _service.DeleteFileAsync(bucketName, userId, maliciousFileName);

            // Assert - Protected file should still exist
            Assert.True(File.Exists(protectedFile));
        }

        #endregion

        #region GetPublicUrl

        [Fact]
        public void GetPublicUrl_ReturnsCorrectFormat()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var fileName = "test.jpg";

            // Act
            var result = _service.GetPublicUrl(bucketName, userId, fileName);

            // Assert
            Assert.Equal($"/{bucketName}/{userId}/{fileName}", result);
        }

        [Fact]
        public void GetPublicUrl_WithPathTraversal_SanitizesFileName()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var maliciousFileName = "../../etc/passwd";

            // Act
            var result = _service.GetPublicUrl(bucketName, userId, maliciousFileName);

            // Assert
            Assert.StartsWith($"/{bucketName}/{userId}/", result);
            Assert.DoesNotContain("..", result);
            Assert.DoesNotContain("etc", result);
        }

        #endregion

        #region GetFileStreamAsync

        [Fact]
        public async Task GetFileStreamAsync_ExistingFile_ReturnsStream()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var fileName = "stream-test.jpg";
            var content = "fake image bytes"u8.ToArray();

            // Upload first so the file exists; UploadFileAsync returns /{bucket}/{userId}/{uniqueName}
            using var uploadStream = new MemoryStream(content);
            var uploadedPath = await _service.UploadFileAsync(bucketName, userId, fileName, uploadStream, "image/jpeg");
            var uniqueFileName = Path.GetFileName(uploadedPath); // extract the stored unique name

            // Act
            var stream = await _service.GetFileStreamAsync(bucketName, userId, uniqueFileName);

            // Assert
            Assert.NotNull(stream);
            var result = new byte[stream!.Length];
            await stream.ReadAsync(result);
            Assert.Equal(content, result);
            await stream.DisposeAsync();
        }

        [Fact]
        public async Task GetFileStreamAsync_NonExistentFile_ReturnsNull()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();

            // Act
            var stream = await _service.GetFileStreamAsync(bucketName, userId, "does-not-exist.jpg");

            // Assert
            Assert.Null(stream);
        }

        [Fact]
        public async Task GetFileStreamAsync_PathTraversalAttempt_ReturnsNull()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();

            // Act – Path.GetFileName will strip traversal so file is just "passwd" which does not exist
            var stream = await _service.GetFileStreamAsync(bucketName, userId, "../../etc/passwd");

            // Assert
            Assert.Null(stream);
        }

        #endregion

        #region Integration Scenarios

        [Fact]
        public async Task CompleteLifecycle_UploadAndDelete_WorksCorrectly()
        {
            // Arrange
            var bucketName = "cover-art";
            var userId = _testUserId.ToString();
            var fileName = "lifecycle.jpg";
            var content = "test image data"u8.ToArray();

            // Act - Upload
            using var uploadStream = new MemoryStream(content);
            var uploadResult = await _service.UploadFileAsync(bucketName, userId, fileName, uploadStream, "image/jpeg");
            
            var actualFileName = Path.GetFileName(uploadResult);
            var filePath = Path.Combine(_testWebRootPath, bucketName, userId, actualFileName);
            
            Assert.True(File.Exists(filePath));

            // Act - Delete
            await _service.DeleteFileAsync(bucketName, userId, actualFileName);

            // Assert
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public async Task MultipleUsers_StoreFilesInSeparateDirectories()
        {
            // Arrange
            var bucketName = "cover-art";
            var user1Id = Guid.NewGuid().ToString();
            var user2Id = Guid.NewGuid().ToString();
            var fileName = "test.jpg";
            var content = "fake image data"u8.ToArray();

            // Act
            using var stream1 = new MemoryStream(content);
            var result1 = await _service.UploadFileAsync(bucketName, user1Id, fileName, stream1, "image/jpeg");

            using var stream2 = new MemoryStream(content);
            var result2 = await _service.UploadFileAsync(bucketName, user2Id, fileName, stream2, "image/jpeg");

            // Assert
            Assert.Contains(user1Id, result1);
            Assert.Contains(user2Id, result2);
            Assert.DoesNotContain(user2Id, result1);
            Assert.DoesNotContain(user1Id, result2);

            // Verify separate directories exist
            var user1Dir = Path.Combine(_testWebRootPath, bucketName, user1Id);
            var user2Dir = Path.Combine(_testWebRootPath, bucketName, user2Id);
            Assert.True(Directory.Exists(user1Dir));
            Assert.True(Directory.Exists(user2Dir));
            Assert.Single(Directory.GetFiles(user1Dir));
            Assert.Single(Directory.GetFiles(user2Dir));
        }

        #endregion
    }
}
