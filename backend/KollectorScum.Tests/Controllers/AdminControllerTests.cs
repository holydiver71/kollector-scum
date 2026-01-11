using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Controllers
{
    public class AdminControllerTests : IDisposable
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IUserInvitationRepository> _mockInvitationRepository;
        private readonly Mock<ILogger<AdminController>> _mockLogger;
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly KollectorScumDbContext _context;
        private readonly AdminController _controller;
        private readonly Guid _adminUserId = Guid.NewGuid();

        public AdminControllerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockInvitationRepository = new Mock<IUserInvitationRepository>();
            _mockLogger = new Mock<ILogger<AdminController>>();
            _mockStorageService = new Mock<IStorageService>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Create in-memory database
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _context = new KollectorScumDbContext(options);

            _controller = new AdminController(
                _mockUserRepository.Object,
                _mockInvitationRepository.Object,
                _mockLogger.Object,
                _context,
                _mockStorageService.Object,
                _mockEnvironment.Object,
                _mockConfiguration.Object
            );

            // Set up authenticated admin user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _adminUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetInvitations_AsAdmin_ReturnsInvitations()
        {
            // Arrange
            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                IsAdmin = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(adminUser);

            var invitations = new List<UserInvitation>
            {
                new UserInvitation
                {
                    Id = 1,
                    Email = "invited@example.com",
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                }
            };
            _mockInvitationRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(invitations);

            // Act
            var result = await _controller.GetInvitations();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedInvitations = Assert.IsType<List<UserInvitationDto>>(okResult.Value);
            Assert.Single(returnedInvitations);
            Assert.Equal("invited@example.com", returnedInvitations[0].Email);
        }

        [Fact]
        public async Task GetInvitations_AsNonAdmin_ReturnsForbidden()
        {
            // Arrange
            var regularUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "user@example.com",
                IsAdmin = false
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(regularUser);

            // Act
            var result = await _controller.GetInvitations();

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task CreateInvitation_AsAdmin_WithValidEmail_CreatesInvitation()
        {
            // Arrange
            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                IsAdmin = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(adminUser);

            _mockInvitationRepository
                .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((UserInvitation?)null);

            _mockUserRepository
                .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);

            var createdInvitation = new UserInvitation
            {
                Id = 1,
                Email = "newuser@example.com",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _adminUserId
            };
            _mockInvitationRepository
                .Setup(x => x.CreateAsync(It.IsAny<UserInvitation>()))
                .ReturnsAsync(createdInvitation);

            var request = new CreateInvitationRequest { Email = "newuser@example.com" };

            // Act
            var result = await _controller.CreateInvitation(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<UserInvitationDto>(createdResult.Value);
            Assert.Equal("newuser@example.com", dto.Email);
        }

        [Fact]
        public async Task CreateInvitation_WithExistingInvitation_ReturnsBadRequest()
        {
            // Arrange
            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                IsAdmin = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(adminUser);

            var existingInvitation = new UserInvitation
            {
                Id = 1,
                Email = "existing@example.com",
                CreatedAt = DateTime.UtcNow
            };
            _mockInvitationRepository
                .Setup(x => x.FindByEmailAsync("existing@example.com"))
                .ReturnsAsync(existingInvitation);

            var request = new CreateInvitationRequest { Email = "existing@example.com" };

            // Act
            var result = await _controller.CreateInvitation(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task DeleteInvitation_AsAdmin_DeletesInvitation()
        {
            // Arrange
            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                IsAdmin = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(adminUser);

            _mockInvitationRepository
                .Setup(x => x.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteInvitation(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetUsers_AsAdmin_ReturnsUsers()
        {
            // Arrange
            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                IsAdmin = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(adminUser);

            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    CreatedAt = DateTime.UtcNow,
                    IsAdmin = false
                },
                adminUser
            };
            _mockUserRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(users);

            // Act
            var result = await _controller.GetUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUsers = Assert.IsType<List<UserAccessDto>>(okResult.Value);
            Assert.Equal(2, returnedUsers.Count);
        }

        [Fact]
        public async Task RevokeUserAccess_AsAdmin_CannotRevokeOwnAccess()
        {
            // Arrange
            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                IsAdmin = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(adminUser);

            // Act
            var result = await _controller.RevokeUserAccess(_adminUserId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RevokeUserAccess_AsAdmin_CannotRevokeAdminUser()
        {
            // Arrange
            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                IsAdmin = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(adminUser);

            var otherAdminUserId = Guid.NewGuid();
            var otherAdmin = new ApplicationUser
            {
                Id = otherAdminUserId,
                Email = "otheradmin@example.com",
                IsAdmin = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(otherAdminUserId))
                .ReturnsAsync(otherAdmin);

            // Act
            var result = await _controller.RevokeUserAccess(otherAdminUserId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ActivateInvitation_AsAdmin_WhenDeactivated_ResetsInvitationToPending()
        {
            // Arrange
            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                IsAdmin = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(adminUser);

            var deactivatedInvitation = new UserInvitation
            {
                Id = 10,
                Email = "cloudymilder@gmail.com",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsUsed = true,
                UsedAt = DateTime.UtcNow.AddHours(-2)
            };

            _mockInvitationRepository
                .Setup(x => x.FindByIdAsync(10))
                .ReturnsAsync(deactivatedInvitation);

            _mockUserRepository
                .Setup(x => x.FindByEmailAsync("cloudymilder@gmail.com"))
                .ReturnsAsync((ApplicationUser?)null);

            _mockInvitationRepository
                .Setup(x => x.UpdateAsync(It.IsAny<UserInvitation>()))
                .ReturnsAsync((UserInvitation inv) => inv);

            // Act
            var result = await _controller.ActivateInvitation(10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<UserInvitationDto>(okResult.Value);
            Assert.False(dto.IsUsed);
            Assert.Null(dto.UsedAt);
        }

        [Fact]
        public async Task ActivateInvitation_WhenInvitationAlreadyPending_ReturnsBadRequest()
        {
            // Arrange
            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                IsAdmin = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(adminUser);

            var pendingInvitation = new UserInvitation
            {
                Id = 11,
                Email = "pending@example.com",
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };
            _mockInvitationRepository
                .Setup(x => x.FindByIdAsync(11))
                .ReturnsAsync(pendingInvitation);

            // Act
            var result = await _controller.ActivateInvitation(11);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ActivateInvitation_WhenUserAlreadyActive_ReturnsBadRequest()
        {
            // Arrange
            var adminUser = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                IsAdmin = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(adminUser);

            var usedInvitation = new UserInvitation
            {
                Id = 12,
                Email = "active@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                IsUsed = true,
                UsedAt = DateTime.UtcNow.AddDays(-1)
            };
            _mockInvitationRepository
                .Setup(x => x.FindByIdAsync(12))
                .ReturnsAsync(usedInvitation);

            _mockUserRepository
                .Setup(x => x.FindByEmailAsync("active@example.com"))
                .ReturnsAsync(new ApplicationUser { Id = Guid.NewGuid(), Email = "active@example.com" });

            // Act
            var result = await _controller.ActivateInvitation(12);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }
}
