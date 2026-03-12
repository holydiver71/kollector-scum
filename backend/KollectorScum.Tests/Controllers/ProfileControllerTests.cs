using System;
using System.Security.Claims;
using System.Threading.Tasks;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Controllers
{
    public class ProfileControllerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IUserProfileRepository> _mockUserProfileRepository;
        private readonly Mock<ILogger<ProfileController>> _mockLogger;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly ProfileController _controller;

        public ProfileControllerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUserProfileRepository = new Mock<IUserProfileRepository>();
            _mockLogger = new Mock<ILogger<ProfileController>>();
            _mockUserContext = new Mock<IUserContext>();

            _controller = new ProfileController(
                _mockUserRepository.Object,
                _mockUserProfileRepository.Object,
                _mockLogger.Object,
                _mockUserContext.Object
            );
        }

        private void SetupUserClaims(Guid userId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(userId);
        }

        [Fact]
        public async Task GetProfile_WithValidUser_ReturnsProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = userId,
                GoogleSub = "google-sub-123",
                Email = "test@example.com",
                DisplayName = "Test User"
            };

            var profile = new UserProfile
            {
                Id = 1,
                UserId = userId,
                SelectedKollectionId = 5
            };

            SetupUserClaims(userId);

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockUserProfileRepository
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(profile);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var profileDto = Assert.IsType<UserProfileDto>(okResult.Value);
            Assert.Equal(userId, profileDto.UserId);
            Assert.Equal(user.Email, profileDto.Email);
            Assert.Equal(user.DisplayName, profileDto.DisplayName);
            Assert.Equal(5, profileDto.SelectedKollectionId);
        }

        [Fact]
        public async Task UpdateProfile_WithValidKollection_UpdatesProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = userId,
                GoogleSub = "google-sub-123",
                Email = "test@example.com",
                DisplayName = "Test User"
            };

            var existingProfile = new UserProfile
            {
                Id = 1,
                UserId = userId,
                SelectedKollectionId = 5
            };

            var request = new UpdateProfileRequest
            {
                SelectedKollectionId = 10
            };

            SetupUserClaims(userId);

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockUserProfileRepository
                .Setup(x => x.KollectionExistsAsync(10))
                .ReturnsAsync(true);

            _mockUserProfileRepository
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(existingProfile);

            _mockUserProfileRepository
                .Setup(x => x.UpdateAsync(It.IsAny<UserProfile>()))
                .ReturnsAsync((UserProfile p) => p);

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var profileDto = Assert.IsType<UserProfileDto>(okResult.Value);
            Assert.Equal(10, profileDto.SelectedKollectionId);

            _mockUserProfileRepository.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProfile_WithInvalidKollection_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = userId,
                GoogleSub = "google-sub-123",
                Email = "test@example.com",
                DisplayName = "Test User"
            };

            var request = new UpdateProfileRequest
            {
                SelectedKollectionId = 999
            };

            SetupUserClaims(userId);

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockUserProfileRepository
                .Setup(x => x.KollectionExistsAsync(999))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);

            _mockUserProfileRepository.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>()), Times.Never);
        }

        [Fact]
        public async Task UpdateProfile_CreatesProfileIfNotExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = userId,
                GoogleSub = "google-sub-123",
                Email = "test@example.com",
                DisplayName = "Test User"
            };

            var request = new UpdateProfileRequest
            {
                SelectedKollectionId = 10
            };

            SetupUserClaims(userId);

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockUserProfileRepository
                .Setup(x => x.KollectionExistsAsync(10))
                .ReturnsAsync(true);

            _mockUserProfileRepository
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync((UserProfile?)null);

            _mockUserProfileRepository
                .Setup(x => x.CreateAsync(It.IsAny<UserProfile>()))
                .ReturnsAsync((UserProfile p) => p);

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var profileDto = Assert.IsType<UserProfileDto>(okResult.Value);
            Assert.Equal(10, profileDto.SelectedKollectionId);

            _mockUserProfileRepository.Verify(x => x.CreateAsync(It.IsAny<UserProfile>()), Times.Once);
            _mockUserProfileRepository.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>()), Times.Never);
        }

        #region DeleteCollection Tests

        [Fact]
        public async Task DeleteCollection_WithValidUser_DeletesAllReleasesAndImages()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = userId,
                GoogleSub = "google-sub-123",
                Email = "test@example.com",
                DisplayName = "Test User"
            };

            SetupUserClaims(userId);

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockUserProfileRepository
                .Setup(x => x.GetUserMusicReleaseCountAsync(userId))
                .ReturnsAsync(10);

            _mockUserProfileRepository
                .Setup(x => x.DeleteAllUserMusicReleasesAsync(userId))
                .ReturnsAsync(10);

            // Act
            var result = await _controller.DeleteCollection();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<DeleteCollectionResponse>(okResult.Value);
            Assert.Equal(10, response.AlbumsDeleted);
            Assert.True(response.Success);
            Assert.NotNull(response.Message);
            Assert.Contains("10 album(s)", response.Message);

            // Verify that DeleteAllUserMusicReleasesAsync was called (which now includes image deletion)
            _mockUserProfileRepository.Verify(x => x.DeleteAllUserMusicReleasesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DeleteCollection_WithNoReleases_ReturnsZeroCount()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = userId,
                GoogleSub = "google-sub-123",
                Email = "test@example.com",
                DisplayName = "Test User"
            };

            SetupUserClaims(userId);

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockUserProfileRepository
                .Setup(x => x.GetUserMusicReleaseCountAsync(userId))
                .ReturnsAsync(0);

            _mockUserProfileRepository
                .Setup(x => x.DeleteAllUserMusicReleasesAsync(userId))
                .ReturnsAsync(0);

            // Act
            var result = await _controller.DeleteCollection();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<DeleteCollectionResponse>(okResult.Value);
            Assert.Equal(0, response.AlbumsDeleted);
            Assert.True(response.Success);

            _mockUserProfileRepository.Verify(x => x.DeleteAllUserMusicReleasesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DeleteCollection_WithInvalidUser_ReturnsUnauthorized()
        {
            // Arrange - No claims set up

            // Act
            var result = await _controller.DeleteCollection();

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result.Result);

            _mockUserProfileRepository.Verify(x => x.DeleteAllUserMusicReleasesAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteCollection_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId);

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.DeleteCollection();

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);

            _mockUserProfileRepository.Verify(x => x.DeleteAllUserMusicReleasesAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Admin Impersonation Tests

        [Fact]
        public async Task GetProfile_AdminImpersonatingNonAdminUser_ReturnsImpersonatedProfile()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                Email = "target@example.com",
                DisplayName = "Target User",
                IsAdmin = false
            };

            var targetProfile = new UserProfile
            {
                Id = 2,
                UserId = targetUserId,
                SelectedKollectionId = 7
            };

            // GetActingUserId returns the target (impersonated) user's ID
            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(targetUserId);

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(targetUserId))
                .ReturnsAsync(targetUser);

            _mockUserProfileRepository
                .Setup(x => x.GetByUserIdAsync(targetUserId))
                .ReturnsAsync(targetProfile);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<UserProfileDto>(okResult.Value);
            Assert.Equal(targetUserId, dto.UserId);
            Assert.Equal("target@example.com", dto.Email);
            Assert.Equal("Target User", dto.DisplayName);
            Assert.Equal(7, dto.SelectedKollectionId);
        }

        [Fact]
        public async Task GetProfile_AdminWithNoImpersonation_ReturnsAdminOwnProfile()
        {
            // Arrange
            var adminUserId = Guid.NewGuid();
            var adminUser = new ApplicationUser
            {
                Id = adminUserId,
                Email = "admin@example.com",
                DisplayName = "Admin User",
                IsAdmin = true
            };

            var adminProfile = new UserProfile
            {
                Id = 1,
                UserId = adminUserId,
                SelectedKollectionId = 3
            };

            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(adminUserId);

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(adminUserId))
                .ReturnsAsync(adminUser);

            _mockUserProfileRepository
                .Setup(x => x.GetByUserIdAsync(adminUserId))
                .ReturnsAsync(adminProfile);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<UserProfileDto>(okResult.Value);
            Assert.Equal(adminUserId, dto.UserId);
            Assert.Equal("admin@example.com", dto.Email);
            Assert.True(dto.IsAdmin);
        }

        [Fact]
        public async Task GetProfile_NonAdminUser_ReturnsOwnProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = userId,
                Email = "regular@example.com",
                DisplayName = "Regular User",
                IsAdmin = false
            };

            var profile = new UserProfile
            {
                Id = 3,
                UserId = userId,
                SelectedKollectionId = 1
            };

            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(userId);

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockUserProfileRepository
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(profile);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<UserProfileDto>(okResult.Value);
            Assert.Equal(userId, dto.UserId);
            Assert.Equal("regular@example.com", dto.Email);
            Assert.False(dto.IsAdmin);
        }

        [Fact]
        public async Task GetProfile_ImpersonatedUserNotFound_ReturnsNotFound()
        {
            // Arrange
            var missingUserId = Guid.NewGuid();

            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(missingUserId);

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(missingUserId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion
    }
}
