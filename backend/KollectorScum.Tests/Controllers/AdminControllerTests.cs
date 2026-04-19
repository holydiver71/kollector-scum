using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IUserInvitationRepository> _mockInvitationRepository;
        private readonly Mock<ILogger<AdminController>> _mockLogger;
        private readonly Mock<IStorageMigrationService> _mockStorageMigrationService;
        private readonly Mock<IUserImpersonationService> _mockUserImpersonationService;
        private readonly Mock<IUserProfileRepository> _mockUserProfileRepository;
        private readonly AdminController _controller;
        private readonly Guid _adminUserId = Guid.NewGuid();

        public AdminControllerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockInvitationRepository = new Mock<IUserInvitationRepository>();
            _mockLogger = new Mock<ILogger<AdminController>>();
            _mockStorageMigrationService = new Mock<IStorageMigrationService>();
            _mockUserImpersonationService = new Mock<IUserImpersonationService>();
            _mockUserProfileRepository = new Mock<IUserProfileRepository>();

            _controller = new AdminController(
                _mockUserRepository.Object,
                _mockInvitationRepository.Object,
                _mockLogger.Object,
                _mockStorageMigrationService.Object,
                _mockUserImpersonationService.Object,
                _mockUserProfileRepository.Object
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
        public async Task ActivateInvitation_AsAdmin_WhenDeactivated_ReactivatesExistingUser()
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

            var deactivatedUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "cloudymilder@gmail.com",
                IsActive = false
            };
            _mockUserRepository
                .Setup(x => x.FindByEmailAsync("cloudymilder@gmail.com"))
                .ReturnsAsync(deactivatedUser);

            _mockUserRepository
                .Setup(x => x.SetActiveAsync(deactivatedUser.Id, true))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ActivateInvitation(10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<UserInvitationDto>(okResult.Value);
            // Invitation stays 'used' — the user's IsActive flag is what changed
            Assert.True(dto.IsUsed);
            _mockUserRepository.Verify(x => x.SetActiveAsync(deactivatedUser.Id, true), Times.Once);
        }

        [Fact]
        public async Task ActivateInvitation_AsAdmin_WhenUserNotFound_ReturnsBadRequest()
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

            var invitation = new UserInvitation
            {
                Id = 10,
                Email = "ghost@example.com",
                CreatedAt = DateTime.UtcNow,
                IsUsed = true,
                UsedAt = DateTime.UtcNow.AddHours(-1)
            };
            _mockInvitationRepository
                .Setup(x => x.FindByIdAsync(10))
                .ReturnsAsync(invitation);

            _mockUserRepository
                .Setup(x => x.FindByEmailAsync("ghost@example.com"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.ActivateInvitation(10);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task RevokeUserAccess_AsAdmin_SoftDeletesUser_PreservesCollection()
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

            var targetUserId = Guid.NewGuid();
            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                Email = "regular@example.com",
                IsAdmin = false,
                IsActive = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(targetUserId))
                .ReturnsAsync(targetUser);

            _mockUserRepository
                .Setup(x => x.SetActiveAsync(targetUserId, false))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RevokeUserAccess(targetUserId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            // Verify soft delete (SetActiveAsync) was used, NOT hard delete
            _mockUserRepository.Verify(x => x.SetActiveAsync(targetUserId, false), Times.Once);
            _mockUserRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
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

        // ─── ImpersonateUser ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds an admin user and sets up the mock to return it for the claims identity.
        /// </summary>
        private ApplicationUser SetupAdminUser()
        {
            var admin = new ApplicationUser
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(_adminUserId))
                .ReturnsAsync(admin);

            return admin;
        }

        [Fact]
        public async Task ImpersonateUser_AsAdmin_WithValidNonAdminUser_Returns200WithUserInfo()
        {
            // Arrange
            SetupAdminUser();

            var targetUserId = Guid.NewGuid();
            _mockUserImpersonationService
                .Setup(s => s.ImpersonateUserAsync(_adminUserId, targetUserId))
                .ReturnsAsync(new ImpersonationDto
                {
                    UserId = targetUserId,
                    Email = "regular@example.com",
                    DisplayName = "Regular User"
                });

            // Act
            var result = await _controller.ImpersonateUser(targetUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ImpersonationDto>(okResult.Value);
            Assert.Equal(targetUserId, dto.UserId);
            Assert.Equal("regular@example.com", dto.Email);
            Assert.Equal("Regular User", dto.DisplayName);
        }

        [Fact]
        public async Task ImpersonateUser_AsNonAdmin_ReturnsForbidden()
        {
            // Arrange — non-admin user returned for the caller
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
            var result = await _controller.ImpersonateUser(Guid.NewGuid());

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task ImpersonateUser_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            var missingId = Guid.NewGuid();
            _mockUserImpersonationService
                .Setup(s => s.ImpersonateUserAsync(_adminUserId, missingId))
                .ReturnsAsync((ImpersonationDto?)null);

            // Act
            var result = await _controller.ImpersonateUser(missingId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task ImpersonateUser_TargetUserIsAdmin_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            var otherAdminId = Guid.NewGuid();
            _mockUserImpersonationService
                .Setup(s => s.ImpersonateUserAsync(_adminUserId, otherAdminId))
                .ThrowsAsync(new InvalidOperationException("Cannot impersonate an admin user"));

            // Act
            var result = await _controller.ImpersonateUser(otherAdminId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ImpersonateUser_AdminImpersonatingSelf_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            _mockUserImpersonationService
                .Setup(s => s.ImpersonateUserAsync(_adminUserId, _adminUserId))
                .ThrowsAsync(new InvalidOperationException("Cannot impersonate yourself"));

            // Act
            var result = await _controller.ImpersonateUser(_adminUserId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ImpersonateUser_WhenUnauthenticated_ReturnsForbidden()
        {
            // Arrange — no claims on the controller context (unauthenticated)
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            _mockUserRepository
                .Setup(x => x.FindByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.ImpersonateUser(Guid.NewGuid());

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        // ─── DeleteUserCollection ────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteUserCollection_AsAdmin_WithDeactivatedUser_DeletesCollectionAndReturnsCount()
        {
            // Arrange
            SetupAdminUser();

            var targetUserId = Guid.NewGuid();
            var deactivatedUser = new ApplicationUser
            {
                Id = targetUserId,
                Email = "deactivated@example.com",
                IsAdmin = false,
                IsActive = false
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(targetUserId))
                .ReturnsAsync(deactivatedUser);

            _mockUserProfileRepository
                .Setup(x => x.DeleteAllUserMusicReleasesAsync(targetUserId))
                .ReturnsAsync(42);

            // Act
            var result = await _controller.DeleteUserCollection(targetUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<DeleteCollectionResponse>(okResult.Value);
            Assert.Equal(42, response.AlbumsDeleted);
            Assert.True(response.Success);
            _mockUserProfileRepository.Verify(x => x.DeleteAllUserMusicReleasesAsync(targetUserId), Times.Once);
        }

        [Fact]
        public async Task DeleteUserCollection_AsAdmin_WithActiveUser_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            var activeUserId = Guid.NewGuid();
            var activeUser = new ApplicationUser
            {
                Id = activeUserId,
                Email = "active@example.com",
                IsAdmin = false,
                IsActive = true
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(activeUserId))
                .ReturnsAsync(activeUser);

            // Act
            var result = await _controller.DeleteUserCollection(activeUserId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _mockUserProfileRepository.Verify(x => x.DeleteAllUserMusicReleasesAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUserCollection_AsAdmin_WithUnknownUser_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            var missingUserId = Guid.NewGuid();
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(missingUserId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.DeleteUserCollection(missingUserId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
            _mockUserProfileRepository.Verify(x => x.DeleteAllUserMusicReleasesAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUserCollection_AsNonAdmin_ReturnsForbidden()
        {
            // Arrange — non-admin user
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
            var result = await _controller.DeleteUserCollection(Guid.NewGuid());

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
            _mockUserProfileRepository.Verify(x => x.DeleteAllUserMusicReleasesAsync(It.IsAny<Guid>()), Times.Never);
        }

        // ─── GetUserCollectionCount ──────────────────────────────────────────────────

        [Fact]
        public async Task GetUserCollectionCount_AsAdmin_ReturnsCountAndEmail()
        {
            // Arrange
            SetupAdminUser();

            var targetUserId = Guid.NewGuid();
            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                Email = "collector@example.com",
                IsAdmin = false,
                IsActive = false
            };
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(targetUserId))
                .ReturnsAsync(targetUser);

            _mockUserProfileRepository
                .Setup(x => x.GetUserMusicReleaseCountAsync(targetUserId))
                .ReturnsAsync(77);

            // Act
            var result = await _controller.GetUserCollectionCount(targetUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            // Verify the anonymous object contains the expected values via reflection
            var value = okResult.Value!;
            var countProp = value.GetType().GetProperty("count")?.GetValue(value);
            var emailProp = value.GetType().GetProperty("email")?.GetValue(value);
            Assert.Equal(77, countProp);
            Assert.Equal("collector@example.com", emailProp);
        }

        [Fact]
        public async Task GetUserCollectionCount_AsAdmin_WithUnknownUser_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            var missingId = Guid.NewGuid();
            _mockUserRepository
                .Setup(x => x.FindByIdAsync(missingId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.GetUserCollectionCount(missingId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            _mockUserProfileRepository.Verify(x => x.GetUserMusicReleaseCountAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetUserCollectionCount_AsNonAdmin_ReturnsForbidden()
        {
            // Arrange — non-admin user
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
            var result = await _controller.GetUserCollectionCount(Guid.NewGuid());

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        // ─── GetAllUserCollectionCounts ──────────────────────────────────────────────

        [Fact]
        public async Task GetAllUserCollectionCounts_AsAdmin_ReturnsCountsForAllUsers()
        {
            // Arrange
            SetupAdminUser();

            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();
            var allUsers = new List<ApplicationUser>
            {
                new ApplicationUser { Id = userId1, Email = "user1@example.com", IsAdmin = false },
                new ApplicationUser { Id = userId2, Email = "user2@example.com", IsAdmin = false }
            };
            _mockUserRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(allUsers);

            var countsMap = new Dictionary<Guid, int>
            {
                { userId1, 12 },
                // userId2 has no releases — absent from the map
            };
            _mockUserProfileRepository
                .Setup(x => x.GetMusicReleaseCountsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(countsMap);

            // Act
            var result = await _controller.GetAllUserCollectionCounts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Verify the repository method was called with all user IDs
            _mockUserProfileRepository.Verify(
                x => x.GetMusicReleaseCountsAsync(It.IsAny<IEnumerable<Guid>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllUserCollectionCounts_AsNonAdmin_ReturnsForbidden()
        {
            // Arrange — non-admin user
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
            var result = await _controller.GetAllUserCollectionCounts();

            // Assert
            Assert.IsType<ForbidResult>(result);
        }
    }
}
