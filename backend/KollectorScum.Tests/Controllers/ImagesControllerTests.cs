using System.Net;
using System.Text;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Unit tests for the new ImagesController endpoints:
    ///   POST /api/images/upload
    ///   GET  /api/images/search
    ///   POST /api/images/download (updated with resize + thumbnail)
    /// </summary>
    public class ImagesControllerTests
    {
        private readonly Mock<IStorageService> _mockStorage;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly Mock<IImageResizerService> _mockResizer;
        private readonly Mock<ILogger<ImagesController>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly Guid _testUserId = Guid.NewGuid();

        public ImagesControllerTests()
        {
            _mockStorage = new Mock<IStorageService>();
            _mockUserContext = new Mock<IUserContext>();
            _mockResizer = new Mock<IImageResizerService>();
            _mockLogger = new Mock<ILogger<ImagesController>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            _mockUserContext.Setup(u => u.GetActingUserId()).Returns(_testUserId);

            // Default resizer: returns a 1-byte stream
            _mockResizer
                .Setup(r => r.ResizeAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(() =>
                {
                    var s = new MemoryStream(new byte[] { 0xFF, 0xD8 });
                    s.Position = 0;
                    return (Stream)s;
                });

            // Default storage: returns a public URL
            _mockStorage
                .Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync((string bucket, string user, string file, Stream _, string _2) =>
                    $"https://cdn.example.com/{bucket}/{user}/{file}");

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ImagesPath"] = Path.GetTempPath(),
                    ["Google:ApiKey"] = "test-key",
                    ["Google:SearchEngineId"] = "test-cx",
                })
                .Build();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private ImagesController CreateController(HttpClient? httpClient = null)
        {
            if (httpClient != null)
                _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            else
                _mockHttpClientFactory
                    .Setup(f => f.CreateClient(It.IsAny<string>()))
                    .Returns(new HttpClient());

            return new ImagesController(
                _configuration,
                _mockLogger.Object,
                _mockStorage.Object,
                _mockUserContext.Object,
                _mockResizer.Object,
                _mockHttpClientFactory.Object);
        }

        private static IFormFile CreateFormFile(string fileName, long length, string contentType = "image/jpeg")
        {
            var content = new byte[length];
            var stream = new MemoryStream(content);
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns(fileName);
            mock.Setup(f => f.Length).Returns(length);
            mock.Setup(f => f.ContentType).Returns(contentType);
            mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((dest, _) =>
                {
                    stream.Position = 0;
                    return stream.CopyToAsync(dest);
                });
            return mock.Object;
        }

        // ─── UploadImage Tests ────────────────────────────────────────────────

        [Fact]
        public async Task UploadImage_WhenNoFile_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.UploadImage(null!, false);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("No file", bad.Value?.ToString());
        }

        [Fact]
        public async Task UploadImage_WhenEmptyFile_ReturnsBadRequest()
        {
            var controller = CreateController();
            var file = CreateFormFile("cover.jpg", 0);
            var result = await controller.UploadImage(file, false);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("No file", bad.Value?.ToString());
        }

        [Fact]
        public async Task UploadImage_WhenOversized_ReturnsBadRequest()
        {
            var controller = CreateController();
            const long over5Mb = 5_242_881;
            var file = CreateFormFile("cover.jpg", over5Mb);
            var result = await controller.UploadImage(file, false);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("5 MB", bad.Value?.ToString());
        }

        [Theory]
        [InlineData("cover.bmp")]
        [InlineData("cover.tiff")]
        [InlineData("cover.exe")]
        public async Task UploadImage_WhenBadExtension_ReturnsBadRequest(string filename)
        {
            var controller = CreateController();
            var file = CreateFormFile(filename, 1024);
            var result = await controller.UploadImage(file, false);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("extension", bad.Value?.ToString());
        }

        [Theory]
        [InlineData("cover.jpg")]
        [InlineData("cover.jpeg")]
        [InlineData("cover.png")]
        [InlineData("cover.webp")]
        [InlineData("cover.gif")]
        public async Task UploadImage_WithValidFile_ReturnsOkWithFilenameAndUrl(string filename)
        {
            var controller = CreateController();
            var file = CreateFormFile(filename, 1024);
            var result = await controller.UploadImage(file, false);
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ImageStoreResult>(ok.Value);
            Assert.False(string.IsNullOrWhiteSpace(body.Filename));
            Assert.Contains("cdn.example.com", body.PublicUrl);
            Assert.Null(body.ThumbnailFilename);
        }

        [Fact]
        public async Task UploadImage_WithGenerateThumbnailTrue_ReturnsThumbnailFields()
        {
            var controller = CreateController();
            var file = CreateFormFile("cover.jpg", 1024);
            var result = await controller.UploadImage(file, generateThumbnail: true);
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ImageStoreResult>(ok.Value);
            Assert.NotNull(body.ThumbnailFilename);
            Assert.NotNull(body.ThumbnailPublicUrl);
            Assert.StartsWith("thumb-", body.ThumbnailFilename);
        }

        [Fact]
        public async Task UploadImage_CallsResizerWithCoverDimension()
        {
            var controller = CreateController();
            var file = CreateFormFile("cover.jpg", 1024);
            await controller.UploadImage(file, false);
            _mockResizer.Verify(
                r => r.ResizeAsync(It.IsAny<Stream>(), 1600, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task UploadImage_WithThumbnail_CallsResizerTwice()
        {
            var controller = CreateController();
            var file = CreateFormFile("cover.jpg", 1024);
            await controller.UploadImage(file, generateThumbnail: true);
            _mockResizer.Verify(
                r => r.ResizeAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Exactly(2));
            _mockResizer.Verify(
                r => r.ResizeAsync(It.IsAny<Stream>(), 300, It.IsAny<string>()),
                Times.Once);
        }

        // ─── SearchImages Tests ───────────────────────────────────────────────

        [Fact]
        public async Task SearchImages_WhenQueryEmpty_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.SearchImages(null);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("required", bad.Value?.ToString());
        }

        [Fact]
        public async Task SearchImages_WhenQueryTooLong_ReturnsBadRequest()
        {
            var controller = CreateController();
            var longQuery = new string('a', 201);
            var result = await controller.SearchImages(longQuery);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("200", bad.Value?.ToString());
        }

        [Fact]
        public async Task SearchImages_WhenGoogleNotConfigured_Returns503()
        {
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ImagesPath"] = Path.GetTempPath() })
                .Build();

            var controller = new ImagesController(
                cfg,
                _mockLogger.Object,
                _mockStorage.Object,
                _mockUserContext.Object,
                _mockResizer.Object,
                _mockHttpClientFactory.Object);

            var result = await controller.SearchImages("iron maiden");
            Assert.IsType<ObjectResult>(result);
            var obj = (ObjectResult)result;
            Assert.Equal(503, obj.StatusCode);
        }

        [Fact]
        public async Task SearchImages_WhenGoogleReturnsResults_ReturnsOkWithList()
        {
            // Arrange: mock Google API response
            const string googleJson = """
            {
                "items": [
                    {
                        "title": "Iron Maiden - Killers",
                        "link": "https://example.com/killers.jpg",
                        "image": {
                            "thumbnailLink": "https://example.com/killers_thumb.jpg",
                            "width": 600,
                            "height": 600
                        }
                    }
                ]
            }
            """;

            var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, googleJson);
            var controller = CreateController(httpClient);

            var result = await controller.SearchImages("iron maiden killers");
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<ImageSearchResult>>(ok.Value);
            Assert.Single(list);
            Assert.Equal("Iron Maiden - Killers", list[0].Title);
            Assert.Equal("https://example.com/killers.jpg", list[0].ImageUrl);
        }

        [Fact]
        public async Task SearchImages_WhenGoogleReturnsNoItems_Returns204()
        {
            const string emptyJson = """{ "searchInformation": { "totalResults": "0" } }""";
            var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, emptyJson);
            var controller = CreateController(httpClient);
            var result = await controller.SearchImages("very obscure album");
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task SearchImages_WhenGoogleReturnsError_Returns502()
        {
            var httpClient = CreateMockedHttpClient(HttpStatusCode.Forbidden, "{}");
            var controller = CreateController(httpClient);
            var result = await controller.SearchImages("test query");
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(502, obj.StatusCode);
        }

        // ─── DownloadImage Tests ──────────────────────────────────────────────

        [Fact]
        public async Task DownloadImage_WithGenerateThumbnail_ReturnsThumbnailFields()
        {
            // Arrange: serve a tiny JPEG
            var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
            var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, jpegBytes, "image/jpeg");
            var controller = CreateController(httpClient);

            var request = new ImageDownloadRequest
            {
                Url = "https://example.com/cover.jpg",
                Filename = "iron-maiden.jpg",
            };

            var result = await controller.DownloadImage(request, generateThumbnail: true);
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ImageStoreResult>(ok.Value);
            Assert.NotNull(body.ThumbnailFilename);
            Assert.StartsWith("thumb-", body.ThumbnailFilename);
        }

        [Fact]
        public async Task DownloadImage_WithoutGenerateThumbnail_HasNoThumbnailFields()
        {
            var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
            var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, jpegBytes, "image/jpeg");
            var controller = CreateController(httpClient);

            var request = new ImageDownloadRequest
            {
                Url = "https://example.com/cover.jpg",
                Filename = "iron-maiden.jpg",
            };

            var result = await controller.DownloadImage(request, generateThumbnail: false);
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ImageStoreResult>(ok.Value);
            Assert.Null(body.ThumbnailFilename);
            Assert.Null(body.ThumbnailPublicUrl);
        }

        // ─── Private Helpers ──────────────────────────────────────────────────

        private static HttpClient CreateMockedHttpClient(HttpStatusCode status, string body)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = status,
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                });
            return new HttpClient(handlerMock.Object);
        }

        private static HttpClient CreateMockedHttpClient(HttpStatusCode status, byte[] body, string contentType)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = status,
                    Content = new ByteArrayContent(body) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType) } },
                });
            return new HttpClient(handlerMock.Object);
        }
    }
}
