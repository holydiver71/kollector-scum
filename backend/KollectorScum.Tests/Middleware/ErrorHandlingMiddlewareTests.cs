using System.Text.Json;
using KollectorScum.Api.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace KollectorScum.Tests.Middleware
{
    /// <summary>
    /// Unit tests for <see cref="ErrorHandlingMiddleware"/>.
    /// </summary>
    public class ErrorHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ErrorHandlingMiddleware>> _mockLogger;
        private readonly Mock<IWebHostEnvironment> _mockEnv;

        public ErrorHandlingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
            _mockEnv = new Mock<IWebHostEnvironment>();
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static HttpContext CreateContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static async Task<(int StatusCode, string Body)> InvokeWithException(
            Exception exception, bool isDevelopment)
        {
            var context = CreateContext();
            await ErrorHandlingMiddleware.HandleExceptionAsync(context, exception, isDevelopment);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            return (context.Response.StatusCode, body);
        }

        // ── Status code mapping ───────────────────────────────────────────────

        [Fact]
        public async Task HandleExceptionAsync_ArgumentException_Returns400()
        {
            var (statusCode, _) = await InvokeWithException(
                new ArgumentException("bad arg"), isDevelopment: false);
            Assert.Equal(400, statusCode);
        }

        [Fact]
        public async Task HandleExceptionAsync_UnauthorizedAccessException_Returns401()
        {
            var (statusCode, _) = await InvokeWithException(
                new UnauthorizedAccessException("denied"), isDevelopment: false);
            Assert.Equal(401, statusCode);
        }

        [Fact]
        public async Task HandleExceptionAsync_NotImplementedException_Returns501()
        {
            var (statusCode, _) = await InvokeWithException(
                new NotImplementedException("not done"), isDevelopment: false);
            Assert.Equal(501, statusCode);
        }

        [Fact]
        public async Task HandleExceptionAsync_GenericException_Returns500()
        {
            var (statusCode, _) = await InvokeWithException(
                new Exception("boom"), isDevelopment: false);
            Assert.Equal(500, statusCode);
        }

        // ── Information-disclosure suppression (Phase 1.3) ───────────────────

        [Fact]
        public async Task HandleExceptionAsync_Production_DoesNotIncludeExceptionDetails()
        {
            // Arrange
            const string sensitiveDetail = "sensitive internal detail about the database connection";
            var exception = new Exception(sensitiveDetail);

            // Act
            var (_, body) = await InvokeWithException(exception, isDevelopment: false);

            // Assert
            var json = JsonDocument.Parse(body).RootElement;
            Assert.Equal("An error occurred while processing your request.",
                json.GetProperty("message").GetString());
            // details must be null / absent in non-Development
            var detailsProp = json.GetProperty("details");
            Assert.Equal(JsonValueKind.Null, detailsProp.ValueKind);
        }

        [Fact]
        public async Task HandleExceptionAsync_Development_IncludesExceptionDetails()
        {
            // Arrange
            const string detail = "specific internal message";
            var exception = new Exception(detail);

            // Act
            var (_, body) = await InvokeWithException(exception, isDevelopment: true);

            // Assert
            var json = JsonDocument.Parse(body).RootElement;
            Assert.Equal("An error occurred while processing your request.",
                json.GetProperty("message").GetString());
            Assert.Equal(detail, json.GetProperty("details").GetString());
        }

        // ── Content-Type header ───────────────────────────────────────────────

        [Fact]
        public async Task HandleExceptionAsync_SetsApplicationJsonContentType()
        {
            var context = CreateContext();
            await ErrorHandlingMiddleware.HandleExceptionAsync(
                context, new Exception("test"), isDevelopment: false);
            Assert.Equal("application/json", context.Response.ContentType);
        }

        // ── InvokeAsync integration ───────────────────────────────────────────

        [Fact]
        public async Task InvokeAsync_NoException_CallsNextAndDoesNotMutateResponse()
        {
            // Arrange
            _mockEnv.Setup(e => e.EnvironmentName).Returns("Production");

            var context = CreateContext();
            var nextCalled = false;

            var middleware = new ErrorHandlingMiddleware(
                _ => { nextCalled = true; return Task.CompletedTask; },
                _mockLogger.Object,
                _mockEnv.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode); // default, unchanged
        }

        [Fact]
        public async Task InvokeAsync_ExceptionThrown_Returns500InProduction()
        {
            // Arrange
            _mockEnv.Setup(e => e.EnvironmentName).Returns("Production");

            var context = CreateContext();

            var middleware = new ErrorHandlingMiddleware(
                _ => throw new Exception("internal error"),
                _mockLogger.Object,
                _mockEnv.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(500, context.Response.StatusCode);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var json = JsonDocument.Parse(body).RootElement;
            // Details should be null in production
            Assert.Equal(JsonValueKind.Null, json.GetProperty("details").ValueKind);
        }

        [Fact]
        public async Task InvokeAsync_ExceptionThrown_Returns500InDevelopmentWithDetails()
        {
            // Arrange
            _mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var context = CreateContext();
            const string errorMessage = "detailed dev message";

            var middleware = new ErrorHandlingMiddleware(
                _ => throw new Exception(errorMessage),
                _mockLogger.Object,
                _mockEnv.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(500, context.Response.StatusCode);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var json = JsonDocument.Parse(body).RootElement;
            Assert.Equal(errorMessage, json.GetProperty("details").GetString());
        }
    }
}
