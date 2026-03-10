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
using System.Net.Http;

namespace KollectorScum.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IGoogleTokenValidator> _mockGoogleTokenValidator;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IUserProfileRepository> _mockUserProfileRepository;
        private readonly Mock<IUserInvitationRepository> _mockInvitationRepository;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<IMagicLinkService> _mockMagicLinkService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IHostEnvironment> _mockEnvironment;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockGoogleTokenValidator = new Mock<IGoogleTokenValidator>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUserProfileRepository = new Mock<IUserProfileRepository>();
            _mockInvitationRepository = new Mock<IUserInvitationRepository>();
            _mockTokenService = new Mock<ITokenService>();
            _mockMagicLinkService = new Mock<IMagicLinkService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockEnvironment = new Mock<IHostEnvironment>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            _controller = new AuthController(
                _mockGoogleTokenValidator.Object,
                _mockUserRepository.Object,
                _mockUserProfileRepository.Object,
                _mockInvitationRepository.Object,
                _mockTokenService.Object,
                _mockMagicLinkService.Object,
                _mockConfiguration.Object,
                _mockEnvironment.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object
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

        // ─── Magic Link Tests ───────────────────────────────────────────────────────

        [Fact]
        public async Task RequestMagicLink_WithInvitedEmail_ReturnsOk()
        {
            // Arrange
            var request = new MagicLinkRequestDto { Email = "invited@example.com" };
            var invitation = new UserInvitation
            {
                Id = 1,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            _mockInvitationRepository
                .Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync(invitation);

            _mockMagicLinkService
                .Setup(x => x.CreateAndSendTokenAsync(request.Email, It.IsAny<string>()))
                .ReturnsAsync(new MagicLinkToken
                {
                    Id = 1,
                    Email = request.Email,
                    Token = "test-token",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                });

            // Need to configure the mock configuration for Frontend:Origins
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(x => x.Value).Returns("http://localhost:3000");
            _mockConfiguration.Setup(x => x["Frontend:Origins"]).Returns("http://localhost:3000");

            // Act
            var result = await _controller.RequestMagicLink(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockMagicLinkService.Verify(
                x => x.CreateAndSendTokenAsync(request.Email, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task RequestMagicLink_WithUninvitedEmail_ReturnsOkWithoutSendingToken()
        {
            // Arrange - returns 200 to avoid email enumeration
            var request = new MagicLinkRequestDto { Email = "unknown@example.com" };

            _mockInvitationRepository
                .Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((UserInvitation?)null);

            // Act
            var result = await _controller.RequestMagicLink(request);

            // Assert - still returns 200 to avoid email enumeration
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockMagicLinkService.Verify(
                x => x.CreateAndSendTokenAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task VerifyMagicLink_WithValidToken_ExistingUser_ReturnsAuthResponse()
        {
            // Arrange
            var tokenValue = "valid-token-abc123";
            var email = "user@example.com";
            var userId = Guid.NewGuid();
            var jwtToken = "jwt-abc";

            var request = new MagicLinkVerifyDto { Token = tokenValue };

            var existingUser = new ApplicationUser
            {
                Id = userId,
                GoogleSub = null,
                Email = email,
                DisplayName = "User"
            };

            var existingProfile = new UserProfile
            {
                Id = 1,
                UserId = userId,
                SelectedKollectionId = null
            };

            _mockMagicLinkService
                .Setup(x => x.ValidateTokenAsync(tokenValue))
                .ReturnsAsync(email);

            _mockUserRepository
                .Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(existingUser);

            _mockUserProfileRepository
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(existingProfile);

            _mockTokenService
                .Setup(x => x.GenerateToken(existingUser))
                .Returns(jwtToken);

            _mockMagicLinkService
                .Setup(x => x.MarkTokenAsUsedAsync(tokenValue))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.VerifyMagicLink(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthResponse>(okResult.Value);
            Assert.Equal(jwtToken, response.Token);
            Assert.Equal(email, response.Profile.Email);

            _mockMagicLinkService.Verify(x => x.MarkTokenAsUsedAsync(tokenValue), Times.Once);
            _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task VerifyMagicLink_WithValidToken_NewUser_CreatesUserAndReturnsAuthResponse()
        {
            // Arrange
            var tokenValue = "valid-token-new-user";
            var email = "newuser@example.com";
            var jwtToken = "jwt-new";

            var request = new MagicLinkVerifyDto { Token = tokenValue };

            var invitation = new UserInvitation
            {
                Id = 2,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            _mockMagicLinkService
                .Setup(x => x.ValidateTokenAsync(tokenValue))
                .ReturnsAsync(email);

            _mockUserRepository
                .Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync((ApplicationUser?)null);

            _mockInvitationRepository
                .Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(invitation);

            _mockUserRepository
                .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync((ApplicationUser u) => u);

            _mockUserProfileRepository
                .Setup(x => x.CreateAsync(It.IsAny<UserProfile>()))
                .ReturnsAsync((UserProfile p) => p);

            _mockInvitationRepository
                .Setup(x => x.UpdateAsync(It.IsAny<UserInvitation>()))
                .ReturnsAsync((UserInvitation inv) => inv);

            _mockTokenService
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(jwtToken);

            _mockMagicLinkService
                .Setup(x => x.MarkTokenAsUsedAsync(tokenValue))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.VerifyMagicLink(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthResponse>(okResult.Value);
            Assert.Equal(jwtToken, response.Token);
            Assert.Equal(email, response.Profile.Email);

            _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
            _mockUserProfileRepository.Verify(x => x.CreateAsync(It.IsAny<UserProfile>()), Times.Once);
            _mockInvitationRepository.Verify(x => x.UpdateAsync(It.Is<UserInvitation>(i => i.IsUsed)), Times.Once);
        }

        [Fact]
        public async Task VerifyMagicLink_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new MagicLinkVerifyDto { Token = "expired-or-invalid-token" };

            _mockMagicLinkService
                .Setup(x => x.ValidateTokenAsync(request.Token))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _controller.VerifyMagicLink(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.NotNull(unauthorizedResult.Value);
        }

        [Fact]
        public async Task VerifyMagicLink_WithValidToken_UninvitedNewUser_ReturnsForbidden()
        {
            // Arrange
            var tokenValue = "valid-token-uninvited";
            var email = "notinvited@example.com";
            var request = new MagicLinkVerifyDto { Token = tokenValue };

            _mockMagicLinkService
                .Setup(x => x.ValidateTokenAsync(tokenValue))
                .ReturnsAsync(email);

            _mockUserRepository
                .Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync((ApplicationUser?)null);

            _mockInvitationRepository
                .Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync((UserInvitation?)null);

            // Act
            var result = await _controller.VerifyMagicLink(request);

            // Assert
            var forbidResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(403, forbidResult.StatusCode);
        }
    }
}
