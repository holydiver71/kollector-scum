using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="UserImpersonationService"/>.
    /// </summary>
    public class UserImpersonationServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<UserImpersonationService>> _mockLogger;
        private readonly UserImpersonationService _service;

        public UserImpersonationServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UserImpersonationService>>();
            _service = new UserImpersonationService(_mockUserRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ImpersonateUserAsync_SelfImpersonation_ThrowsInvalidOperationException()
        {
            var adminId = Guid.NewGuid();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ImpersonateUserAsync(adminId, adminId));
        }

        [Fact]
        public async Task ImpersonateUserAsync_UserNotFound_ReturnsNull()
        {
            var adminId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            _mockUserRepository.Setup(r => r.FindByIdAsync(targetId))
                .ReturnsAsync((Api.Models.ApplicationUser?)null);

            var result = await _service.ImpersonateUserAsync(adminId, targetId);

            Assert.Null(result);
        }

        [Fact]
        public async Task ImpersonateUserAsync_AdminTarget_ThrowsInvalidOperationException()
        {
            var adminId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            _mockUserRepository.Setup(r => r.FindByIdAsync(targetId))
                .ReturnsAsync(new Api.Models.ApplicationUser
                {
                    Id = targetId,
                    Email = "admin@example.com",
                    IsAdmin = true
                });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ImpersonateUserAsync(adminId, targetId));
        }

        [Fact]
        public async Task ImpersonateUserAsync_ValidTarget_ReturnsImpersonationDto()
        {
            var adminId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            _mockUserRepository.Setup(r => r.FindByIdAsync(targetId))
                .ReturnsAsync(new Api.Models.ApplicationUser
                {
                    Id = targetId,
                    Email = "user@example.com",
                    DisplayName = "Test User",
                    IsAdmin = false
                });

            var result = await _service.ImpersonateUserAsync(adminId, targetId);

            Assert.NotNull(result);
            Assert.Equal(targetId, result.UserId);
            Assert.Equal("user@example.com", result.Email);
            Assert.Equal("Test User", result.DisplayName);
        }
    }
}
