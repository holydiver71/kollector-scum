using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
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

            // Setup logger
            var logger = new Mock<ILogger<UserProfileRepository>>();

            _repository = new UserProfileRepository(_context, configuration.Object, logger.Object);
        }

        [Fact]
        public async Task DeleteAllUserMusicReleasesAsync_DeletesReleasesAndImageFiles()
        {
            // Arrange
            var coversPath = Path.Combine(_testImagesPath, "covers");
            var thumbnailsPath = Path.Combine(_testImagesPath, "thumbnails");
            
            var frontCoverFile = "test-front-1.jpg";
            var backCoverFile = "test-back-1.jpg";
            var thumbnailFile = "test-thumb-1.jpg";

            // Create test image files
            File.WriteAllText(Path.Combine(coversPath, frontCoverFile), "front cover content");
            File.WriteAllText(Path.Combine(coversPath, backCoverFile), "back cover content");
            File.WriteAllText(Path.Combine(thumbnailsPath, thumbnailFile), "thumbnail content");

            // Create test release with images
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
            await _context.SaveChangesAsync();

            // Verify files exist before deletion
            Assert.True(File.Exists(Path.Combine(coversPath, frontCoverFile)));
            Assert.True(File.Exists(Path.Combine(coversPath, backCoverFile)));
            Assert.True(File.Exists(Path.Combine(thumbnailsPath, thumbnailFile)));

            // Act
            var deletedCount = await _repository.DeleteAllUserMusicReleasesAsync(_testUserId);

            // Assert
            Assert.Equal(1, deletedCount);
            
            // Verify database record deleted
            var remainingReleases = await _context.MusicReleases.CountAsync(mr => mr.UserId == _testUserId);
            Assert.Equal(0, remainingReleases);
            
            // Verify image files deleted
            Assert.False(File.Exists(Path.Combine(coversPath, frontCoverFile)));
            Assert.False(File.Exists(Path.Combine(coversPath, backCoverFile)));
            Assert.False(File.Exists(Path.Combine(thumbnailsPath, thumbnailFile)));
        }

        [Fact]
        public async Task DeleteAllUserMusicReleasesAsync_WithMultipleReleases_DeletesAllImagesAndReleases()
        {
            // Arrange
            var coversPath = Path.Combine(_testImagesPath, "covers");
            var thumbnailsPath = Path.Combine(_testImagesPath, "thumbnails");
            
            // Create test files for first release
            var frontCover1 = "album1-front.jpg";
            var thumbnail1 = "album1-thumb.jpg";
            File.WriteAllText(Path.Combine(coversPath, frontCover1), "content1");
            File.WriteAllText(Path.Combine(thumbnailsPath, thumbnail1), "thumb1");

            // Create test files for second release
            var frontCover2 = "album2-front.jpg";
            var backCover2 = "album2-back.jpg";
            File.WriteAllText(Path.Combine(coversPath, frontCover2), "content2");
            File.WriteAllText(Path.Combine(coversPath, backCover2), "back2");

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
            
            // Verify all files deleted
            Assert.False(File.Exists(Path.Combine(coversPath, frontCover1)));
            Assert.False(File.Exists(Path.Combine(thumbnailsPath, thumbnail1)));
            Assert.False(File.Exists(Path.Combine(coversPath, frontCover2)));
            Assert.False(File.Exists(Path.Combine(coversPath, backCover2)));
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
