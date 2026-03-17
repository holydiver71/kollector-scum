using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
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
            _mockSearch.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
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
            _mockSearch.Setup(s => s.SearchAsync("iron maiden killers", 4, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.SearchCoverArt(q: "iron maiden killers");

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IReadOnlyList<CoverArtSearchResultDto>>(ok.Value);
            Assert.Single(list);
        }
    }
}
