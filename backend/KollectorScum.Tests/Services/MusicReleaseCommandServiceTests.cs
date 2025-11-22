using System.Text.Json;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for MusicReleaseCommandService
    /// Tests for Create, Update, and Delete operations including image file handling
    /// </summary>
    public class MusicReleaseCommandServiceTests : IDisposable
    {
        private readonly Mock<IRepository<MusicRelease>> _mockMusicReleaseRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IEntityResolverService> _mockEntityResolver;
        private readonly Mock<IMusicReleaseMapperService> _mockMapper;
        private readonly Mock<IMusicReleaseValidator> _mockValidator;
        private readonly Mock<ILogger<MusicReleaseCommandService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly MusicReleaseCommandService _service;
        private readonly string _testImagesPath;

        public MusicReleaseCommandServiceTests()
        {
            _mockMusicReleaseRepo = new Mock<IRepository<MusicRelease>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEntityResolver = new Mock<IEntityResolverService>();
            _mockMapper = new Mock<IMusicReleaseMapperService>();
            _mockValidator = new Mock<IMusicReleaseValidator>();
            _mockLogger = new Mock<ILogger<MusicReleaseCommandService>>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Setup test directory for images
            _testImagesPath = Path.Combine(Path.GetTempPath(), "kollector-test-images", Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(_testImagesPath, "covers"));
            Directory.CreateDirectory(Path.Combine(_testImagesPath, "thumbnails"));

            // Setup configuration to use test path
            _mockConfiguration.Setup(c => c["ImagesPath"]).Returns(_testImagesPath);

            _service = new MusicReleaseCommandService(
                _mockMusicReleaseRepo.Object,
                _mockUnitOfWork.Object,
                _mockEntityResolver.Object,
                _mockMapper.Object,
                _mockValidator.Object,
                _mockLogger.Object,
                _mockConfiguration.Object
            );
        }

        public void Dispose()
        {
            // Cleanup test directory
            if (Directory.Exists(_testImagesPath))
            {
                Directory.Delete(_testImagesPath, true);
            }
        }

        #region DeleteMusicReleaseAsync Tests

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithValidId_DeletesReleaseAndImages()
        {
            // Arrange
            var coversPath = Path.Combine(_testImagesPath, "covers");
            var thumbnailsPath = Path.Combine(_testImagesPath, "thumbnails");
            var frontCoverFile = "test-front.jpg";
            var backCoverFile = "test-back.jpg";
            var thumbnailFile = "test-thumb.jpg";

            // Create test image files
            File.WriteAllText(Path.Combine(coversPath, frontCoverFile), "test front cover");
            File.WriteAllText(Path.Combine(coversPath, backCoverFile), "test back cover");
            File.WriteAllText(Path.Combine(thumbnailsPath, thumbnailFile), "test thumbnail");

            var imageDto = new MusicReleaseImageDto
            {
                CoverFront = frontCoverFile,
                CoverBack = backCoverFile,
                Thumbnail = thumbnailFile
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album to Delete",
                Images = JsonSerializer.Serialize(imageDto)
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Verify files exist before deletion
            Assert.True(File.Exists(Path.Combine(coversPath, frontCoverFile)));
            Assert.True(File.Exists(Path.Combine(coversPath, backCoverFile)));
            Assert.True(File.Exists(Path.Combine(thumbnailsPath, thumbnailFile)));

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
            
            // Verify database operations
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);

            // Verify image files were deleted from correct folders
            Assert.False(File.Exists(Path.Combine(coversPath, frontCoverFile)));
            Assert.False(File.Exists(Path.Combine(coversPath, backCoverFile)));
            Assert.False(File.Exists(Path.Combine(thumbnailsPath, thumbnailFile)));
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithNoImages_DeletesReleaseSuccessfully()
        {
            // Arrange
            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album without Images",
                Images = null
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithPartialImages_DeletesExistingImages()
        {
            // Arrange
            var coversPath = Path.Combine(_testImagesPath, "covers");
            var thumbnailsPath = Path.Combine(_testImagesPath, "thumbnails");
            var frontCoverFile = "test-front-only.jpg";

            // Create only front cover file
            File.WriteAllText(Path.Combine(coversPath, frontCoverFile), "test front cover");

            var imageDto = new MusicReleaseImageDto
            {
                CoverFront = frontCoverFile,
                CoverBack = null,  // No back cover
                Thumbnail = null   // No thumbnail
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album with Partial Images",
                Images = JsonSerializer.Serialize(imageDto)
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Verify file exists before deletion
            Assert.True(File.Exists(Path.Combine(coversPath, frontCoverFile)));

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
            
            // Verify image file was deleted
            Assert.False(File.Exists(Path.Combine(coversPath, frontCoverFile)));
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithMissingImageFiles_DeletesReleaseSuccessfully()
        {
            // Arrange - Release has image metadata but files don't exist
            var imageDto = new MusicReleaseImageDto
            {
                CoverFront = "nonexistent-front.jpg",
                CoverBack = "nonexistent-back.jpg",
                Thumbnail = "nonexistent-thumb.jpg"
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album with Missing Image Files",
                Images = JsonSerializer.Serialize(imageDto)
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert - Should succeed even though files don't exist
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithInvalidImageJson_DeletesReleaseSuccessfully()
        {
            // Arrange - Release has invalid JSON in Images field
            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album with Invalid Image JSON",
                Images = "invalid json {]"
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert - Should succeed even with invalid JSON
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithNonexistentId_ReturnsNotFound()
        {
            // Arrange
            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((MusicRelease?)null);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(999);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WhenDatabaseThrowsException_ReturnsFailure()
        {
            // Arrange
            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album to Delete"
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.DatabaseError, result.ErrorType);
            Assert.Contains("Database error", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithLockedImageFile_StillDeletesRelease()
        {
            // Arrange
            var coversPath = Path.Combine(_testImagesPath, "covers");
            var frontCoverFile = "locked-file.jpg";
            var fullPath = Path.Combine(coversPath, frontCoverFile);

            // Create and lock a file
            File.WriteAllText(fullPath, "locked content");
            FileStream? lockStream = null;

            try
            {
                lockStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.None);

                var imageDto = new MusicReleaseImageDto
                {
                    CoverFront = frontCoverFile
                };

                var existingRelease = new MusicRelease
                {
                    Id = 1,
                    Title = "Album with Locked Image",
                    Images = JsonSerializer.Serialize(imageDto)
                };

                _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync(existingRelease);
                _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
                _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

                // Act
                var result = await _service.DeleteMusicReleaseAsync(1);

                // Assert - Should succeed even if image deletion fails
                Assert.True(result.IsSuccess);
                Assert.True(result.Value);
                _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Once);
                _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            }
            finally
            {
                // Cleanup: release the lock
                lockStream?.Dispose();
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithAllThreeImageTypes_DeletesAllFiles()
        {
            // Arrange
            var coversPath = Path.Combine(_testImagesPath, "covers");
            var thumbnailsPath = Path.Combine(_testImagesPath, "thumbnails");
            var frontCoverFile = "album-front.jpg";
            var backCoverFile = "album-back.jpg";
            var thumbnailFile = "album-thumb.jpg";

            // Create all three image files
            File.WriteAllText(Path.Combine(coversPath, frontCoverFile), "front");
            File.WriteAllText(Path.Combine(coversPath, backCoverFile), "back");
            File.WriteAllText(Path.Combine(thumbnailsPath, thumbnailFile), "thumb");

            var imageDto = new MusicReleaseImageDto
            {
                CoverFront = frontCoverFile,
                CoverBack = backCoverFile,
                Thumbnail = thumbnailFile
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Complete Album",
                Images = JsonSerializer.Serialize(imageDto)
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert
            Assert.True(result.IsSuccess);
            
            // Verify all three files were deleted from correct locations
            Assert.False(File.Exists(Path.Combine(coversPath, frontCoverFile)), "Front cover should be deleted from covers folder");
            Assert.False(File.Exists(Path.Combine(coversPath, backCoverFile)), "Back cover should be deleted from covers folder");
            Assert.False(File.Exists(Path.Combine(thumbnailsPath, thumbnailFile)), "Thumbnail should be deleted from thumbnails folder");
        }

        #endregion
    }
}
