using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Controller for authentication operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IGoogleTokenValidator _googleTokenValidator;
        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IGoogleTokenValidator googleTokenValidator,
            IUserRepository userRepository,
            IUserProfileRepository userProfileRepository,
            ITokenService tokenService,
            IConfiguration configuration,
            IHostEnvironment env,
            ILogger<AuthController> logger)
        {
            _googleTokenValidator = googleTokenValidator;
            _userRepository = userRepository;
            _userProfileRepository = userProfileRepository;
            _tokenService = tokenService;
            _configuration = configuration;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user with Google ID token
        /// </summary>
        /// <param name="request">The Google authentication request</param>
        /// <returns>An authentication response with JWT token and profile</returns>
        [HttpPost("google")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> GoogleAuth([FromBody] GoogleAuthRequest request)
        {
            try
            {
                // Validate the Google ID token
                var (googleSub, email, displayName) = await _googleTokenValidator.ValidateTokenAsync(request.IdToken);

                // Find or create user
                var user = await _userRepository.FindByGoogleSubAsync(googleSub);
                if (user == null)
                {
                    _logger.LogInformation("Creating new user for Google sub {GoogleSub}", googleSub);
                    user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        GoogleSub = googleSub,
                        Email = email,
                        DisplayName = displayName
                    };
                    user = await _userRepository.CreateAsync(user);

                    // Create a default user profile
                    var profile = new UserProfile
                    {
                        UserId = user.Id,
                        SelectedKollectionId = null
                    };
                    await _userProfileRepository.CreateAsync(profile);
                }
                else
                {
                    // Update user info if changed
                    if (user.Email != email || user.DisplayName != displayName)
                    {
                        user.Email = email;
                        user.DisplayName = displayName;
                        await _userRepository.UpdateAsync(user);
                    }
                }

                // Generate JWT token
                var token = _tokenService.GenerateToken(user);

                // Get user profile
                var userProfile = await _userProfileRepository.GetByUserIdAsync(user.Id);

                var profileDto = new UserProfileDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    SelectedKollectionId = userProfile?.SelectedKollectionId
                };

                return Ok(new AuthResponse
                {
                    Token = token,
                    Profile = profileDto
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized Google authentication attempt");
                return Unauthorized(new { message = "Invalid Google token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google authentication");
                return BadRequest(new { message = "Authentication failed" });
            }
        }

        /// <summary>
        /// Temporary bootstrap endpoint to capture Google sub and create mapping to UserId.
        /// Dev-only: gated by environment and feature flag.
        /// </summary>
        [HttpPost("bootstrap")]
        [ProducesResponseType(typeof(BootstrapResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BootstrapResponse>> Bootstrap([FromBody] BootstrapRequest request)
        {
            // Gate: only allow in Development and when feature flag enabled
            var enabled = _configuration.GetValue<bool>("Features:EnableBootstrapEndpoint");
            if (!_env.IsDevelopment() || !enabled)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bootstrap endpoint disabled" });
            }

            // Optional simple secret to avoid accidental exposure
            var configuredSecret = _configuration.GetValue<string>("Features:BootstrapSecret");
            if (!string.IsNullOrEmpty(configuredSecret))
            {
                var headerSecret = Request.Headers["X-Bootstrap-Secret"].FirstOrDefault();
                if (!string.Equals(configuredSecret, headerSecret))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Invalid bootstrap secret" });
                }
            }

            try
            {
                string googleSub;
                string email;
                string? displayName;

                if (!string.IsNullOrWhiteSpace(request.IdToken))
                {
                    // Validate the Google ID token provided in the request body
                    (googleSub, email, displayName) = await _googleTokenValidator.ValidateTokenAsync(request.IdToken);
                }
                else
                {
                    return BadRequest(new { message = "IdToken is required for bootstrap" });
                }

                var user = await _userRepository.FindByGoogleSubAsync(googleSub);
                if (user == null)
                {
                    _logger.LogInformation("Bootstrap: creating new user for Google sub {GoogleSub}", googleSub);
                    user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        GoogleSub = googleSub,
                        Email = email,
                        DisplayName = displayName
                    };
                    user = await _userRepository.CreateAsync(user);

                    var profile = new UserProfile
                    {
                        UserId = user.Id,
                        SelectedKollectionId = null
                    };
                    await _userProfileRepository.CreateAsync(profile);
                }

                var response = new BootstrapResponse
                {
                    UserId = user.Id,
                    GoogleSub = user.GoogleSub
                };

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized bootstrap attempt");
                return Unauthorized(new { message = "Invalid Google token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bootstrap");
                return BadRequest(new { message = "Bootstrap failed" });
            }
        }
    }
}
