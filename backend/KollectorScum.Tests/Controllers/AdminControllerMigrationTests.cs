using System.Security.Claims;
using System.Text.Json;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Tests for AdminController's migration endpoint.
    /// Verifies authorization and delegation to IStorageMigrationService.
    /// </summary>
    public class AdminControllerMigrationTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IUserInvitationRepository> _mockInvitationRepository;
        private readonly Mock<ILogger<AdminController>> _mockLogger;
        private readonly Mock<IStorageMigrationService> _mockStorageMigrationService;
        private readonly Mock<IUserImpersonationService> _mockUserImpersonationService;
        private readonly AdminController _controller;
        private readonly Guid _adminUserId = Guid.NewGuid();
        private readonly Guid _testUserId = Guid.NewGuid();

        public AdminControllerMigrationTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockInvitationRepository = new Mock<IUserInvitationRepository>();
            _mockLogger = new Mock<ILogger<AdminController>>();
            _mockStorageMigrationService = new Mock<IStorageMigrationService>();
            _mockUserImpersonationService = new Mock<IUserImpersonationService>();

            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@test.com",
                IsAdmin = true
            };
            _mockUserRepository.Setup(x => x.FindByIdAsync(_adminUserId)).ReturnsAsync(adminUser);

            _controller = new AdminController(
                _mockUserRepository.Object,
                _mockInvitationRepository.Object,
                _mockLogger.Object,
                _mockStorageMigrationService.Object,
                _mockUserImpersonationService.Object
            );

            SetupAdminUser();
        }

        private void SetupAdminUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _adminUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        private void SetupNonAdminUser()
        {
            var nonAdminId = Guid.NewGuid();
            var nonAdminUser = new ApplicationUser { Id = nonAdminId, Email = "user@test.com", IsAdmin = false };
            _mockUserRepository.Setup(x => x.FindByIdAsync(nonAdminId)).ReturnsAsync(nonAdminUser);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, nonAdminId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        #region Authorization Tests

        [Fact]
        public async Task MigrateLocalStorage_AsNonAdmin_ReturnsForbid()
        {
            SetupNonAdminUser();

            var result = await _controller.MigrateLocalStorage();

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task MigrateLocalStorage_AsAdmin_ReturnsOk()
        {
            _mockStorageMigrationService.Setup(s => s.MigrateLocalStorageAsync(null))
                .ReturnsAsync(new StorageMigrationResult { TotalConsidered = 0 });

            var result = await _controller.MigrateLocalStorage();

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region Delegation Tests

        [Fact]
        public async Task MigrateLocalStorage_WithNoReleases_ReturnsNoMigratedMessage()
        {
            _mockStorageMigrationService.Setup(s => s.MigrateLocalStorageAsync(null))
                .ReturnsAsync(new StorageMigrationResult { TotalConsidered = 0, MigratedCount = 0 });

            var result = await _controller.MigrateLocalStorage();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);

            var message = responseObj.GetProperty("Message").GetString();
            Assert.Contains("No local images", message);
        }

        [Fact]
        public async Task MigrateLocalStorage_WithMigratedReleases_ReturnsSummary()
        {
            _mockStorageMigrationService.Setup(s => s.MigrateLocalStorageAsync(null))
                .ReturnsAsync(new StorageMigrationResult
                {
                    TotalConsidered = 5,
                    MigratedCount = 4,
                    SkippedCount = 1,
                    Errors = new List<string>()
                });

            var result = await _controller.MigrateLocalStorage();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);

            Assert.Equal(5, responseObj.GetProperty("TotalConsidered").GetInt32());
            Assert.Equal(4, responseObj.GetProperty("MigratedCount").GetInt32());
            Assert.Equal(1, responseObj.GetProperty("SkippedCount").GetInt32());
        }

        [Fact]
        public async Task MigrateLocalStorage_WithReleaseId_CallsServiceWithReleaseId()
        {
            var releaseId = 42;
            _mockStorageMigrationService.Setup(s => s.MigrateLocalStorageAsync(releaseId))
                .ReturnsAsync(new StorageMigrationResult { TotalConsidered = 1, MigratedCount = 1 });

            var result = await _controller.MigrateLocalStorage(releaseId);

            _mockStorageMigrationService.Verify(s => s.MigrateLocalStorageAsync(releaseId), Times.Once);
        }

        [Fact]
        public async Task MigrateLocalStorage_WithErrors_ReturnsErrorsInResponse()
        {
            _mockStorageMigrationService.Setup(s => s.MigrateLocalStorageAsync(null))
                .ReturnsAsync(new StorageMigrationResult
                {
                    TotalConsidered = 2,
                    MigratedCount = 1,
                    SkippedCount = 0,
                    Errors = new List<string> { "Failed to migrate release 2: Disk full" }
                });

            var result = await _controller.MigrateLocalStorage();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(json);

            Assert.Equal(1, responseObj.GetProperty("ErrorCount").GetInt32());
            Assert.True(responseObj.GetProperty("Errors").GetArrayLength() > 0);
        }

        #endregion
    }
}
