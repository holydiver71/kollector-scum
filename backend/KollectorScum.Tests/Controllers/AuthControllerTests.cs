using System;
using System.Threading.Tasks;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IGoogleTokenValidator> _mockGoogleTokenValidator;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IUserProfileRepository> _mockUserProfileRepository;
        private readonly Mock<IUserInvitationRepository> _mockInvitationRepository;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IHostEnvironment> _mockEnvironment;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockGoogleTokenValidator = new Mock<IGoogleTokenValidator>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUserProfileRepository = new Mock<IUserProfileRepository>();
            _mockInvitationRepository = new Mock<IUserInvitationRepository>();
            _mockTokenService = new Mock<ITokenService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockEnvironment = new Mock<IHostEnvironment>();
            _mockLogger = new Mock<ILogger<AuthController>>();

            _controller = new AuthController(
                _mockGoogleTokenValidator.Object,
                _mockUserRepository.Object,
                _mockUserProfileRepository.Object,
                _mockInvitationRepository.Object,
                _mockTokenService.Object,
                _mockConfiguration.Object,
                _mockEnvironment.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GoogleAuth_WithValidToken_CreatesNewUser_ReturnsAuthResponse()
        {
            // Arrange
            var request = new GoogleAuthRequest { IdToken = "valid-token" };
            var googleSub = "google-sub-123";
            var email = "test@example.com";
            var displayName = "Test User";
            var jwtToken = "jwt-token-123";

            _mockGoogleTokenValidator
                .Setup(x => x.ValidateTokenAsync(request.IdToken))
                .ReturnsAsync((googleSub, email, displayName));

            _mockUserRepository
                .Setup(x => x.FindByGoogleSubAsync(googleSub))
                .ReturnsAsync((ApplicationUser?)null);

            var invitation = new UserInvitation
            {
                Id = 1,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };
            _mockInvitationRepository
                .Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(invitation);

            _mockUserRepository
                .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync((ApplicationUser user) => user);

            _mockUserProfileRepository
                .Setup(x => x.CreateAsync(It.IsAny<UserProfile>()))
                .ReturnsAsync((UserProfile profile) => profile);

            _mockInvitationRepository
                .Setup(x => x.UpdateAsync(It.IsAny<UserInvitation>()))
                .ReturnsAsync((UserInvitation inv) => inv);

            _mockTokenService
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(jwtToken);

            // Act
            var result = await _controller.GoogleAuth(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthResponse>(okResult.Value);
            Assert.Equal(jwtToken, response.Token);
            Assert.Equal(email, response.Profile.Email);
            Assert.Equal(displayName, response.Profile.DisplayName);

            _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
            _mockUserProfileRepository.Verify(x => x.CreateAsync(It.IsAny<UserProfile>()), Times.Once);
            _mockInvitationRepository.Verify(x => x.UpdateAsync(It.Is<UserInvitation>(i => i.IsUsed)), Times.Once);
        }

        [Fact]
        public async Task GoogleAuth_WithValidToken_ExistingUser_ReturnsAuthResponse()
        {
            // Arrange
            var request = new GoogleAuthRequest { IdToken = "valid-token" };
            var googleSub = "google-sub-123";
            var email = "test@example.com";
            var displayName = "Test User";
            var jwtToken = "jwt-token-123";
            var userId = Guid.NewGuid();

            var existingUser = new ApplicationUser
            {
                Id = userId,
                GoogleSub = googleSub,
                Email = email,
                DisplayName = displayName
            };

            var existingProfile = new UserProfile
            {
                Id = 1,
                UserId = userId,
                SelectedKollectionId = null
            };

            _mockGoogleTokenValidator
                .Setup(x => x.ValidateTokenAsync(request.IdToken))
                .ReturnsAsync((googleSub, email, displayName));

            _mockUserRepository
                .Setup(x => x.FindByGoogleSubAsync(googleSub))
                .ReturnsAsync(existingUser);

            _mockUserProfileRepository
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(existingProfile);

            _mockTokenService
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(jwtToken);

            // Act
            var result = await _controller.GoogleAuth(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthResponse>(okResult.Value);
            Assert.Equal(jwtToken, response.Token);
            Assert.Equal(email, response.Profile.Email);

            _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
            _mockUserProfileRepository.Verify(x => x.CreateAsync(It.IsAny<UserProfile>()), Times.Never);
        }

        [Fact]
        public async Task GoogleAuth_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new GoogleAuthRequest { IdToken = "invalid-token" };

            _mockGoogleTokenValidator
                .Setup(x => x.ValidateTokenAsync(request.IdToken))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid token"));

            // Act
            var result = await _controller.GoogleAuth(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.NotNull(unauthorizedResult.Value);
        }

        [Fact]
        public async Task GoogleAuth_WithUninvitedUser_ReturnsForbidden()
        {
            // Arrange
            var request = new GoogleAuthRequest { IdToken = "valid-token" };
            var googleSub = "google-sub-123";
            var email = "uninvited@example.com";
            var displayName = "Uninvited User";

            _mockGoogleTokenValidator
                .Setup(x => x.ValidateTokenAsync(request.IdToken))
                .ReturnsAsync((googleSub, email, displayName));

            _mockUserRepository
                .Setup(x => x.FindByGoogleSubAsync(googleSub))
                .ReturnsAsync((ApplicationUser?)null);

            _mockInvitationRepository
                .Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync((UserInvitation?)null);

            // Act
            var result = await _controller.GoogleAuth(request);

            // Assert
            var forbidResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(403, forbidResult.StatusCode);
        }
    }
}
