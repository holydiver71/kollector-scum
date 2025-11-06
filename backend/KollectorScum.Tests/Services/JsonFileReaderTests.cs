using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using KollectorScum.Api.Services;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for JsonFileReader service
    /// Tests file I/O operations, JSON deserialization, and error handling
    /// </summary>
    public class JsonFileReaderTests : IDisposable
    {
        private readonly Mock<ILogger<JsonFileReader>> _mockLogger;
        private readonly JsonFileReader _service;
        private readonly string _testDirectory;

        public JsonFileReaderTests()
        {
            _mockLogger = new Mock<ILogger<JsonFileReader>>();
            _service = new JsonFileReader(_mockLogger.Object);
            _testDirectory = Path.Combine(Path.GetTempPath(), $"JsonFileReaderTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        #region ReadJsonFileAsync Tests

        [Fact]
        public async Task ReadJsonFileAsync_WithValidFile_ReturnsDeserializedObject()
        {
            // Arrange
            var testData = new TestDto { Id = 1, Name = "Test" };
            var filePath = Path.Combine(_testDirectory, "valid.json");
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(testData));

            // Act
            var result = await _service.ReadJsonFileAsync<TestDto>(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public async Task ReadJsonFileAsync_WithValidArray_ReturnsDeserializedList()
        {
            // Arrange
            var testData = new[]
            {
                new TestDto { Id = 1, Name = "Test1" },
                new TestDto { Id = 2, Name = "Test2" }
            };
            var filePath = Path.Combine(_testDirectory, "array.json");
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(testData));

            // Act
            var result = await _service.ReadJsonFileAsync<TestDto[]>(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Equal("Test1", result[0].Name);
            Assert.Equal("Test2", result[1].Name);
        }

        [Fact]
        public async Task ReadJsonFileAsync_WithCaseInsensitiveProperties_DeserializesCorrectly()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "caseinsensitive.json");
            await File.WriteAllTextAsync(filePath, "{\"id\": 1, \"NAME\": \"Test\"}");

            // Act
            var result = await _service.ReadJsonFileAsync<TestDto>(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public async Task ReadJsonFileAsync_WithEmptyFile_ReturnsNull()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "empty.json");
            await File.WriteAllTextAsync(filePath, string.Empty);

            // Act
            var result = await _service.ReadJsonFileAsync<TestDto>(filePath);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ReadJsonFileAsync_WithWhitespaceOnlyFile_ReturnsNull()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "whitespace.json");
            await File.WriteAllTextAsync(filePath, "   \n\t  ");

            // Act
            var result = await _service.ReadJsonFileAsync<TestDto>(filePath);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ReadJsonFileAsync_WithNonExistentFile_ReturnsNull()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "nonexistent.json");

            // Act
            var result = await _service.ReadJsonFileAsync<TestDto>(filePath);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ReadJsonFileAsync_WithInvalidJson_ThrowsJsonException()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "invalid.json");
            await File.WriteAllTextAsync(filePath, "{ invalid json }");

            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(
                () => _service.ReadJsonFileAsync<TestDto>(filePath)
            );
        }

        [Fact]
        public async Task ReadJsonFileAsync_WithNullOrEmptyPath_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ReadJsonFileAsync<TestDto>(null!)
            );

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ReadJsonFileAsync<TestDto>(string.Empty)
            );

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ReadJsonFileAsync<TestDto>("   ")
            );
        }

        [Fact]
        public async Task ReadJsonFileAsync_WithUtf8Encoding_DeserializesCorrectly()
        {
            // Arrange
            var testData = new TestDto { Id = 1, Name = "Test™ ñ 中文" };
            var filePath = Path.Combine(_testDirectory, "utf8.json");
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(testData), Encoding.UTF8);

            // Act
            var result = await _service.ReadJsonFileAsync<TestDto>(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test™ ñ 中文", result.Name);
        }

        [Fact]
        public async Task ReadJsonFileAsync_WithLargeFile_PerformsWithinReasonableTime()
        {
            // Arrange
            var largeArray = new TestDto[1000];
            for (int i = 0; i < 1000; i++)
            {
                largeArray[i] = new TestDto { Id = i, Name = $"Test{i}" };
            }
            var filePath = Path.Combine(_testDirectory, "large.json");
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(largeArray));

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _service.ReadJsonFileAsync<TestDto[]>(filePath);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1000, result.Length);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Operation took {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region FileExists Tests

        [Fact]
        public void FileExists_WithExistingFile_ReturnsTrue()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "exists.json");
            File.WriteAllText(filePath, "{}");

            // Act
            var result = _service.FileExists(filePath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void FileExists_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "doesnotexist.json");

            // Act
            var result = _service.FileExists(filePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void FileExists_WithNullOrEmptyPath_ReturnsFalse()
        {
            // Act & Assert
            Assert.False(_service.FileExists(null!));
            Assert.False(_service.FileExists(string.Empty));
            Assert.False(_service.FileExists("   "));
        }

        #endregion

        #region GetJsonArrayCountAsync Tests

        [Fact]
        public async Task GetJsonArrayCountAsync_WithValidArray_ReturnsCorrectCount()
        {
            // Arrange
            var testData = new[]
            {
                new TestDto { Id = 1, Name = "Test1" },
                new TestDto { Id = 2, Name = "Test2" },
                new TestDto { Id = 3, Name = "Test3" }
            };
            var filePath = Path.Combine(_testDirectory, "count.json");
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(testData));

            // Act
            var result = await _service.GetJsonArrayCountAsync<TestDto>(filePath);

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public async Task GetJsonArrayCountAsync_WithEmptyArray_ReturnsZero()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "emptyarray.json");
            await File.WriteAllTextAsync(filePath, "[]");

            // Act
            var result = await _service.GetJsonArrayCountAsync<TestDto>(filePath);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetJsonArrayCountAsync_WithNonExistentFile_ReturnsZero()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "nonexistent.json");

            // Act
            var result = await _service.GetJsonArrayCountAsync<TestDto>(filePath);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetJsonArrayCountAsync_WithNullOrEmptyPath_ReturnsZero()
        {
            // Act & Assert
            Assert.Equal(0, await _service.GetJsonArrayCountAsync<TestDto>(null!));
            Assert.Equal(0, await _service.GetJsonArrayCountAsync<TestDto>(string.Empty));
            Assert.Equal(0, await _service.GetJsonArrayCountAsync<TestDto>("   "));
        }

        [Fact]
        public async Task GetJsonArrayCountAsync_WithInvalidJson_ReturnsZero()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "invalidcount.json");
            await File.WriteAllTextAsync(filePath, "{ not an array }");

            // Act
            var result = await _service.GetJsonArrayCountAsync<TestDto>(filePath);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region Test Helper Classes

        private class TestDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        #endregion
    }
}
