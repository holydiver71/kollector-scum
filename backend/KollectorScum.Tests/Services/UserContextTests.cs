using System;
using System.Collections.Generic;
using System.Security.Claims;
using KollectorScum.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for the UserContext service, including admin impersonation logic
    /// </summary>
    public class UserContextTests
    {
        private readonly Mock<ILogger<UserContext>> _mockLogger;

        public UserContextTests()
        {
            _mockLogger = new Mock<ILogger<UserContext>>();
        }

        /// <summary>
        /// Creates a UserContext backed by a DefaultHttpContext with the supplied claims and headers.
        /// </summary>
        private UserContext CreateContext(
            IEnumerable<Claim>? claims = null,
            string? actAsHeader = null,
            string? userIdQuery = null)
        {
            var httpContext = new DefaultHttpContext();

            if (claims != null)
            {
                var identity = new ClaimsIdentity(claims, "TestAuth");
                httpContext.User = new ClaimsPrincipal(identity);
            }

            if (actAsHeader != null)
                httpContext.Request.Headers["X-Admin-Act-As"] = actAsHeader;

            if (userIdQuery != null)
                httpContext.Request.QueryString = new QueryString($"?userId={userIdQuery}");

            var mockAccessor = new Mock<IHttpContextAccessor>();
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            return new UserContext(mockAccessor.Object, _mockLogger.Object);
        }

        // ─── GetActingUserId ────────────────────────────────────────────────────────

        [Fact]
        public void GetActingUserId_AdminWithValidHeader_ReturnsTargetUserId()
        {
            // Arrange
            var ownId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
                new Claim("IsAdmin", "True")
            };
            var sut = CreateContext(claims, actAsHeader: targetId.ToString());

            // Act
            var result = sut.GetActingUserId();

            // Assert
            Assert.Equal(targetId, result);
        }

        [Fact]
        public void GetActingUserId_AdminWithInvalidGuidInHeader_FallsBackToOwnId()
        {
            // Arrange
            var ownId = Guid.NewGuid();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
                new Claim("IsAdmin", "True")
            };
            var sut = CreateContext(claims, actAsHeader: "not-a-guid");

            // Act
            var result = sut.GetActingUserId();

            // Assert
            Assert.Equal(ownId, result);
        }

        [Fact]
        public void GetActingUserId_AdminWithEmptyHeader_FallsBackToOwnId()
        {
            // Arrange
            var ownId = Guid.NewGuid();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
                new Claim("IsAdmin", "True")
            };
            // No header provided
            var sut = CreateContext(claims);

            // Act
            var result = sut.GetActingUserId();

            // Assert
            Assert.Equal(ownId, result);
        }

        [Fact]
        public void GetActingUserId_AdminWithQueryParam_ReturnsTargetUserId()
        {
            // Arrange
            var ownId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
                new Claim("IsAdmin", "True")
            };
            // No header, but userId query param present
            var sut = CreateContext(claims, userIdQuery: targetId.ToString());

            // Act
            var result = sut.GetActingUserId();

            // Assert
            Assert.Equal(targetId, result);
        }

        [Fact]
        public void GetActingUserId_HeaderTakesPrecedenceOverQueryParam()
        {
            // Arrange
            var ownId = Guid.NewGuid();
            var headerTargetId = Guid.NewGuid();
            var queryTargetId = Guid.NewGuid();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
                new Claim("IsAdmin", "True")
            };
            var sut = CreateContext(claims, actAsHeader: headerTargetId.ToString(), userIdQuery: queryTargetId.ToString());

            // Act
            var result = sut.GetActingUserId();

            // Assert
            Assert.Equal(headerTargetId, result);
        }

        [Fact]
        public void GetActingUserId_NonAdminWithHeader_IgnoresHeaderReturnsOwnId()
        {
            // Arrange
            var ownId = Guid.NewGuid();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
                new Claim("IsAdmin", "False")
            };
            var sut = CreateContext(claims, actAsHeader: Guid.NewGuid().ToString());

            // Act
            var result = sut.GetActingUserId();

            // Assert
            Assert.Equal(ownId, result);
        }

        [Fact]
        public void GetActingUserId_AdminWithNoHeaderOrQuery_ReturnsOwnId()
        {
            // Arrange
            var ownId = Guid.NewGuid();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
                new Claim("IsAdmin", "True")
            };
            var sut = CreateContext(claims);

            // Act
            var result = sut.GetActingUserId();

            // Assert
            Assert.Equal(ownId, result);
        }

        [Fact]
        public void GetActingUserId_UnauthenticatedUser_ReturnsNull()
        {
            // Arrange — no claims at all
            var sut = CreateContext();

            // Act
            var result = sut.GetActingUserId();

            // Assert
            Assert.Null(result);
        }

        // ─── IsAdmin ────────────────────────────────────────────────────────────────

        [Fact]
        public void IsAdmin_WithTrueIsAdminClaim_ReturnsTrue()
        {
            // Arrange
            var claims = new[] { new Claim("IsAdmin", "True") };
            var sut = CreateContext(claims);

            // Act & Assert
            Assert.True(sut.IsAdmin());
        }

        [Fact]
        public void IsAdmin_WithFalseIsAdminClaim_ReturnsFalse()
        {
            // Arrange
            var claims = new[] { new Claim("IsAdmin", "False") };
            var sut = CreateContext(claims);

            // Act & Assert
            Assert.False(sut.IsAdmin());
        }

        [Fact]
        public void IsAdmin_WithMissingClaim_ReturnsFalse()
        {
            // Arrange — only name identifier, no IsAdmin claim
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
            var sut = CreateContext(claims);

            // Act & Assert
            Assert.False(sut.IsAdmin());
        }

        // ─── GetUserId ───────────────────────────────────────────────────────────────

        [Fact]
        public void GetUserId_WithValidGuidClaim_ReturnsUserId()
        {
            // Arrange
            var expectedId = Guid.NewGuid();
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, expectedId.ToString()) };
            var sut = CreateContext(claims);

            // Act
            var result = sut.GetUserId();

            // Assert
            Assert.Equal(expectedId, result);
        }

        [Fact]
        public void GetUserId_WithMissingClaim_ReturnsNull()
        {
            // Arrange — authenticated but no NameIdentifier claim
            var claims = new[] { new Claim("IsAdmin", "True") };
            var sut = CreateContext(claims);

            // Act
            var result = sut.GetUserId();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetUserId_WithMalformedGuidClaim_ReturnsNull()
        {
            // Arrange
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
            var sut = CreateContext(claims);

            // Act
            var result = sut.GetUserId();

            // Assert
            Assert.Null(result);
        }
    }
}
