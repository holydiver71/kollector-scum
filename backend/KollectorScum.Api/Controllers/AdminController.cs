using System.Security.Claims;
using System.Text.Json;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly KollectorScumDbContext _context;
        private readonly IStorageService _storageService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public AdminController(
            IUserRepository userRepository,
            IUserInvitationRepository userInvitationRepository,
            ILogger<AdminController> logger,
            KollectorScumDbContext context,
            IStorageService storageService,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _userInvitationRepository = userInvitationRepository;
            _logger = logger;
            _context = context;
            _storageService = storageService;
            _environment = environment;
            _configuration = configuration;
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

        /// <summary>
        /// Debug endpoint to test file existence check
        /// </summary>
        [HttpGet("debug-file-check/{releaseId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> DebugFileCheck(int releaseId)
        {
            var release = await _context.MusicReleases.FindAsync(releaseId);
            if (release == null)
            {
                return NotFound(new { Message = "Release not found" });
            }

            var imagesPath = _configuration["ImagesPath"] ?? "/home/andy/music-images";
            var oldCoverArtPath = Path.Combine(imagesPath, "covers");

            var imagesObject = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(release.Images ?? "{}");
            var hasCoverFront = imagesObject?.ContainsKey("CoverFront") ?? false;
            var coverFrontValue = hasCoverFront ? imagesObject!["CoverFront"].GetString() : null;
            var fullPath = coverFrontValue != null ? Path.Combine(oldCoverArtPath, coverFrontValue) : null;
            var fileExists = fullPath != null ? System.IO.File.Exists(fullPath) : false;

            return Ok(new
            {
                ReleaseId = release.Id,
                Title = release.Title,
                ImagesJson = release.Images,
                ConfiguredImagesPath = imagesPath,
                OldCoverArtPath = oldCoverArtPath,
                CoverFrontValue = coverFrontValue,
                FullPath = fullPath,
                FileExists = fileExists,
                DirectoryExists = System.IO.Directory.Exists(oldCoverArtPath)
            });
        }

        /// <summary>
        /// Migrates existing cover art from flat file structure to multi-tenant structure
        /// This endpoint copies files from wwwroot/cover-art/{filename} to wwwroot/cover-art/{userId}/{filename}
        /// Only processes releases that have flat-path URLs and valid UserIds
        /// </summary>
        [HttpPost("migrate-local-storage")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> MigrateLocalStorage()
        {
            if (!await IsUserAdminAsync())
            {
                return Forbid();
            }

            var currentUserId = GetUserIdFromClaims();
            _logger.LogInformation("Admin {AdminId} initiated local storage migration", currentUserId);

            // Find all releases with Images JSON field (they contain just filenames that need migration)
            // The Images field contains JSON like: {"CoverFront":"filename.jpg","CoverBack":null,"Thumbnail":"..."}
            var releasesToMigrate = await _context.MusicReleases
                .Where(r => r.Images != null && r.Images.Contains("CoverFront"))
                .ToListAsync();

            if (!releasesToMigrate.Any())
            {
                _logger.LogInformation("No releases found with cover art to migrate");
                return Ok(new { Message = "No local images to migrate.", TotalConsidered = 0, MigratedCount = 0 });
            }

            var migratedCount = 0;
            var skippedCount = 0;
            var errors = new List<string>();
            
            // Get the old images path from configuration (where images are currently stored)
            var imagesPath = _configuration["ImagesPath"] ?? "/home/andy/music-images";
            var oldCoverArtPath = Path.Combine(imagesPath, "covers");
            
            _logger.LogInformation("Migrating cover art from {OldPath} to wwwroot/cover-art/{{userId}}", oldCoverArtPath);

            foreach (var release in releasesToMigrate)
            {
                try
                {
                    // Skip releases without a valid UserId
                    if (release.UserId == Guid.Empty)
                    {
                        _logger.LogWarning("Skipping release {ReleaseId} '{Title}' due to missing UserId", 
                            release.Id, release.Title);
                        skippedCount++;
                        continue;
                    }

                    // Parse the Images JSON to extract cover art URLs
                    if (string.IsNullOrWhiteSpace(release.Images))
                    {
                        continue;
                    }

                    var imagesObject = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(release.Images);
                    if (imagesObject == null || !imagesObject.ContainsKey("CoverFront"))
                    {
                        continue;
                    }

                    var coverFrontElement = imagesObject["CoverFront"];
                    if (coverFrontElement.ValueKind == JsonValueKind.Null || coverFrontElement.ValueKind == JsonValueKind.Undefined)
                    {
                        continue;
                    }
                    
                    var coverFrontValue = coverFrontElement.GetString();
                    if (string.IsNullOrWhiteSpace(coverFrontValue))
                    {
                        continue;
                    }

                    // Skip if already migrated (contains /cover-art/ URL pattern)
                    if (coverFrontValue.StartsWith("/cover-art/"))
                    {
                        continue;
                    }

                    // The value is just a filename (e.g., "GirlschoolScreamingBlueMu9701_f.jpeg")
                    // These files exist in /home/andy/music-images/covers/
                    var fileName = coverFrontValue;
                    
                    _logger.LogInformation("Processing release {ReleaseId} '{Title}' with filename: '{FileName}'", 
                        release.Id, release.Title, fileName);
                    
                    // Skip if it's already a full path or URL
                    if (fileName.Contains("/") || fileName.StartsWith("http"))
                    {
                        _logger.LogWarning("Skipping release {ReleaseId} - unexpected URL format: {Url}", 
                            release.Id, fileName);
                        skippedCount++;
                        continue;
                    }

                    // Check if old file exists
                    var oldFilePath = Path.Combine(oldCoverArtPath, fileName);
                    _logger.LogInformation("Checking file existence at: {FilePath}", oldFilePath);
                    
                    if (!System.IO.File.Exists(oldFilePath))
                    {
                        _logger.LogWarning("File not found for release {ReleaseId}: {FilePath}", release.Id, oldFilePath);
                        skippedCount++;
                        continue;
                    }
                    
                    _logger.LogInformation("File found, proceeding with migration for release {ReleaseId}", release.Id);

                    // Copy file using storage service
                    using (var fileStream = System.IO.File.OpenRead(oldFilePath))
                    {
                        // Determine content type from file extension
                        var extension = Path.GetExtension(fileName).ToLowerInvariant();
                        var contentType = extension switch
                        {
                            ".jpg" or ".jpeg" => "image/jpeg",
                            ".png" => "image/png",
                            ".webp" => "image/webp",
                            ".gif" => "image/gif",
                            _ => "image/jpeg"
                        };

                        var newUrl = await _storageService.UploadFileAsync(
                            "cover-art",
                            release.UserId.ToString(),
                            fileName,
                            fileStream,
                            contentType
                        );

                        // Update the Images JSON with the new URL
                        imagesObject["CoverFront"] = JsonDocument.Parse($"\"{newUrl}\"").RootElement;
                        release.Images = System.Text.Json.JsonSerializer.Serialize(imagesObject);
                        
                        // Mark the entity as modified so EF Core tracks the change
                        _context.Entry(release).Property(r => r.Images).IsModified = true;
                        
                        migratedCount++;
                        _logger.LogInformation(
                            "Migrated cover art for release {ReleaseId} '{Title}': {OldFilename} -> {NewUrl}",
                            release.Id, release.Title, fileName, newUrl);
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Failed to migrate release {release.Id} '{release.Title}': {ex.Message}";
                    _logger.LogError(ex, "Failed to migrate cover art for release {ReleaseId}", release.Id);
                    errors.Add(error);
                }
            }

            // Save all changes
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Local storage migration completed: {MigratedCount} migrated, {SkippedCount} skipped, {ErrorCount} errors",
                migratedCount, skippedCount, errors.Count);

            return Ok(new
            {
                Message = "Local storage migration completed",
                TotalConsidered = releasesToMigrate.Count,
                MigratedCount = migratedCount,
                SkippedCount = skippedCount,
                ErrorCount = errors.Count,
                Errors = errors.Take(10).ToList() // Return first 10 errors only
            });
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
