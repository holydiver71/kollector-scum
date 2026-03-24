using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    public class DiscogsCollectionImportServiceTests
    {
        private readonly Mock<IDiscogsService> _mockDiscogsService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRepository<MusicRelease>> _mockMusicRepo;
        private readonly Mock<IDiscogsImageService> _mockImageService;
        private readonly Mock<ILogger<DiscogsCollectionImportService>> _mockLogger;
        private readonly Mock<IHostEnvironment> _mockEnv;

        public DiscogsCollectionImportServiceTests()
        {
            _mockDiscogsService = new Mock<IDiscogsService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMusicRepo = new Mock<IRepository<MusicRelease>>();
            _mockImageService = new Mock<IDiscogsImageService>();
            _mockLogger = new Mock<ILogger<DiscogsCollectionImportService>>();
            _mockEnv = new Mock<IHostEnvironment>();

            _mockUnitOfWork.Setup(u => u.MusicReleases).Returns(_mockMusicRepo.Object);
            // Default behaviors to keep MapToMusicReleaseAsync lightweight
            _mockMusicRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MusicRelease,bool>>?>()))
                .ReturnsAsync(0);
            _mockMusicRepo.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MusicRelease,bool>>>(), null, ""))
                .ReturnsAsync(new List<MusicRelease>());
            _mockUnitOfWork.Setup(u => u.UpsertFormatAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.UpsertLabelAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.UpsertCountryAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.UpsertArtistAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.UpsertGenreAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(1);
            _mockMusicRepo.Setup(r => r.AddAsync(It.IsAny<MusicRelease>())).ReturnsAsync((MusicRelease m) => m);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        }

        private DiscogsCollectionImportService CreateService()
        {
            var mockCache = new Mock<KollectorScum.Api.Interfaces.ICacheService>();
            return new DiscogsCollectionImportService(
                _mockDiscogsService.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockImageService.Object,
                _mockEnv.Object,
                mockCache.Object);
        }

        private DiscogsCollectionResponseDto MakePage(int page, int perPage, int totalPages, int totalItems)
        {
            var releases = Enumerable.Range(1, perPage).Select(i => new DiscogsCollectionReleaseDto
            {
                InstanceId = Guid.NewGuid().ToString(),
                BasicInformation = new DiscogsBasicInfoDto
                {
                    // Use Id=0 to avoid the 1.1s per-release delay in MapToMusicReleaseAsync
                    Id = 0,
                    Title = $"Test Release {page}-{i}",
                }
            }).ToList();

            return new DiscogsCollectionResponseDto
            {
                Pagination = new DiscogsPaginationDto
                {
                    Page = page,
                    PerPage = perPage,
                    Pages = totalPages,
                    Items = totalItems
                },
                Releases = releases
            };
        }

        [Fact]
        public async Task ImportCollectionAsync_NewUser_InDevelopment_Applies100Limit()
        {
            // Arrange
            var username = "testuser";
            var userId = Guid.NewGuid();
            _mockEnv.SetupGet(e => e.EnvironmentName).Returns("Development");

            // Simulate 3 pages of 100 => total 300
            _mockDiscogsService.Setup(s => s.GetUserCollectionAsync(username, 1, 100))
                .ReturnsAsync(MakePage(1, 100, 3, 300));
            // Page 2/3 should not be called when limit applies, but set them up defensively
            _mockDiscogsService.Setup(s => s.GetUserCollectionAsync(username, 2, 100))
                .ReturnsAsync(MakePage(2, 100, 3, 300));
            _mockDiscogsService.Setup(s => s.GetUserCollectionAsync(username, 3, 100))
                .ReturnsAsync(MakePage(3, 100, 3, 300));

            var service = CreateService();

            // Act
            var result = await service.ImportCollectionAsync(username, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(300, result.TotalReleases);
            Assert.Equal(100, result.ImportedReleases);
            // Only first page should be requested when limit applies
            _mockDiscogsService.Verify(s => s.GetUserCollectionAsync(username, 1, 100), Times.Once);
            _mockDiscogsService.Verify(s => s.GetUserCollectionAsync(username, 2, 100), Times.Never);
        }

        [Fact]
        public async Task ImportCollectionAsync_InProduction_NoLimit()
        {
            // Arrange
            var username = "produser";
            var userId = Guid.NewGuid();
            _mockEnv.SetupGet(e => e.EnvironmentName).Returns("Production");

            // Simulate 2 pages of 50 => total 100 to keep test fast
            _mockDiscogsService.Setup(s => s.GetUserCollectionAsync(username, 1, 100))
                .ReturnsAsync(MakePage(1, 50, 2, 100));
            _mockDiscogsService.Setup(s => s.GetUserCollectionAsync(username, 2, 100))
                .ReturnsAsync(MakePage(2, 50, 2, 100));

            var service = CreateService();

            // Act
            var result = await service.ImportCollectionAsync(username, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.TotalReleases);
            Assert.Equal(100, result.ImportedReleases);
            _mockDiscogsService.Verify(s => s.GetUserCollectionAsync(username, 1, 100), Times.Once);
            _mockDiscogsService.Verify(s => s.GetUserCollectionAsync(username, 2, 100), Times.Once);
        }
    }
}
