using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="StorageMigrationService"/>.
    /// </summary>
    public class StorageMigrationServiceTests
    {
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<StorageMigrationService>> _mockLogger;

        public StorageMigrationServiceTests()
        {
            _mockStorageService = new Mock<IStorageService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<StorageMigrationService>>();

            _mockConfiguration.Setup(c => c["ImagesPath"]).Returns("/tmp/test-images");
        }

        private KollectorScumDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new KollectorScumDbContext(options);
        }

        [Fact]
        public async Task MigrateLocalStorageAsync_NoReleases_ReturnsEmptyResult()
        {
            using var context = CreateInMemoryContext(nameof(MigrateLocalStorageAsync_NoReleases_ReturnsEmptyResult));
            var service = new StorageMigrationService(context, _mockStorageService.Object, _mockConfiguration.Object, _mockLogger.Object);

            var result = await service.MigrateLocalStorageAsync();

            Assert.Equal(0, result.TotalConsidered);
            Assert.Equal(0, result.MigratedCount);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task MigrateLocalStorageAsync_ReleaseNotFound_ReturnsError()
        {
            using var context = CreateInMemoryContext(nameof(MigrateLocalStorageAsync_ReleaseNotFound_ReturnsError));
            var service = new StorageMigrationService(context, _mockStorageService.Object, _mockConfiguration.Object, _mockLogger.Object);

            var result = await service.MigrateLocalStorageAsync(99999);

            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task MigrateLocalStorageAsync_AlreadyMigratedRelease_IsSkipped()
        {
            using var context = CreateInMemoryContext(nameof(MigrateLocalStorageAsync_AlreadyMigratedRelease_IsSkipped));

            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test",
                UserId = Guid.NewGuid(),
                Images = "{\"CoverFront\":\"/cover-art/userid/file.jpg\"}"
            };
            context.MusicReleases.Add(release);
            await context.SaveChangesAsync();

            var service = new StorageMigrationService(context, _mockStorageService.Object, _mockConfiguration.Object, _mockLogger.Object);

            var result = await service.MigrateLocalStorageAsync();

            Assert.Equal(0, result.MigratedCount);
            _mockStorageService.Verify(s => s.UploadFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
        }
    }
}
