using System.Security.Claims;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Controller for admin operations (invitation and user management)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserInvitationRepository _userInvitationRepository;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IUserRepository userRepository,
            IUserInvitationRepository userInvitationRepository,
            ILogger<AdminController> logger)
        {
            _userRepository = userRepository;
            _userInvitationRepository = userInvitationRepository;
            _logger = logger;
        }

        /// <summary>
        /// Gets all invitations (admin only)
        /// </summary>
        [HttpGet("invitations")]
        [ProducesResponseType(typeof(List<UserInvitationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<UserInvitationDto>>> GetInvitations()
        {
            if (!await IsUserAdminAsync())
            {
                return Forbid();
            }

            var invitations = await _userInvitationRepository.GetAllAsync();
            var dtos = invitations.Select(i => new UserInvitationDto
            {
                Id = i.Id,
                Email = i.Email,
                CreatedAt = i.CreatedAt,
                IsUsed = i.IsUsed,
                UsedAt = i.UsedAt
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Creates a new invitation (admin only)
        /// </summary>
        [HttpPost("invitations")]
        [ProducesResponseType(typeof(UserInvitationDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserInvitationDto>> CreateInvitation([FromBody] CreateInvitationRequest request)
        {
            if (!await IsUserAdminAsync())
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Email is required" });
            }

            // Validate email format
            if (!IsValidEmail(request.Email))
            {
                return BadRequest(new { message = "Invalid email format" });
            }

            // Check if invitation already exists
            var existingInvitation = await _userInvitationRepository.FindByEmailAsync(request.Email);
            if (existingInvitation != null)
            {
                return BadRequest(new { message = "An invitation already exists for this email" });
            }

            // Check if user already has access
            var existingUser = await _userRepository.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User already has access to the application" });
            }

            var userId = GetUserIdFromClaims();
            var invitation = new UserInvitation
            {
                Email = request.Email.Trim().ToLower(),
                CreatedByUserId = userId!.Value,
                CreatedAt = DateTime.UtcNow
            };

            invitation = await _userInvitationRepository.CreateAsync(invitation);

            _logger.LogInformation("Admin {AdminId} created invitation for {Email}", userId, request.Email);

            var dto = new UserInvitationDto
            {
                Id = invitation.Id,
                Email = invitation.Email,
                CreatedAt = invitation.CreatedAt,
                IsUsed = invitation.IsUsed,
                UsedAt = invitation.UsedAt
            };

            return CreatedAtAction(nameof(GetInvitations), new { id = invitation.Id }, dto);
        }

        /// <summary>
        /// Deletes an invitation (admin only)
        /// </summary>
        [HttpDelete("invitations/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteInvitation(int id)
        {
            if (!await IsUserAdminAsync())
            {
                return Forbid();
            }

            var deleted = await _userInvitationRepository.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = "Invitation not found" });
            }

            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Admin {AdminId} deleted invitation {InvitationId}", userId, id);

            return NoContent();
        }

        /// <summary>
        /// Activates (re-enables) a previously used invitation where the user has been revoked (admin only)
        /// </summary>
        [HttpPost("invitations/{id}/activate")]
        [ProducesResponseType(typeof(UserInvitationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserInvitationDto>> ActivateInvitation(int id)
        {
            if (!await IsUserAdminAsync())
            {
                return Forbid();
            }

            var invitation = await _userInvitationRepository.FindByIdAsync(id);
            if (invitation == null)
            {
                return NotFound(new { message = "Invitation not found" });
            }

            if (!invitation.IsUsed)
            {
                return BadRequest(new { message = "Registration is already active" });
            }

            // Only allow activation if the user no longer exists (revoked)
            var existingUser = await _userRepository.FindByEmailAsync(invitation.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User is already active" });
            }

            invitation.IsUsed = false;
            invitation.UsedAt = null;
            invitation = await _userInvitationRepository.UpdateAsync(invitation);

            var adminUserId = GetUserIdFromClaims();
            _logger.LogInformation("Admin {AdminId} activated invitation {InvitationId} for {Email}", adminUserId, invitation.Id, invitation.Email);

            var dto = new UserInvitationDto
            {
                Id = invitation.Id,
                Email = invitation.Email,
                CreatedAt = invitation.CreatedAt,
                IsUsed = invitation.IsUsed,
                UsedAt = invitation.UsedAt
            };

            return Ok(dto);
        }

        /// <summary>
        /// Gets all users with access (admin only)
        /// </summary>
        [HttpGet("users")]
        [ProducesResponseType(typeof(List<UserAccessDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<UserAccessDto>>> GetUsers()
        {
            if (!await IsUserAdminAsync())
            {
                return Forbid();
            }

            var users = await _userRepository.GetAllAsync();
            var dtos = users.Select(u => new UserAccessDto
            {
                UserId = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                CreatedAt = u.CreatedAt,
                IsAdmin = u.IsAdmin
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Deactivates a user's access (admin only)
        /// </summary>
        [HttpDelete("users/{userId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeUserAccess(Guid userId)
        {
            if (!await IsUserAdminAsync())
            {
                return Forbid();
            }

            var currentUserId = GetUserIdFromClaims();
            if (userId == currentUserId)
            {
                return BadRequest(new { message = "You cannot deactivate your own access" });
            }

            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (user.IsAdmin)
            {
                return BadRequest(new { message = "Cannot deactivate access for admin users" });
            }

            await _userRepository.DeleteAsync(userId);

            _logger.LogInformation("Admin {AdminId} deactivated access for user {DeactivatedUserId}", currentUserId, userId);

            return NoContent();
        }

        private async Task<bool> IsUserAdminAsync()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return false;
            }

            var user = await _userRepository.FindByIdAsync(userId.Value);
            return user?.IsAdmin ?? false;
        }

        private Guid? GetUserIdFromClaims()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
