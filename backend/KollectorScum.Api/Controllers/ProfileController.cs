using System.Security.Claims;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Controller for user profile operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IUserRepository userRepository,
            IUserProfileRepository userProfileRepository,
            ILogger<ProfileController> logger)
        {
            _userRepository = userRepository;
            _userProfileRepository = userProfileRepository;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current user's profile
        /// </summary>
        /// <returns>The user profile DTO</returns>
        [HttpGet]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid user ID in token" });
            }

            var user = await _userRepository.FindByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var profile = await _userProfileRepository.GetByUserIdAsync(userId.Value);

            return Ok(new UserProfileDto
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                SelectedKollectionId = profile?.SelectedKollectionId,
                SelectedTheme = profile?.SelectedTheme ?? "metal-default",
                IsAdmin = user.IsAdmin
            });
        }

        /// <summary>
        /// Updates the current user's profile
        /// </summary>
        /// <param name="request">The update profile request</param>
        /// <returns>The updated user profile DTO</returns>
        [HttpPut]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid user ID in token" });
            }

            var user = await _userRepository.FindByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate kollection exists if provided
            if (request.SelectedKollectionId.HasValue)
            {
                var kollectionExists = await _userProfileRepository.KollectionExistsAsync(request.SelectedKollectionId.Value);
                if (!kollectionExists)
                {
                    return BadRequest(new { message = "Invalid kollection ID" });
                }
            }

            var profile = await _userProfileRepository.GetByUserIdAsync(userId.Value);

            // Allowed theme names
            var allowedThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "midnight", "metal-default", "clean-light" };
            var requestedTheme = request.SelectedTheme?.Trim();
            if (requestedTheme != null && !allowedThemes.Contains(requestedTheme))
            {
                return BadRequest(new { message = "Invalid theme name" });
            }

            if (profile == null)
            {
                // Create profile if it doesn't exist
                profile = new Models.UserProfile
                {
                    UserId = userId.Value,
                    SelectedKollectionId = request.SelectedKollectionId,
                    SelectedTheme = requestedTheme ?? "metal-default"
                };
                await _userProfileRepository.CreateAsync(profile);
            }
            else
            {
                // Update existing profile
                profile.SelectedKollectionId = request.SelectedKollectionId;
                if (requestedTheme != null)
                {
                    profile.SelectedTheme = requestedTheme;
                }
                await _userProfileRepository.UpdateAsync(profile);
            }

            _logger.LogInformation("Updated profile for user {UserId}, selected kollection: {KollectionId}, theme: {Theme}",
                userId.Value, request.SelectedKollectionId, profile.SelectedTheme);

            return Ok(new UserProfileDto
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                SelectedKollectionId = profile.SelectedKollectionId,
                SelectedTheme = profile.SelectedTheme,
                IsAdmin = user.IsAdmin
            });
        }

        /// <summary>
        /// Deletes all music releases in the user's collection
        /// </summary>
        /// <returns>The delete collection response with count of albums deleted</returns>
        [HttpDelete("collection")]
        [ProducesResponseType(typeof(DeleteCollectionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeleteCollectionResponse>> DeleteCollection()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid user ID in token" });
            }

            var user = await _userRepository.FindByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Get count before deleting
            var count = await _userProfileRepository.GetUserMusicReleaseCountAsync(userId.Value);

            // Delete all releases for the user (includes image cleanup)
            var deletedCount = await _userProfileRepository.DeleteAllUserMusicReleasesAsync(userId.Value);

            _logger.LogInformation("Deleted {DeletedCount} music releases and associated images for user {UserId}",
                deletedCount, userId.Value);

            return Ok(new DeleteCollectionResponse
            {
                AlbumsDeleted = deletedCount,
                Success = true,
                Message = $"Successfully deleted {deletedCount} album(s) from your collection."
            });
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
    }
}
