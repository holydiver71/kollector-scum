using System;
using System.Threading.Tasks;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for the MagicLinkService
    /// </summary>
    public class MagicLinkServiceTests
    {
        private readonly Mock<IMagicLinkTokenRepository> _mockTokenRepository;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<MagicLinkService>> _mockLogger;
        private readonly MagicLinkService _service;

        public MagicLinkServiceTests()
        {
            _mockTokenRepository = new Mock<IMagicLinkTokenRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<MagicLinkService>>();

            _mockConfiguration.Setup(x => x["Email:MagicLinkExpiryMinutes"]).Returns("15");

            _service = new MagicLinkService(
                _mockTokenRepository.Object,
                _mockEmailService.Object,
                _mockConfiguration.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task CreateAndSendTokenAsync_CreatesTokenAndSendsEmail()
        {
            // Arrange
            var email = "user@example.com";
            var frontendOrigin = "http://localhost:3000";

            _mockTokenRepository
                .Setup(x => x.CreateAsync(It.IsAny<MagicLinkToken>()))
                .ReturnsAsync((MagicLinkToken t) =>
                {
                    t.Id = 1;
                    return t;
                });

            _mockEmailService
                .Setup(x => x.SendMagicLinkEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAndSendTokenAsync(email, frontendOrigin);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));
            Assert.True(result.ExpiresAt > DateTime.UtcNow);

            _mockTokenRepository.Verify(x => x.CreateAsync(It.IsAny<MagicLinkToken>()), Times.Once);
            _mockEmailService.Verify(
                x => x.SendMagicLinkEmailAsync(
                    email,
                    It.Is<string>(link => link.Contains(result.Token))),
                Times.Once);
        }

        [Fact]
        public async Task CreateAndSendTokenAsync_NormalizesEmailToLowercase()
        {
            // Arrange
            var email = "User@EXAMPLE.COM";
            var expectedEmail = "user@example.com";

            _mockTokenRepository
                .Setup(x => x.CreateAsync(It.IsAny<MagicLinkToken>()))
                .ReturnsAsync((MagicLinkToken t) => t);

            _mockEmailService
                .Setup(x => x.SendMagicLinkEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAndSendTokenAsync(email, "http://localhost:3000");

            // Assert
            Assert.Equal(expectedEmail, result.Email);
        }

        [Fact]
        public async Task CreateAndSendTokenAsync_MagicLinkContainsFrontendOriginAndToken()
        {
            // Arrange
            var email = "user@example.com";
            var frontendOrigin = "https://app.example.com";
            string? capturedLink = null;

            _mockTokenRepository
                .Setup(x => x.CreateAsync(It.IsAny<MagicLinkToken>()))
                .ReturnsAsync((MagicLinkToken t) => t);

            _mockEmailService
                .Setup(x => x.SendMagicLinkEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((_, link) => capturedLink = link)
                .Returns(Task.CompletedTask);

            // Act
            await _service.CreateAndSendTokenAsync(email, frontendOrigin);

            // Assert
            Assert.NotNull(capturedLink);
            Assert.StartsWith(frontendOrigin, capturedLink);
            Assert.Contains("/auth/magic-link?token=", capturedLink);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithValidToken_ReturnsEmail()
        {
            // Arrange
            var token = "valid-token-abc";
            var email = "user@example.com";

            var record = new MagicLinkToken
            {
                Id = 1,
                Email = email,
                Token = token,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            _mockTokenRepository
                .Setup(x => x.FindByTokenAsync(token))
                .ReturnsAsync(record);

            // Act
            var result = await _service.ValidateTokenAsync(token);

            // Assert
            Assert.Equal(email, result);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithNonexistentToken_ReturnsNull()
        {
            // Arrange
            _mockTokenRepository
                .Setup(x => x.FindByTokenAsync(It.IsAny<string>()))
                .ReturnsAsync((MagicLinkToken?)null);

            // Act
            var result = await _service.ValidateTokenAsync("nonexistent-token");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithAlreadyUsedToken_ReturnsNull()
        {
            // Arrange
            var token = "used-token";
            var record = new MagicLinkToken
            {
                Id = 1,
                Email = "user@example.com",
                Token = token,
                IsUsed = true,
                UsedAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _mockTokenRepository
                .Setup(x => x.FindByTokenAsync(token))
                .ReturnsAsync(record);

            // Act
            var result = await _service.ValidateTokenAsync(token);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithExpiredToken_ReturnsNull()
        {
            // Arrange
            var token = "expired-token";
            var record = new MagicLinkToken
            {
                Id = 1,
                Email = "user@example.com",
                Token = token,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // already expired
            };

            _mockTokenRepository
                .Setup(x => x.FindByTokenAsync(token))
                .ReturnsAsync(record);

            // Act
            var result = await _service.ValidateTokenAsync(token);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task MarkTokenAsUsedAsync_SetsIsUsedAndUsedAt()
        {
            // Arrange
            var token = "use-me-token";
            MagicLinkToken? updatedToken = null;

            var record = new MagicLinkToken
            {
                Id = 1,
                Email = "user@example.com",
                Token = token,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            _mockTokenRepository
                .Setup(x => x.FindByTokenAsync(token))
                .ReturnsAsync(record);

            _mockTokenRepository
                .Setup(x => x.UpdateAsync(It.IsAny<MagicLinkToken>()))
                .Callback<MagicLinkToken>(t => updatedToken = t)
                .ReturnsAsync((MagicLinkToken t) => t);

            // Act
            await _service.MarkTokenAsUsedAsync(token);

            // Assert
            Assert.NotNull(updatedToken);
            Assert.True(updatedToken!.IsUsed);
            Assert.NotNull(updatedToken.UsedAt);
        }

        [Fact]
        public async Task MarkTokenAsUsedAsync_WithNonexistentToken_DoesNotThrow()
        {
            // Arrange
            _mockTokenRepository
                .Setup(x => x.FindByTokenAsync(It.IsAny<string>()))
                .ReturnsAsync((MagicLinkToken?)null);

            // Act & Assert - should not throw
            await _service.MarkTokenAsUsedAsync("nonexistent");
            _mockTokenRepository.Verify(x => x.UpdateAsync(It.IsAny<MagicLinkToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAndSendTokenAsync_UsesConfiguredExpiryMinutes()
        {
            // Arrange
            var expiryMinutes = 30;
            _mockConfiguration.Setup(x => x["Email:MagicLinkExpiryMinutes"]).Returns(expiryMinutes.ToString());

            var service = new MagicLinkService(
                _mockTokenRepository.Object,
                _mockEmailService.Object,
                _mockConfiguration.Object,
                _mockLogger.Object
            );

            MagicLinkToken? createdToken = null;
            _mockTokenRepository
                .Setup(x => x.CreateAsync(It.IsAny<MagicLinkToken>()))
                .Callback<MagicLinkToken>(t => createdToken = t)
                .ReturnsAsync((MagicLinkToken t) => t);

            _mockEmailService
                .Setup(x => x.SendMagicLinkEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await service.CreateAndSendTokenAsync("user@example.com", "http://localhost:3000");

            // Assert
            Assert.NotNull(createdToken);
            var expectedExpiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
            Assert.True(createdToken!.ExpiresAt <= expectedExpiry.AddSeconds(5));
            Assert.True(createdToken.ExpiresAt >= expectedExpiry.AddSeconds(-5));
        }
    }
}
