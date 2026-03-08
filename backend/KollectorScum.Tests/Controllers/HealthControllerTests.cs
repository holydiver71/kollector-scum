using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.Services;
using KollectorScum.Api.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Unit tests for HealthController
    /// </summary>
    public class HealthControllerTests
    {
        private readonly Mock<ILogger<HealthController>> _mockLogger;
        private readonly Mock<IDataSeedingService> _mockDataSeedingService;
        private readonly Mock<HealthCheckService> _mockHealthCheckService;
        private readonly HealthController _controller;

        /// <summary>Builds a HealthReport with a single database entry at the given status.</summary>
        private static HealthReport BuildReport(HealthStatus dbStatus)
        {
            var entries = new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    dbStatus, description: null, duration: TimeSpan.Zero,
                    exception: null, data: null)
            };
            return new HealthReport(entries, dbStatus, TimeSpan.Zero);
        }

        public HealthControllerTests()
        {
            _mockLogger = new Mock<ILogger<HealthController>>();
            _mockDataSeedingService = new Mock<IDataSeedingService>();
            _mockHealthCheckService = new Mock<HealthCheckService>();

            // Default: database is healthy
            _mockHealthCheckService
                .Setup(s => s.CheckHealthAsync(
                    It.IsAny<Func<HealthCheckRegistration, bool>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildReport(HealthStatus.Healthy));

            _controller = new HealthController(
                _mockLogger.Object,
                _mockDataSeedingService.Object,
                _mockHealthCheckService.Object);
        }

        [Fact]
        public async Task Get_ReturnsOkResult_WithHealthyStatus()
        {
            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var healthStatus = okResult.Value;

            Assert.NotNull(healthStatus);

            var statusProperty   = healthStatus.GetType().GetProperty("Status");
            var dbStatusProperty = healthStatus.GetType().GetProperty("DbStatus");
            var timestampProperty = healthStatus.GetType().GetProperty("Timestamp");

            Assert.NotNull(statusProperty);
            Assert.NotNull(dbStatusProperty);
            Assert.NotNull(timestampProperty);
            Assert.Equal("Healthy",  statusProperty.GetValue(healthStatus));
            Assert.Equal("Healthy", dbStatusProperty.GetValue(healthStatus));
        }

        [Fact]
        public async Task Get_ReturnsDbUnhealthy_WhenDatabaseCheckFails()
        {
            // Arrange
            _mockHealthCheckService
                .Setup(s => s.CheckHealthAsync(
                    It.IsAny<Func<HealthCheckRegistration, bool>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildReport(HealthStatus.Unhealthy));

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var healthStatus = okResult.Value!;
            var dbStatusProperty = healthStatus.GetType().GetProperty("DbStatus");
            Assert.Equal("Unhealthy", dbStatusProperty!.GetValue(healthStatus));
        }

        [Fact]
        public async Task Get_ReturnsDegraded_WhenHealthCheckServiceThrows()
        {
            // Arrange
            _mockHealthCheckService
                .Setup(s => s.CheckHealthAsync(
                    It.IsAny<Func<HealthCheckRegistration, bool>?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("connection refused"));

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var healthStatus = okResult.Value!;
            var statusProperty   = healthStatus.GetType().GetProperty("Status");
            var dbStatusProperty = healthStatus.GetType().GetProperty("DbStatus");
            Assert.Equal("Degraded",   statusProperty!.GetValue(healthStatus));
            Assert.Equal("Unhealthy", dbStatusProperty!.GetValue(healthStatus));
        }

        [Fact]
        public async Task SeedData_ReturnsOkResult_WhenSeedingSucceeds()
        {
            // Arrange
            _mockDataSeedingService.Setup(s => s.SeedLookupDataAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SeedData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            
            Assert.NotNull(response);
            
            // Verify the response structure
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Data seeding completed successfully", messageProperty.GetValue(response));

            // Verify the service was called
            _mockDataSeedingService.Verify(s => s.SeedLookupDataAsync(), Times.Once);
        }

        [Fact]
        public async Task SeedData_ReturnsInternalServerError_WhenSeedingFails()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Seeding failed");
            _mockDataSeedingService.Setup(s => s.SeedLookupDataAsync())
                .ThrowsAsync(expectedException);

            // Act
            var result = await _controller.SeedData();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = statusCodeResult.Value;
            
            Assert.NotNull(response);
            
            // Verify error response structure
            var messageProperty = response.GetType().GetProperty("Message");
            var errorProperty = response.GetType().GetProperty("Error");
            
            Assert.NotNull(messageProperty);
            Assert.NotNull(errorProperty);
            Assert.Equal("An error occurred during data seeding", messageProperty.GetValue(response));
            Assert.Equal("Seeding failed", errorProperty.GetValue(response));
        }

        [Fact]
        public async Task SeedMusicReleases_ReturnsOkResult_WhenSeedingSucceeds()
        {
            // Arrange
            _mockDataSeedingService.Setup(s => s.SeedMusicReleasesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SeedMusicReleases();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            
            Assert.NotNull(response);
            
            // Verify the response structure
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Music releases seeding completed successfully", messageProperty.GetValue(response));

            // Verify the service was called
            _mockDataSeedingService.Verify(s => s.SeedMusicReleasesAsync(), Times.Once);
        }

        [Fact]
        public async Task SeedMusicReleases_ReturnsInternalServerError_WhenSeedingFails()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Music release seeding failed");
            _mockDataSeedingService.Setup(s => s.SeedMusicReleasesAsync())
                .ThrowsAsync(expectedException);

            // Act
            var result = await _controller.SeedMusicReleases();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = statusCodeResult.Value;
            
            Assert.NotNull(response);
            
            // Verify error response structure
            var messageProperty = response.GetType().GetProperty("Message");
            var errorProperty = response.GetType().GetProperty("Error");
            
            Assert.NotNull(messageProperty);
            Assert.NotNull(errorProperty);
            Assert.Equal("An error occurred during music releases seeding", messageProperty.GetValue(response));
            Assert.Equal("Music release seeding failed", errorProperty.GetValue(response));
        }
    }
}
