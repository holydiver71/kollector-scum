using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Repositories
{
    /// <summary>
    /// Tests for UserProfileRepository with focus on image deletion during collection deletion
    /// </summary>
    public class UserProfileRepositoryTests : IDisposable
    {
        private readonly KollectorScumDbContext _context;
        private readonly UserProfileRepository _repository;
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly string _testImagesPath;
        private readonly Guid _testUserId;

        public UserProfileRepositoryTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new KollectorScumDbContext(options);
            _testUserId = Guid.NewGuid();

            // Setup test images directory
            _testImagesPath = Path.Combine(Path.GetTempPath(), $"kollector-test-images-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testImagesPath);
            Directory.CreateDirectory(Path.Combine(_testImagesPath, "covers"));
            Directory.CreateDirectory(Path.Combine(_testImagesPath, "thumbnails"));

            // Setup configuration
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(c => c["ImagesPath"]).Returns(_testImagesPath);
            configuration.Setup(c => c["R2:BucketName"]).Returns("test-bucket");

            // Setup logger
            var logger = new Mock<ILogger<UserProfileRepository>>();

            // Setup storage service
            _mockStorageService = new Mock<IStorageService>();
            _mockStorageService
                .Setup(s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _repository = new UserProfileRepository(_context, configuration.Object, logger.Object, _mockStorageService.Object);
        }

        [Fact]
        public async Task DeleteAllUserMusicReleasesAsync_DeletesReleasesAndImageFiles()
        {
            // Arrange
            var frontCoverFile = "test-front-1.jpg";
            var backCoverFile = "test-back-1.jpg";
            var thumbnailFile = "test-thumb-1.jpg";

            // Create test release with bare-filename image references
            var imageDto = new MusicReleaseImageDto
            {
                CoverFront = frontCoverFile,
                CoverBack = backCoverFile,
                Thumbnail = thumbnailFile
            };

            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _testUserId,
                Images = JsonSerializer.Serialize(imageDto),
                DateAdded = DateTime.UtcNow
            };

            _context.MusicReleases.Add(release);
            // Add some user-owned lookup rows that should be removed when collection is deleted
            _context.Artists.Add(new Artist { Name = "Test Artist", UserId = _testUserId });
            _context.Genres.Add(new Genre { Name = "Test Genre", UserId = _testUserId });
            _context.Labels.Add(new Label { Name = "Test Label", UserId = _testUserId });

            await _context.SaveChangesAsync();

            // Act
            var deletedCount = await _repository.DeleteAllUserMusicReleasesAsync(_testUserId);

            // Assert
            Assert.Equal(1, deletedCount);

            // Verify database record deleted
            var remainingReleases = await _context.MusicReleases.CountAsync(mr => mr.UserId == _testUserId);
            Assert.Equal(0, remainingReleases);

            // Verify each image was deleted via the storage service
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), _testUserId.ToString(), frontCoverFile), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), _testUserId.ToString(), backCoverFile), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), _testUserId.ToString(), thumbnailFile), Times.Once);

            // Verify user-owned lookup rows removed
            var remainingArtists = await _context.Artists.CountAsync(a => a.UserId == _testUserId);
            var remainingGenres = await _context.Genres.CountAsync(g => g.UserId == _testUserId);
            var remainingLabels = await _context.Labels.CountAsync(l => l.UserId == _testUserId);
            Assert.Equal(0, remainingArtists);
            Assert.Equal(0, remainingGenres);
            Assert.Equal(0, remainingLabels);
        }

        [Fact]
        public async Task DeleteAllUserMusicReleasesAsync_WithMultipleReleases_DeletesAllImagesAndReleases()
        {
            // Arrange
            var frontCover1 = "album1-front.jpg";
            var thumbnail1 = "album1-thumb.jpg";
            var frontCover2 = "album2-front.jpg";
            var backCover2 = "album2-back.jpg";

            // Create releases
            var release1 = new MusicRelease
            {
                Id = 1,
                Title = "Album 1",
                UserId = _testUserId,
                Images = JsonSerializer.Serialize(new MusicReleaseImageDto
                {
                    CoverFront = frontCover1,
                    Thumbnail = thumbnail1
                }),
                DateAdded = DateTime.UtcNow
            };

            var release2 = new MusicRelease
            {
                Id = 2,
                Title = "Album 2",
                UserId = _testUserId,
                Images = JsonSerializer.Serialize(new MusicReleaseImageDto
                {
                    CoverFront = frontCover2,
                    CoverBack = backCover2
                }),
                DateAdded = DateTime.UtcNow
            };

            _context.MusicReleases.AddRange(release1, release2);
            await _context.SaveChangesAsync();

            // Act
            var deletedCount = await _repository.DeleteAllUserMusicReleasesAsync(_testUserId);

            // Assert
            Assert.Equal(2, deletedCount);

            // Verify all images were deleted via the storage service
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), _testUserId.ToString(), frontCover1), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), _testUserId.ToString(), thumbnail1), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), _testUserId.ToString(), frontCover2), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), _testUserId.ToString(), backCover2), Times.Once);
        }

        [Fact]
        public async Task DeleteAllUserMusicReleasesAsync_WithMissingImageFiles_DeletesReleasesSuccessfully()
        {
            // Arrange - Release has image metadata but files don't exist
            var imageDto = new MusicReleaseImageDto
            {
                CoverFront = "nonexistent-front.jpg",
                CoverBack = "nonexistent-back.jpg"
            };

            var release = new MusicRelease
            {
                Id = 1,
                Title = "Album with Missing Files",
                UserId = _testUserId,
                Images = JsonSerializer.Serialize(imageDto),
                DateAdded = DateTime.UtcNow
            };

            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            // Act
            var deletedCount = await _repository.DeleteAllUserMusicReleasesAsync(_testUserId);

            // Assert
            Assert.Equal(1, deletedCount);
            var remainingReleases = await _context.MusicReleases.CountAsync(mr => mr.UserId == _testUserId);
            Assert.Equal(0, remainingReleases);
        }

        [Fact]
        public async Task DeleteAllUserMusicReleasesAsync_WithEmptyCollection_ReturnsZero()
        {
            // Arrange - No releases in database

            // Act
            var deletedCount = await _repository.DeleteAllUserMusicReleasesAsync(_testUserId);

            // Assert
            Assert.Equal(0, deletedCount);
        }

        [Fact]
        public async Task DeleteAllUserMusicReleasesAsync_OnlyDeletesSpecifiedUserReleases()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            
            var userRelease = new MusicRelease
            {
                Id = 1,
                Title = "User Album",
                UserId = _testUserId,
                DateAdded = DateTime.UtcNow
            };

            var otherUserRelease = new MusicRelease
            {
                Id = 2,
                Title = "Other User Album",
                UserId = otherUserId,
                DateAdded = DateTime.UtcNow
            };

            // Add lookup rows for both users to verify only the specified user's
            // lookup rows are deleted.
            _context.Artists.Add(new Artist { Name = "User Artist", UserId = _testUserId });
            _context.Genres.Add(new Genre { Name = "User Genre", UserId = _testUserId });
            _context.Labels.Add(new Label { Name = "User Label", UserId = _testUserId });

            _context.Artists.Add(new Artist { Name = "Other Artist", UserId = otherUserId });
            _context.Genres.Add(new Genre { Name = "Other Genre", UserId = otherUserId });
            _context.Labels.Add(new Label { Name = "Other Label", UserId = otherUserId });

            _context.MusicReleases.AddRange(userRelease, otherUserRelease);
            await _context.SaveChangesAsync();

            // Act
            var deletedCount = await _repository.DeleteAllUserMusicReleasesAsync(_testUserId);

            // Assert
            Assert.Equal(1, deletedCount);
            
            // Verify only test user's release was deleted
            var userReleases = await _context.MusicReleases.CountAsync(mr => mr.UserId == _testUserId);
            var otherReleases = await _context.MusicReleases.CountAsync(mr => mr.UserId == otherUserId);
            Assert.Equal(0, userReleases);
            Assert.Equal(1, otherReleases);

            // Verify lookup rows: user's lookups removed, other user's remain
            var userArtists = await _context.Artists.CountAsync(a => a.UserId == _testUserId);
            var otherArtists = await _context.Artists.CountAsync(a => a.UserId == otherUserId);
            var userGenres = await _context.Genres.CountAsync(g => g.UserId == _testUserId);
            var otherGenres = await _context.Genres.CountAsync(g => g.UserId == otherUserId);
            var userLabels = await _context.Labels.CountAsync(l => l.UserId == _testUserId);
            var otherLabels = await _context.Labels.CountAsync(l => l.UserId == otherUserId);

            Assert.Equal(0, userArtists);
            Assert.Equal(1, otherArtists);
            Assert.Equal(0, userGenres);
            Assert.Equal(1, otherGenres);
            Assert.Equal(0, userLabels);
            Assert.Equal(1, otherLabels);
        }

        [Fact]
        public async Task DeleteAllUserMusicReleasesAsync_WithLocalRelativePathImages_CallsStorageServiceDelete()
        {
            // Arrange – images stored locally with relative URL format: /{bucket}/{userId}/{filename}
            var userId = _testUserId;
            var frontCoverUrl = $"/cover-art-staging/{userId}/front-cover.jpg";
            var thumbnailUrl = $"/cover-art-staging/{userId}/thumbnail.jpg";

            var release = new MusicRelease
            {
                Id = 98,
                Title = "Local Album",
                UserId = userId,
                Images = JsonSerializer.Serialize(new MusicReleaseImageDto
                {
                    CoverFront = frontCoverUrl,
                    Thumbnail = thumbnailUrl
                }),
                DateAdded = DateTime.UtcNow
            };

            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            // Act
            var deletedCount = await _repository.DeleteAllUserMusicReleasesAsync(userId);

            // Assert
            Assert.Equal(1, deletedCount);

            // IStorageService.DeleteFileAsync should have been called with the parsed bucket and filename
            _mockStorageService.Verify(
                s => s.DeleteFileAsync("cover-art-staging", userId.ToString(), "front-cover.jpg"),
                Times.Once);
            _mockStorageService.Verify(
                s => s.DeleteFileAsync("cover-art-staging", userId.ToString(), "thumbnail.jpg"),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAllUserMusicReleasesAsync_WithR2Images_CallsStorageServiceDelete()
        {
            // Arrange – images stored in Cloudflare R2 (HTTPS URLs)
            var userId = _testUserId;
            var frontCoverUrl = $"https://pub-example.r2.dev/{userId}/front-cover.jpg";
            var thumbnailUrl = $"https://pub-example.r2.dev/{userId}/thumbnail.jpg";

            var release = new MusicRelease
            {
                Id = 99,
                Title = "Cloud Album",
                UserId = userId,
                Images = JsonSerializer.Serialize(new MusicReleaseImageDto
                {
                    CoverFront = frontCoverUrl,
                    Thumbnail = thumbnailUrl
                }),
                DateAdded = DateTime.UtcNow
            };

            _context.MusicReleases.Add(release);
            await _context.SaveChangesAsync();

            // Act
            var deletedCount = await _repository.DeleteAllUserMusicReleasesAsync(userId);

            // Assert
            Assert.Equal(1, deletedCount);

            // IStorageService.DeleteFileAsync should have been called once per image
            _mockStorageService.Verify(
                s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>(), "front-cover.jpg"),
                Times.Once);
            _mockStorageService.Verify(
                s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>(), "thumbnail.jpg"),
                Times.Once);
        }

        [Fact]
        public async Task GetUserMusicReleaseCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var release1 = new MusicRelease { Id = 1, Title = "Album 1", UserId = _testUserId, DateAdded = DateTime.UtcNow };
            var release2 = new MusicRelease { Id = 2, Title = "Album 2", UserId = _testUserId, DateAdded = DateTime.UtcNow };
            var release3 = new MusicRelease { Id = 3, Title = "Album 3", UserId = Guid.NewGuid(), DateAdded = DateTime.UtcNow };

            _context.MusicReleases.AddRange(release1, release2, release3);
            await _context.SaveChangesAsync();

            // Act
            var count = await _repository.GetUserMusicReleaseCountAsync(_testUserId);

            // Assert
            Assert.Equal(2, count);
        }

        public void Dispose()
        {
            // Clean up test images directory
            if (Directory.Exists(_testImagesPath))
            {
                Directory.Delete(_testImagesPath, true);
            }

            _context.Dispose();
        }
    }
}
