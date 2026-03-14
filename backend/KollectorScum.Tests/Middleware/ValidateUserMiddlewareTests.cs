using System.Security.Claims;
using System.Text.Json;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Middleware;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace KollectorScum.Tests.Middleware
{
    /// <summary>
    /// Unit tests for <see cref="ValidateUserMiddleware"/>.
    /// </summary>
    public class ValidateUserMiddlewareTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly ValidateUserMiddleware _middleware;
        private bool _nextCalled;

        public ValidateUserMiddlewareTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _nextCalled = false;

            _middleware = new ValidateUserMiddleware(
                _ =>
                {
                    _nextCalled = true;
                    return Task.CompletedTask;
                },
                Mock.Of<ILogger<ValidateUserMiddleware>>(),
                _memoryCache);
        }

        [Fact]
        public async Task InvokeAsync_WhenUnauthenticated_DoesNotQueryRepository()
        {
            var context = CreateContext();

            await _middleware.InvokeAsync(context, _mockUserRepository.Object);

            Assert.True(_nextCalled);
            _mockUserRepository.Verify(r => r.FindByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_WhenAuthenticatedUserMissing_ReturnsUnauthorizedAndStopsPipeline()
        {
            var userId = Guid.NewGuid();
            var context = CreateContext(userId);
            _mockUserRepository.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

            await _middleware.InvokeAsync(context, _mockUserRepository.Object);

            Assert.False(_nextCalled);
            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var json = JsonDocument.Parse(body).RootElement;
            Assert.Equal("Your access has been deactivated. Please sign in again or contact the administrator.", json.GetProperty("message").GetString());
        }

        [Fact]
        public async Task InvokeAsync_WhenAuthenticatedUserExists_CachesExistenceAcrossRequests()
        {
            var userId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, Email = "user@example.com" };
            _mockUserRepository.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);

            var firstContext = CreateContext(userId);
            await _middleware.InvokeAsync(firstContext, _mockUserRepository.Object);

            _nextCalled = false;
            var secondContext = CreateContext(userId);
            await _middleware.InvokeAsync(secondContext, _mockUserRepository.Object);

            Assert.True(_nextCalled);
            _mockUserRepository.Verify(r => r.FindByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WhenUserPreviouslyMissing_UsesCachedMissingState()
        {
            var userId = Guid.NewGuid();
            _mockUserRepository.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

            var firstContext = CreateContext(userId);
            await _middleware.InvokeAsync(firstContext, _mockUserRepository.Object);

            _nextCalled = false;
            var secondContext = CreateContext(userId);
            await _middleware.InvokeAsync(secondContext, _mockUserRepository.Object);

            Assert.False(_nextCalled);
            Assert.Equal(StatusCodes.Status401Unauthorized, secondContext.Response.StatusCode);
            _mockUserRepository.Verify(r => r.FindByIdAsync(userId), Times.Once);
        }

        private static DefaultHttpContext CreateContext(Guid? userId = null)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            if (userId.HasValue)
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
                };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                context.User = new ClaimsPrincipal(identity);
            }

            return context;
        }
    }
}
