using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    public class MusicReleaseQueryServiceTests
    {
        private readonly Mock<IRepository<MusicRelease>> _mockMusicReleaseRepo;
        private readonly Mock<IRepository<Artist>> _mockArtistRepo;
        private readonly Mock<IRepository<Label>> _mockLabelRepo;
        private readonly Mock<IMusicReleaseMapperService> _mockMapper;
        private readonly Mock<ICollectionStatisticsService> _mockStatisticsService;
        private readonly KollectorScumDbContext _context;
        private readonly Mock<ILogger<MusicReleaseQueryService>> _mockLogger;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly MusicReleaseQueryService _service;

        public MusicReleaseQueryServiceTests()
        {
            _mockMusicReleaseRepo = new Mock<IRepository<MusicRelease>>();
            _mockArtistRepo = new Mock<IRepository<Artist>>();
            _mockLabelRepo = new Mock<IRepository<Label>>();
            _mockMapper = new Mock<IMusicReleaseMapperService>();
            _mockStatisticsService = new Mock<ICollectionStatisticsService>();
            _mockLogger = new Mock<ILogger<MusicReleaseQueryService>>();
            _mockUserContext = new Mock<IUserContext>();
            var defaultUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");
            _mockUserContext.Setup(u => u.GetActingUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.GetUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.IsAdmin()).Returns(false);

            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new KollectorScumDbContext(options);

            _service = new MusicReleaseQueryService(
                _mockMusicReleaseRepo.Object,
                _mockArtistRepo.Object,
                _mockLabelRepo.Object,
                _mockMapper.Object,
                _mockStatisticsService.Object,
                _context,
                _mockLogger.Object,
                _mockUserContext.Object
            );
        }

        [Fact]
        public async Task GetMusicReleasesAsync_WithUserContext_FiltersByUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(userId);

            var parameters = new MusicReleaseQueryParameters
            {
                Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
            };

            // Capture the filter passed to GetPagedAsync
            Expression<Func<MusicRelease, bool>>? capturedFilter = null;
            _mockMusicReleaseRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                It.IsAny<string>()
            )).Callback<int, int, Expression<Func<MusicRelease, bool>>, Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>, string>(
                (page, size, filter, orderBy, include) => capturedFilter = filter
            ).ReturnsAsync(new PagedResult<MusicRelease>
            {
                Items = new List<MusicRelease>(),
                TotalCount = 0
            });

            // Act
            await _service.GetMusicReleasesAsync(parameters);

            // Assert
            Assert.NotNull(capturedFilter);
            
            // Compile and test the filter
            var func = capturedFilter!.Compile();
            
            var myRelease = new MusicRelease { UserId = userId, Title = "My Release" };
            var otherRelease = new MusicRelease { UserId = otherUserId, Title = "Other Release" };

            Assert.True(func(myRelease), "Filter should match user's release");
            Assert.False(func(otherRelease), "Filter should NOT match other user's release");
        }

        [Fact]
        public async Task GetMusicReleasesAsync_WithNoUserContext_ReturnsEmpty()
        {
            // Arrange
            _mockUserContext.Setup(x => x.GetActingUserId()).Returns((Guid?)null);

            var parameters = new MusicReleaseQueryParameters
            {
                Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
            };

            // Capture the filter passed to GetPagedAsync
            Expression<Func<MusicRelease, bool>>? capturedFilter = null;
            _mockMusicReleaseRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                It.IsAny<string>()
            )).Callback<int, int, Expression<Func<MusicRelease, bool>>, Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>, string>(
                (page, size, filter, orderBy, include) => capturedFilter = filter
            ).ReturnsAsync(new PagedResult<MusicRelease>
            {
                Items = new List<MusicRelease>(),
                TotalCount = 0
            });

            // Act
            await _service.GetMusicReleasesAsync(parameters);

            // Assert
            Assert.NotNull(capturedFilter);
            var func = capturedFilter!.Compile();
            var anyRelease = new MusicRelease { UserId = Guid.NewGuid(), Title = "Any Release" };
            Assert.False(func(anyRelease), "Filter should always return false when no user context");
        }
    }
}
