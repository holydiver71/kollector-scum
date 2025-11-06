using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using KollectorScum.Api.Controllers;
using System;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Unit tests for BaseApiController
    /// </summary>
    public class BaseApiControllerTests
    {
        // Concrete implementation of BaseApiController for testing
        public class TestController : BaseApiController
        {
            public TestController(ILogger<TestController> logger) : base(logger)
            {
            }

            // Expose protected methods for testing
            public new ActionResult HandleError(Exception ex, string context) => base.HandleError(ex, context);
            public new ActionResult? ValidatePaginationParameters(int page, int pageSize, int maxPageSize = 5000) 
                => base.ValidatePaginationParameters(page, pageSize, maxPageSize);
            public new void LogOperation(string operation, object? parameters = null) 
                => base.LogOperation(operation, parameters);
        }

        private readonly Mock<ILogger<TestController>> _mockLogger;
        private readonly TestController _controller;

        public BaseApiControllerTests()
        {
            _mockLogger = new Mock<ILogger<TestController>>();
            _controller = new TestController(_mockLogger.Object);
        }

        #region HandleError Tests

        [Fact]
        public void HandleError_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var exception = new ArgumentException("Invalid argument");

            // Act
            var result = _controller.HandleError(exception, "TestOperation");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid argument", badRequestResult.Value);
        }

        [Fact]
        public void HandleError_KeyNotFoundException_ReturnsNotFound()
        {
            // Arrange
            var exception = new KeyNotFoundException("Entity not found");

            // Act
            var result = _controller.HandleError(exception, "TestOperation");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Entity not found", notFoundResult.Value);
        }

        [Fact]
        public void HandleError_InvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var exception = new InvalidOperationException("Invalid operation");

            // Act
            var result = _controller.HandleError(exception, "TestOperation");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid operation", badRequestResult.Value);
        }

        [Fact]
        public void HandleError_GenericException_ReturnsInternalServerError()
        {
            // Arrange
            var exception = new Exception("Unexpected error");

            // Act
            var result = _controller.HandleError(exception, "TestOperation");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while processing your request", statusCodeResult.Value);
        }

        [Fact]
        public void HandleError_LogsErrorWithContext()
        {
            // Arrange
            var exception = new Exception("Test error");
            var context = "TestOperation";

            // Act
            _controller.HandleError(exception, context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(context)),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region ValidatePaginationParameters Tests

        [Fact]
        public void ValidatePaginationParameters_ValidParameters_ReturnsNull()
        {
            // Arrange
            int page = 1;
            int pageSize = 50;

            // Act
            var result = _controller.ValidatePaginationParameters(page, pageSize);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidatePaginationParameters_PageLessThanOne_ReturnsBadRequest()
        {
            // Arrange
            int page = 0;
            int pageSize = 50;

            // Act
            var result = _controller.ValidatePaginationParameters(page, pageSize);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Page must be greater than 0", badRequestResult.Value);
        }

        [Fact]
        public void ValidatePaginationParameters_NegativePage_ReturnsBadRequest()
        {
            // Arrange
            int page = -1;
            int pageSize = 50;

            // Act
            var result = _controller.ValidatePaginationParameters(page, pageSize);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Page must be greater than 0", badRequestResult.Value);
        }

        [Fact]
        public void ValidatePaginationParameters_PageSizeLessThanOne_ReturnsBadRequest()
        {
            // Arrange
            int page = 1;
            int pageSize = 0;

            // Act
            var result = _controller.ValidatePaginationParameters(page, pageSize);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Page size must be between 1 and 5000", badRequestResult.Value);
        }

        [Fact]
        public void ValidatePaginationParameters_PageSizeExceedsMax_ReturnsBadRequest()
        {
            // Arrange
            int page = 1;
            int pageSize = 5001;

            // Act
            var result = _controller.ValidatePaginationParameters(page, pageSize);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Page size must be between 1 and 5000", badRequestResult.Value);
        }

        [Fact]
        public void ValidatePaginationParameters_CustomMaxPageSize_ValidatesCorrectly()
        {
            // Arrange
            int page = 1;
            int pageSize = 150;
            int maxPageSize = 100;

            // Act
            var result = _controller.ValidatePaginationParameters(page, pageSize, maxPageSize);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Page size must be between 1 and 100", badRequestResult.Value);
        }

        [Fact]
        public void ValidatePaginationParameters_MaximumAllowedPageSize_ReturnsNull()
        {
            // Arrange
            int page = 1;
            int pageSize = 5000;

            // Act
            var result = _controller.ValidatePaginationParameters(page, pageSize);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region LogOperation Tests

        [Fact]
        public void LogOperation_WithoutParameters_LogsOperationName()
        {
            // Arrange
            var operation = "TestOperation";

            // Act
            _controller.LogOperation(operation);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(operation)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogOperation_WithParameters_LogsOperationAndParameters()
        {
            // Arrange
            var operation = "TestOperation";
            var parameters = new { id = 123, name = "test" };

            // Act
            _controller.LogOperation(operation, parameters);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(operation)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogOperation_WithNullParameters_LogsOnlyOperation()
        {
            // Arrange
            var operation = "TestOperation";

            // Act
            _controller.LogOperation(operation, null);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(operation)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestController(null!));
        }

        #endregion
    }
}
