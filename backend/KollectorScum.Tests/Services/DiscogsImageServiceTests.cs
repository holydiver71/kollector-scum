using System.Net;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="DiscogsImageService"/>.
    /// </summary>
    public class DiscogsImageServiceTests
    {
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<DiscogsImageService>> _mockLogger;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;

        public DiscogsImageServiceTests()
        {
            _mockStorageService = new Mock<IStorageService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<DiscogsImageService>>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();

            // Default to development environment for tests
            _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");

            _mockConfiguration.Setup(c => c["R2:BucketName"]).Returns("test-bucket");
        }

        private DiscogsImageService CreateService(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler);
            return new DiscogsImageService(client, _mockStorageService.Object, _mockConfiguration.Object, _mockLogger.Object, _mockEnvironment.Object);
        }

        #region SanitizeFilename Tests

        [Fact]
        public void SanitizeFilename_RemovesInvalidCharacters()
        {
            var handler = new Mock<HttpMessageHandler>();
            var service = CreateService(handler.Object);

            // '/' is definitively invalid on all platforms
            var result = service.SanitizeFilename("Artist/Title.jpg");

            Assert.DoesNotContain("/", result);
            Assert.EndsWith(".jpg", result);
        }

        [Fact]
        public void SanitizeFilename_TruncatesLongFilenames()
        {
            var handler = new Mock<HttpMessageHandler>();
            var service = CreateService(handler.Object);

            var longName = new string('a', 300) + ".jpg";
            var result = service.SanitizeFilename(longName);

            Assert.True(result.Length <= 200);
            Assert.EndsWith(".jpg", result);
        }

        [Fact]
        public void SanitizeFilename_PreservesValidCharacters()
        {
            var handler = new Mock<HttpMessageHandler>();
            var service = CreateService(handler.Object);

            var filename = "Artist-Title-2023.jpg";
            var result = service.SanitizeFilename(filename);

            Assert.Equal(filename, result);
        }

        #endregion

        #region DownloadAndStoreCoverArtAsync Tests

        [Fact]
        public async Task DownloadAndStoreCoverArtAsync_SuccessfulDownload_ReturnsActualStoredFilename()
        {
            var userId = Guid.NewGuid();
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
                });

            // Storage returns the same filename — no suffix needed
            _mockStorageService.Setup(s => s.UploadFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("https://cdn.example.com/Artist-Title-2023.jpg");

            var service = CreateService(mockHandler.Object);

            var result = await service.DownloadAndStoreCoverArtAsync(
                "https://example.com/image.jpg", "Artist", "Title", "2023", userId);

            Assert.Equal("Artist-Title-2023.jpg", result);
            _mockStorageService.Verify(s => s.UploadFileAsync(
                "test-bucket", userId.ToString(), It.IsAny<string>(),
                It.IsAny<Stream>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DownloadAndStoreCoverArtAsync_WhenFilenameCollision_ReturnsActualSuffixedFilename()
        {
            // Arrange: storage returns a suffixed name (e.g. -1) because the original already exists
            var userId = Guid.NewGuid();
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
                });

            // Simulate LocalFileSystemStorageService appending -1 to avoid collision
            _mockStorageService.Setup(s => s.UploadFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync($"/cover-art/{userId}/PIXIES-Trompe Le Monde-1991-1.jpg");

            var service = CreateService(mockHandler.Object);

            var result = await service.DownloadAndStoreCoverArtAsync(
                "https://img.discogs.com/cover.jpg", "PIXIES", "Trompe Le Monde", "1991", userId);

            // Must return the suffixed filename, NOT the original "PIXIES-Trompe Le Monde-1991.jpg"
            Assert.Equal("PIXIES-Trompe Le Monde-1991-1.jpg", result);
        }

        [Fact]
        public async Task DownloadAndStoreCoverArtAsync_FailedDownload_ReturnsNull()
        {
            var userId = Guid.NewGuid();
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });

            var service = CreateService(mockHandler.Object);

            var result = await service.DownloadAndStoreCoverArtAsync(
                "https://example.com/image.jpg", "Artist", "Title", "2023", userId);

            Assert.Null(result);
            _mockStorageService.Verify(s => s.UploadFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DownloadAndStoreCoverArtAsync_HttpException_ReturnsNull()
        {
            var userId = Guid.NewGuid();
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var service = CreateService(mockHandler.Object);

            var result = await service.DownloadAndStoreCoverArtAsync(
                "https://example.com/image.jpg", "Artist", "Title", "2023", userId);

            Assert.Null(result);
        }

        #endregion
    }
}
