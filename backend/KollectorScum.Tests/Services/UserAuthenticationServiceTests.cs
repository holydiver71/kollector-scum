using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="UserAuthenticationService"/>.
    /// </summary>
    public class UserAuthenticationServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IUserProfileRepository> _mockUserProfileRepository;
        private readonly Mock<IUserInvitationRepository> _mockInvitationRepository;
        private readonly Mock<ILogger<UserAuthenticationService>> _mockLogger;
        private readonly UserAuthenticationService _service;

        public UserAuthenticationServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUserProfileRepository = new Mock<IUserProfileRepository>();
            _mockInvitationRepository = new Mock<IUserInvitationRepository>();
            _mockLogger = new Mock<ILogger<UserAuthenticationService>>();

            _service = new UserAuthenticationService(
                _mockUserRepository.Object,
                _mockUserProfileRepository.Object,
                _mockInvitationRepository.Object,
                _mockLogger.Object);
        }

        #region FindOrCreateUserFromGoogleAsync Tests

        [Fact]
        public async Task FindOrCreateUserFromGoogleAsync_ExistingUser_ReturnsUser()
        {
            var existing = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                GoogleSub = "sub123",
                Email = "user@example.com",
                DisplayName = "User"
            };

            _mockUserRepository.Setup(r => r.FindByGoogleSubAsync("sub123")).ReturnsAsync(existing);

            var result = await _service.FindOrCreateUserFromGoogleAsync("sub123", "user@example.com", "User");

            Assert.Equal(existing.Id, result.Id);
            _mockUserRepository.Verify(r => r.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task FindOrCreateUserFromGoogleAsync_ExistingUser_UpdatesChangedEmail()
        {
            var existing = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                GoogleSub = "sub123",
                Email = "old@example.com",
                DisplayName = "User"
            };

            _mockUserRepository.Setup(r => r.FindByGoogleSubAsync("sub123")).ReturnsAsync(existing);
            _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync((ApplicationUser u) => u);

            await _service.FindOrCreateUserFromGoogleAsync("sub123", "new@example.com", "User");

            _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        }

        [Fact]
        public async Task FindOrCreateUserFromGoogleAsync_NoInvitation_ThrowsUnauthorizedAccessException()
        {
            _mockUserRepository.Setup(r => r.FindByGoogleSubAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            _mockInvitationRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((UserInvitation?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.FindOrCreateUserFromGoogleAsync("sub123", "uninvited@example.com", "User"));
        }

        [Fact]
        public async Task FindOrCreateUserFromGoogleAsync_DeactivatedAccount_ThrowsUnauthorizedAccessException()
        {
            _mockUserRepository.Setup(r => r.FindByGoogleSubAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            _mockUserRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            _mockInvitationRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserInvitation { Email = "user@example.com", IsUsed = true });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.FindOrCreateUserFromGoogleAsync("sub123", "user@example.com", "User"));
        }

        [Fact]
        public async Task FindOrCreateUserFromGoogleAsync_NewInvitedUser_CreatesUserAndProfile()
        {
            var invitation = new UserInvitation { Email = "user@example.com", IsUsed = false };
            var createdUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com" };

            _mockUserRepository.Setup(r => r.FindByGoogleSubAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            _mockUserRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            _mockInvitationRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(invitation);
            _mockUserRepository.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(createdUser);
            _mockUserProfileRepository.Setup(r => r.CreateAsync(It.IsAny<UserProfile>()))
                .ReturnsAsync(new UserProfile());
            _mockInvitationRepository.Setup(r => r.UpdateAsync(It.IsAny<UserInvitation>()))
                .ReturnsAsync(invitation);

            var result = await _service.FindOrCreateUserFromGoogleAsync("sub123", "user@example.com", "User");

            Assert.Equal(createdUser.Id, result.Id);
            _mockUserProfileRepository.Verify(r => r.CreateAsync(It.IsAny<UserProfile>()), Times.Once);
            _mockInvitationRepository.Verify(r => r.UpdateAsync(It.Is<UserInvitation>(i => i.IsUsed)), Times.Once);
        }

        #endregion

        #region FindOrCreateUserFromEmailAsync Tests

        [Fact]
        public async Task FindOrCreateUserFromEmailAsync_ExistingUser_ReturnsUser()
        {
            var existing = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com" };

            _mockUserRepository.Setup(r => r.FindByEmailAsync("user@example.com")).ReturnsAsync(existing);

            var result = await _service.FindOrCreateUserFromEmailAsync("user@example.com");

            Assert.Equal(existing.Id, result.Id);
            _mockUserRepository.Verify(r => r.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task FindOrCreateUserFromEmailAsync_NoInvitation_ThrowsUnauthorizedAccessException()
        {
            _mockUserRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            _mockInvitationRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((UserInvitation?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.FindOrCreateUserFromEmailAsync("uninvited@example.com"));
        }

        [Fact]
        public async Task FindOrCreateUserFromEmailAsync_NewInvitedUser_CreatesUserAndProfile()
        {
            var invitation = new UserInvitation { Email = "user@example.com", IsUsed = false };
            var createdUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com" };

            _mockUserRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            _mockInvitationRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(invitation);
            _mockUserRepository.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(createdUser);
            _mockUserProfileRepository.Setup(r => r.CreateAsync(It.IsAny<UserProfile>()))
                .ReturnsAsync(new UserProfile());
            _mockInvitationRepository.Setup(r => r.UpdateAsync(It.IsAny<UserInvitation>()))
                .ReturnsAsync(invitation);

            var result = await _service.FindOrCreateUserFromEmailAsync("user@example.com");

            Assert.Equal(createdUser.Id, result.Id);
            _mockUserProfileRepository.Verify(r => r.CreateAsync(It.IsAny<UserProfile>()), Times.Once);
            _mockInvitationRepository.Verify(r => r.UpdateAsync(It.Is<UserInvitation>(i => i.IsUsed)), Times.Once);
        }

        #endregion
    }
}
