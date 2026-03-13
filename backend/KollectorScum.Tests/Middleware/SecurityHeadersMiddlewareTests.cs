using KollectorScum.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace KollectorScum.Tests.Middleware
{
    /// <summary>
    /// Unit tests for <see cref="SecurityHeadersMiddleware"/>.
    /// </summary>
    public class SecurityHeadersMiddlewareTests
    {
        /// <summary>
        /// Builds an <see cref="HttpContext"/> with an in-memory response.
        /// </summary>
        private static HttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            return context;
        }

        [Fact]
        public async Task InvokeAsync_AddsXContentTypeOptionsHeader()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        }

        [Fact]
        public async Task InvokeAsync_AddsXFrameOptionsHeader()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
        }

        [Fact]
        public async Task InvokeAsync_AddsXXssProtectionHeader()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("1; mode=block", context.Response.Headers["X-XSS-Protection"].ToString());
        }

        [Fact]
        public async Task InvokeAsync_AddsReferrerPolicyHeader()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"].ToString());
        }

        [Fact]
        public async Task InvokeAsync_AddsPermissionsPolicyHeader()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            var header = context.Response.Headers["Permissions-Policy"].ToString();
            Assert.Contains("camera=()", header);
            Assert.Contains("microphone=()", header);
            Assert.Contains("geolocation=()", header);
        }

        [Fact]
        public async Task InvokeAsync_AddsContentSecurityPolicyHeader()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            var csp = context.Response.Headers["Content-Security-Policy"].ToString();
            Assert.Contains("default-src 'none'", csp);
            Assert.Contains("frame-ancestors 'none'", csp);
        }

        [Fact]
        public async Task InvokeAsync_CallsNextMiddleware()
        {
            // Arrange
            var nextCalled = false;
            var context = CreateHttpContext();
            var middleware = new SecurityHeadersMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_AllSixSecurityHeadersArePresent()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            // Act
            await middleware.InvokeAsync(context);

            // Assert – verify all required headers are set
            Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
            Assert.True(context.Response.Headers.ContainsKey("X-Frame-Options"));
            Assert.True(context.Response.Headers.ContainsKey("X-XSS-Protection"));
            Assert.True(context.Response.Headers.ContainsKey("Referrer-Policy"));
            Assert.True(context.Response.Headers.ContainsKey("Permissions-Policy"));
            Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        }

        [Fact]
        public void Constructor_NullNext_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SecurityHeadersMiddleware(null!));
        }
    }
}
