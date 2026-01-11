using System.Security.Claims;
using System.Text;
using System.Text.Json;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Integration tests for AdminController's migration endpoint.
    /// Tests path safety, error handling, and migration logic.
    /// </summary>
    public class AdminControllerMigrationTests : IDisposable
    {
        private readonly KollectorScumDbContext _context;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IUserInvitationRepository> _mockInvitationRepository;
        private readonly Mock<ILogger<AdminController>> _mockLogger;
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AdminController _controller;
        private readonly Guid _adminUserId = Guid.NewGuid();
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly string _testImagesPath;

        public AdminControllerMigrationTests()
        {
            // Create in-memory database
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _context = new KollectorScumDbContext(options);

            // Create test images directory
            _testImagesPath = Path.Combine(Path.GetTempPath(), $"test_images_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testImagesPath);
            Directory.CreateDirectory(Path.Combine(_testImagesPath, "covers"));

            // Setup mocks
            _mockUserRepository = new Mock<IUserRepository>();
            _mockInvitationRepository = new Mock<IUserInvitationRepository>();
            _mockLogger = new Mock<ILogger<AdminController>>();
            _mockStorageService = new Mock<IStorageService>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Configure mocks
            _mockConfiguration.Setup(c => c["ImagesPath"]).Returns(_testImagesPath);
            _mockEnvironment.Setup(e => e.WebRootPath).Returns(Path.Combine(_testImagesPath, "wwwroot"));

            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@test.com",
                IsAdmin = true
            };
            _mockUserRepository.Setup(x => x.FindByIdAsync(_adminUserId)).ReturnsAsync(adminUser);

            // Create controller
            _controller = new AdminController(
                _mockUserRepository.Object,
                _mockInvitationRepository.Object,
                _mockLogger.Object,
                _context,
                _mockStorageService.Object,
                _mockEnvironment.Object,
                _mockConfiguration.Object
            );

            // Set up authenticated admin user
            SetupAdminUser();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            if (Directory.Exists(_testImagesPath))
            {
                Directory.Delete(_testImagesPath, recursive: true);
            }
        }

        private void SetupAdminUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _adminUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private void SetupNonAdminUser()
        {
            var nonAdminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@test.com",
                IsAdmin = false
            };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, nonAdminUser.Id.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _mockUserRepository.Setup(x => x.FindByIdAsync(nonAdminUser.Id)).ReturnsAsync(nonAdminUser);
        }

        #region Authorization Tests

        [Fact]
        public async Task MigrateLocalStorage_AsNonAdmin_ReturnsForbid()
        {
            // Arrange
            SetupNonAdminUser();

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task MigrateLocalStorage_AsAdmin_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        #endregion

        #region Migration Logic Tests

        [Fact]
        public async Task MigrateLocalStorage_WithNoReleases_ReturnsZeroMigrated()
        {
            // Arrange
            // No releases in database

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            Assert.NotNull(response);
            
            var json = JsonSerializer.Serialize(response);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal(0, responseObj.GetProperty("TotalConsidered").GetInt32());
            Assert.Equal(0, responseObj.GetProperty("MigratedCount").GetInt32());
        }

        [Fact]
        public async Task MigrateLocalStorage_WithValidRelease_MigratesSuccessfully()
        {
            // Arrange
            var fileName = "test-cover.jpg";
            var sourceFilePath = Path.Combine(_testImagesPath, "covers", fileName);
            await File.WriteAllTextAsync(sourceFilePath, "fake image data");

            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = $"{{\"CoverFront\":\"{fileName}\"}}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            var newUrl = $"/cover-art/{_testUserId}/unique-filename.jpg";
            _mockStorageService
                .Setup(s => s.UploadFileAsync("cover-art", _testUserId.ToString(), fileName, It.IsAny<Stream>(), "image/jpeg"))
                .ReturnsAsync(newUrl);

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal(1, responseObj.GetProperty("MigratedCount").GetInt32());
            Assert.Equal(0, responseObj.GetProperty("SkippedCount").GetInt32());

            // Verify database was updated
            var updatedRelease = await _context.MusicReleases.FindAsync(1);
            Assert.NotNull(updatedRelease);
            Assert.Contains(newUrl, updatedRelease.Images);

            // Verify storage service was called
            _mockStorageService.Verify(
                s => s.UploadFileAsync("cover-art", _testUserId.ToString(), fileName, It.IsAny<Stream>(), "image/jpeg"),
                Times.Once);
        }

        [Fact]
        public async Task MigrateLocalStorage_WithMissingFile_SkipsRelease()
        {
            // Arrange
            var fileName = "nonexistent.jpg";
            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = $"{{\"CoverFront\":\"{fileName}\"}}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal(0, responseObj.GetProperty("MigratedCount").GetInt32());
            Assert.Equal(1, responseObj.GetProperty("SkippedCount").GetInt32());

            // Verify storage service was not called
            _mockStorageService.Verify(
                s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task MigrateLocalStorage_WithEmptyUserId_SkipsRelease()
        {
            // Arrange
            var fileName = "test-cover.jpg";
            var sourceFilePath = Path.Combine(_testImagesPath, "covers", fileName);
            await File.WriteAllTextAsync(sourceFilePath, "fake image data");

            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = Guid.Empty, // Invalid user ID
                Images = $"{{\"CoverFront\":\"{fileName}\"}}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal(0, responseObj.GetProperty("MigratedCount").GetInt32());
            Assert.Equal(1, responseObj.GetProperty("SkippedCount").GetInt32());

            // Verify storage service was not called
            _mockStorageService.Verify(
                s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task MigrateLocalStorage_WithAlreadyMigratedUrl_SkipsRelease()
        {
            // Arrange
            var migratedUrl = $"/cover-art/{_testUserId}/already-migrated.jpg";
            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = $"{{\"CoverFront\":\"{migratedUrl}\"}}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal(0, responseObj.GetProperty("MigratedCount").GetInt32());

            // Verify storage service was not called
            _mockStorageService.Verify(
                s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()),
                Times.Never);
        }

        #endregion

        #region Path Safety Tests

        [Theory]
        [InlineData("../../../etc/passwd")]
        [InlineData("../../sneaky.jpg")]
        [InlineData("subdir/../../../windows/system32/config/sam")]
        public async Task MigrateLocalStorage_WithDirectoryTraversalInFilename_HandledSafely(string maliciousFileName)
        {
            // Arrange
            // Create a file with a safe name that we'll reference with the malicious path
            var safeFileName = "safe.jpg";
            var sourceFilePath = Path.Combine(_testImagesPath, "covers", safeFileName);
            await File.WriteAllTextAsync(sourceFilePath, "fake image data");

            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = $"{{\"CoverFront\":\"{maliciousFileName}\"}}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            // Should skip due to file not found or path validation
            Assert.Equal(0, responseObj.GetProperty("MigratedCount").GetInt32());
            Assert.Equal(1, responseObj.GetProperty("SkippedCount").GetInt32());
        }

        [Theory]
        [InlineData("http://evil.com/malware.jpg")]
        [InlineData("https://example.com/image.jpg")]
        [InlineData("/absolute/path/image.jpg")]
        public async Task MigrateLocalStorage_WithExternalOrAbsolutePath_SkipsRelease(string suspiciousPath)
        {
            // Arrange
            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = $"{{\"CoverFront\":\"{suspiciousPath}\"}}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal(0, responseObj.GetProperty("MigratedCount").GetInt32());
            Assert.Equal(1, responseObj.GetProperty("SkippedCount").GetInt32());
        }

        #endregion

        #region Extension Handling Tests

        [Theory]
        [InlineData("image.jpg", "image/jpeg")]
        [InlineData("image.jpeg", "image/jpeg")]
        [InlineData("image.png", "image/png")]
        [InlineData("image.webp", "image/webp")]
        [InlineData("image.gif", "image/gif")]
        [InlineData("image.JPG", "image/jpeg")] // Case insensitive
        public async Task MigrateLocalStorage_WithVariousExtensions_UsesCorrectContentType(string fileName, string expectedContentType)
        {
            // Arrange
            var sourceFilePath = Path.Combine(_testImagesPath, "covers", fileName);
            await File.WriteAllTextAsync(sourceFilePath, "fake image data");

            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = $"{{\"CoverFront\":\"{fileName}\"}}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            var newUrl = $"/cover-art/{_testUserId}/unique-filename{Path.GetExtension(fileName)}";
            _mockStorageService
                .Setup(s => s.UploadFileAsync("cover-art", _testUserId.ToString(), fileName, It.IsAny<Stream>(), expectedContentType))
                .ReturnsAsync(newUrl);

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            _mockStorageService.Verify(
                s => s.UploadFileAsync("cover-art", _testUserId.ToString(), fileName, It.IsAny<Stream>(), expectedContentType),
                Times.Once);
        }

        [Fact]
        public async Task MigrateLocalStorage_WithUnknownExtension_UsesJpegDefault()
        {
            // Arrange
            var fileName = "image.bmp"; // Unsupported extension
            var sourceFilePath = Path.Combine(_testImagesPath, "covers", fileName);
            await File.WriteAllTextAsync(sourceFilePath, "fake image data");

            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = $"{{\"CoverFront\":\"{fileName}\"}}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            var newUrl = $"/cover-art/{_testUserId}/unique-filename.bmp";
            _mockStorageService
                .Setup(s => s.UploadFileAsync("cover-art", _testUserId.ToString(), fileName, It.IsAny<Stream>(), "image/jpeg"))
                .ReturnsAsync(newUrl);

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert - Should default to image/jpeg
            _mockStorageService.Verify(
                s => s.UploadFileAsync("cover-art", _testUserId.ToString(), fileName, It.IsAny<Stream>(), "image/jpeg"),
                Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task MigrateLocalStorage_WhenStorageServiceThrows_RecordsError()
        {
            // Arrange
            var fileName = "test-cover.jpg";
            var sourceFilePath = Path.Combine(_testImagesPath, "covers", fileName);
            await File.WriteAllTextAsync(sourceFilePath, "fake image data");

            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = $"{{\"CoverFront\":\"{fileName}\"}}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            _mockStorageService
                .Setup(s => s.UploadFileAsync("cover-art", _testUserId.ToString(), fileName, It.IsAny<Stream>(), "image/jpeg"))
                .ThrowsAsync(new IOException("Disk full"));

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal(0, responseObj.GetProperty("MigratedCount").GetInt32());
            Assert.Equal(1, responseObj.GetProperty("ErrorCount").GetInt32());
            
            var errors = responseObj.GetProperty("Errors");
            Assert.True(errors.GetArrayLength() > 0);
        }

        [Fact]
        public async Task MigrateLocalStorage_WithMultipleReleases_MigratesAllValid()
        {
            // Arrange
            var file1 = "cover1.jpg";
            var file2 = "cover2.jpg";
            var file3 = "cover3.jpg";
            
            await File.WriteAllTextAsync(Path.Combine(_testImagesPath, "covers", file1), "fake image 1");
            await File.WriteAllTextAsync(Path.Combine(_testImagesPath, "covers", file2), "fake image 2");
            await File.WriteAllTextAsync(Path.Combine(_testImagesPath, "covers", file3), "fake image 3");

            var releases = new List<MusicRelease>
            {
                new MusicRelease { Id = 1, Title = "Album 1", UserId = _testUserId, Images = $"{{\"CoverFront\":\"{file1}\"}}" },
                new MusicRelease { Id = 2, Title = "Album 2", UserId = _testUserId, Images = $"{{\"CoverFront\":\"{file2}\"}}" },
                new MusicRelease { Id = 3, Title = "Album 3", UserId = _testUserId, Images = $"{{\"CoverFront\":\"{file3}\"}}" }
            };
            _context.MusicReleases.AddRange(releases);
            await _context.SaveChangesAsync();

            _mockStorageService
                .Setup(s => s.UploadFileAsync("cover-art", _testUserId.ToString(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync((string bucket, string userId, string fileName, Stream stream, string contentType) =>
                    $"/cover-art/{userId}/{Guid.NewGuid()}{Path.GetExtension(fileName)}");

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal(3, responseObj.GetProperty("MigratedCount").GetInt32());
            Assert.Equal(0, responseObj.GetProperty("SkippedCount").GetInt32());
            Assert.Equal(0, responseObj.GetProperty("ErrorCount").GetInt32());

            // Verify all releases were updated
            var updatedReleases = await _context.MusicReleases.ToListAsync();
            foreach (var release in updatedReleases)
            {
                Assert.Contains("/cover-art/", release.Images);
            }
        }

        #endregion

        #region JSON Handling Tests

        [Fact]
        public async Task MigrateLocalStorage_WithNullCoverFront_SkipsRelease()
        {
            // Arrange
            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = "{\"CoverFront\":null}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal(0, responseObj.GetProperty("MigratedCount").GetInt32());
        }

        [Fact]
        public async Task MigrateLocalStorage_WithEmptyCoverFront_SkipsRelease()
        {
            // Arrange
            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = "{\"CoverFront\":\"\"}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal(0, responseObj.GetProperty("MigratedCount").GetInt32());
        }

        [Fact]
        public async Task MigrateLocalStorage_WithMissingCoverFrontKey_SkipsRelease()
        {
            // Arrange
            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = "{\"CoverBack\":\"back.jpg\"}"
            };
            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MigrateLocalStorage();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal(0, responseObj.GetProperty("TotalConsidered").GetInt32());
        }

        #endregion
    }
}
