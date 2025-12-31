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
        private readonly ProfileController _controller;

        public ProfileControllerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUserProfileRepository = new Mock<IUserProfileRepository>();
            _mockLogger = new Mock<ILogger<ProfileController>>();

            _controller = new ProfileController(
                _mockUserRepository.Object,
                _mockUserProfileRepository.Object,
                _mockLogger.Object
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
    }
}
