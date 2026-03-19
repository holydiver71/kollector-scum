using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Unit tests for the image upload and search endpoints in <see cref="ImagesController"/>.
    /// </summary>
    public class ImagesControllerImageTests
    {
        private readonly Mock<IConfiguration> _mockConfig = new();
        private readonly Mock<ILogger<ImagesController>> _mockLogger = new();
        private readonly Mock<IStorageService> _mockStorage = new();
        private readonly Mock<IUserContext> _mockUserContext = new();
        private readonly Mock<IImageResizerService> _mockResizer = new();
        private readonly Mock<ICoverArtSearchService> _mockSearch = new();
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();

        public ImagesControllerImageTests()
        {
            _mockConfig.Setup(c => c["ImagesPath"]).Returns("/tmp/images");
            _mockConfig.Setup(c => c["R2:BucketName"]).Returns("test-bucket");
            _mockUserContext.Setup(u => u.GetActingUserId()).Returns(Guid.NewGuid());
        }

        private ImagesController CreateController()
        {
            var controller = new ImagesController(
                _mockConfig.Object,
                _mockLogger.Object,
                _mockStorage.Object,
                _mockUserContext.Object,
                _mockResizer.Object,
                _mockSearch.Object,
                _mockHttpClientFactory.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            };
            return controller;
        }

        private static IFormFile CreateFormFile(string contentType, long size = 100)
        {
            var bytes = new byte[size];
            var stream = new MemoryStream(bytes);
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns("cover.jpg");
            mock.Setup(f => f.Length).Returns(size);
            mock.Setup(f => f.ContentType).Returns(contentType);
            mock.Setup(f => f.OpenReadStream()).Returns(stream);
            return mock.Object;
        }

        // ─── UploadImage ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task UploadImage_NoFile_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.UploadImage(generateThumbnail: false, file: null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadImage_EmptyFile_ReturnsBadRequest()
        {
            var controller = CreateController();
            var file = CreateFormFile("image/jpeg", size: 0);
            var result = await controller.UploadImage(generateThumbnail: false, file: file);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadImage_FileTooLarge_ReturnsBadRequest()
        {
            var controller = CreateController();
            var file = CreateFormFile("image/jpeg", size: 6 * 1024 * 1024); // 6 MB
            var result = await controller.UploadImage(generateThumbnail: false, file: file);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadImage_InvalidMimeType_ReturnsBadRequest()
        {
            var controller = CreateController();
            var file = CreateFormFile("application/pdf");
            var result = await controller.UploadImage(generateThumbnail: false, file: file);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadImage_ValidFile_ReturnsOkWithFilenames()
        {
            var resizedStream = new MemoryStream(new byte[500]);
            var thumbStream = new MemoryStream(new byte[100]);

            _mockResizer.Setup(r => r.ResizeAsync(It.IsAny<Stream>(), 1600))
                .ReturnsAsync(resizedStream);
            _mockResizer.Setup(r => r.GenerateThumbnailAsync(It.IsAny<Stream>(), 300))
                .ReturnsAsync(thumbStream);
            _mockStorage.Setup(s => s.UploadFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("https://cdn.example.com/file.jpg");

            var controller = CreateController();
            var file = CreateFormFile("image/jpeg", size: 1024);

            var result = await controller.UploadImage(generateThumbnail: true, file: file);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<ImageUploadResponseDto>(ok.Value);
            Assert.NotEmpty(dto.Filename);
            Assert.NotNull(dto.ThumbnailFilename);
        }

        [Fact]
        public async Task UploadImage_WithoutThumbnail_ThumbnailFilenameIsNull()
        {
            var resizedStream = new MemoryStream(new byte[500]);
            _mockResizer.Setup(r => r.ResizeAsync(It.IsAny<Stream>(), 1600))
                .ReturnsAsync(resizedStream);
            _mockStorage.Setup(s => s.UploadFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("https://cdn.example.com/file.jpg");

            var controller = CreateController();
            var file = CreateFormFile("image/jpeg", size: 1024);

            var result = await controller.UploadImage(generateThumbnail: false, file: file);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<ImageUploadResponseDto>(ok.Value);
            Assert.Null(dto.ThumbnailFilename);
        }

        // ─── SearchCoverArt ───────────────────────────────────────────────────────────

        [Fact]
        public async Task SearchCoverArt_MissingQuery_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.SearchCoverArt(q: null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SearchCoverArt_QueryTooLong_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.SearchCoverArt(q: new string('x', 201));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SearchCoverArt_InvalidLimit_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.SearchCoverArt(q: "iron maiden", limit: 0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SearchCoverArt_NoResults_ReturnsNoContent()
        {
            _mockSearch.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<CoverArtSearchResultDto>());

            var controller = CreateController();
            var result = await controller.SearchCoverArt(q: "unknown album xyz");
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task SearchCoverArt_WithResults_ReturnsOkWithList()
        {
            var expected = new List<CoverArtSearchResultDto>
            {
                new() { MbId = "mbid-1", Title = "Killers", Artist = "Iron Maiden", Confidence = 1.0 },
            };
            _mockSearch.Setup(s => s.SearchAsync("iron maiden killers", null, 4, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.SearchCoverArt(q: "iron maiden killers");

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IReadOnlyList<CoverArtSearchResultDto>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task SearchCoverArt_WithCatalogueNumber_PassesCatalogueNumberToService()
        {
            var expected = new List<CoverArtSearchResultDto>
            {
                new() { Title = "Album", Artist = "Artist", CatalogueNumber = "CAT001", Confidence = 0.95 },
            };
            _mockSearch.Setup(s => s.SearchAsync("artist album", "CAT001", 4, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.SearchCoverArt(q: "artist album", catalogueNumber: "CAT001");

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IReadOnlyList<CoverArtSearchResultDto>>(ok.Value);
            Assert.Single(list);
            Assert.Equal("CAT001", list.First().CatalogueNumber);
        }

        [Fact]
        public async Task SearchCoverArt_CatalogueNumberTooLong_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.SearchCoverArt(q: "test", catalogueNumber: new string('x', 51));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #region ProxyImage Tests

        /// <summary>
        /// Builds a mock <see cref="HttpClient"/> backed by a handler that returns the supplied response.
        /// </summary>
        private static HttpClient BuildMockHttpClient(HttpResponseMessage response)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            return new HttpClient(handler.Object);
        }

        [Fact]
        public async Task ProxyImage_MissingUrl_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.ProxyImage(url: "");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ProxyImage_NonDiscogsUrl_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.ProxyImage(url: "https://evil.example.com/image.jpg");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ProxyImage_HttpDiscogsUrl_ReturnsBadRequest()
        {
            // Must be HTTPS – plain HTTP should be rejected.
            var controller = CreateController();
            var result = await controller.ProxyImage(url: "http://i.discogs.com/image.jpg");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ProxyImage_ValidDiscogsUrl_ReturnsImageBytes()
        {
            var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG magic bytes
            var upstreamResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(imageBytes),
            };
            upstreamResponse.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            var client = BuildMockHttpClient(upstreamResponse);
            _mockHttpClientFactory
                .Setup(f => f.CreateClient(ImagesController.ImageDownloadClientName))
                .Returns(client);

            var controller = CreateController();
            var result = await controller.ProxyImage(url: "https://i.discogs.com/abc/cover.jpg");

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("image/jpeg", fileResult.ContentType);
            Assert.Equal(imageBytes, fileResult.FileContents);
        }

        [Fact]
        public async Task ProxyImage_UpstreamReturnsNotFound_Returns502()
        {
            var upstreamResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

            var client = BuildMockHttpClient(upstreamResponse);
            _mockHttpClientFactory
                .Setup(f => f.CreateClient(ImagesController.ImageDownloadClientName))
                .Returns(client);

            var controller = CreateController();
            var result = await controller.ProxyImage(url: "https://i.discogs.com/abc/cover.jpg");

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status502BadGateway, statusResult.StatusCode);
        }

        [Fact]
        public async Task ProxyImage_UpstreamReturnsNonImageContentType_ReturnsBadRequest()
        {
            var upstreamResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<script>alert('xss')</script>"),
            };
            upstreamResponse.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");

            var client = BuildMockHttpClient(upstreamResponse);
            _mockHttpClientFactory
                .Setup(f => f.CreateClient(ImagesController.ImageDownloadClientName))
                .Returns(client);

            var controller = CreateController();
            var result = await controller.ProxyImage(url: "https://i.discogs.com/abc/cover.jpg");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ProxyImage_HttpRequestException_Returns502()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network failure"));

            var client = new HttpClient(handler.Object);
            _mockHttpClientFactory
                .Setup(f => f.CreateClient(ImagesController.ImageDownloadClientName))
                .Returns(client);

            var controller = CreateController();
            var result = await controller.ProxyImage(url: "https://i.discogs.com/abc/cover.jpg");

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status502BadGateway, statusResult.StatusCode);
        }

        #endregion
    }
}
