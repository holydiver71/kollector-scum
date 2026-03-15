using System.Text.Json;
using System.Text.Json.Serialization;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        private readonly IUserInvitationRepository _userInvitationRepository;
        private readonly ITokenService _tokenService;
        private readonly IMagicLinkService _magicLinkService;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;
        private readonly ILogger<AuthController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserAuthenticationService _userAuthenticationService;

        public AuthController(
            IGoogleTokenValidator googleTokenValidator,
            IUserRepository userRepository,
            IUserProfileRepository userProfileRepository,
            IUserInvitationRepository userInvitationRepository,
            ITokenService tokenService,
            IMagicLinkService magicLinkService,
            IConfiguration configuration,
            IHostEnvironment env,
            ILogger<AuthController> logger,
            IHttpClientFactory httpClientFactory,
            IUserAuthenticationService userAuthenticationService)
        {
            _googleTokenValidator = googleTokenValidator;
            _userRepository = userRepository;
            _userProfileRepository = userProfileRepository;
            _userInvitationRepository = userInvitationRepository;
            _tokenService = tokenService;
            _magicLinkService = magicLinkService;
            _configuration = configuration;
            _env = env;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userAuthenticationService = userAuthenticationService;
        }

        /// <summary>
        /// Authenticates a user with Google ID token
        /// </summary>
        /// <param name="request">The Google authentication request</param>
        /// <returns>An authentication response with JWT token and profile</returns>
        [HttpPost("google")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AuthResponse>> GoogleAuth([FromBody] GoogleAuthRequest request)
        {
            try
            {
                // Validate the Google ID token
                var (googleSub, email, displayName) = await _googleTokenValidator.ValidateTokenAsync(request.IdToken);

                ApplicationUser user;
                try
                {
                    user = await _userAuthenticationService.FindOrCreateUserFromGoogleAsync(googleSub, email, displayName);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning("Access denied during Google auth: {Message}", ex.Message);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
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
                    SelectedKollectionId = userProfile?.SelectedKollectionId,
                    IsAdmin = user.IsAdmin
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
        /// Initiates Google OAuth authorization code flow.
        /// Redirects the browser to Google's consent screen.
        /// Not rate-limited by the strict auth policy — it only generates a redirect URL
        /// and is not a brute-force target. The global policy (100 req/min) still applies.
        /// </summary>
        [HttpGet("google/login")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GoogleLogin()
        {
            var clientId = _configuration["Google:ClientId"];
            var redirectUri = _configuration["Google:RedirectUri"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            {
                _logger.LogError("Google:ClientId or Google:RedirectUri is not configured");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "OAuth is not configured on the server." });
            }

            var authUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={Uri.EscapeDataString(clientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                "&response_type=code" +
                $"&scope={Uri.EscapeDataString("openid email profile")}" +
                "&access_type=offline" +
                "&prompt=select_account";

            _logger.LogInformation("Redirecting to Google OAuth consent screen");
            return Redirect(authUrl);
        }

        /// <summary>
        /// Handles the OAuth callback from Google.
        /// Exchanges the authorization code for an ID token, creates/updates the user,
        /// generates a JWT and redirects the browser to the frontend callback page.
        /// </summary>
        [HttpGet("google/callback")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public async Task<IActionResult> GoogleCallback([FromQuery] string? code, [FromQuery] string? error)
        {
            var frontendOrigin = GetFrontendOrigin();

            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("Google OAuth callback error: {Error}", error ?? "no code");
                return Redirect($"{frontendOrigin}/?error=google_auth_failed");
            }

            try
            {
                // Exchange authorization code for tokens
                var idToken = await ExchangeCodeForIdTokenAsync(code);

                // Validate the ID token (reuse existing logic)
                var (googleSub, email, displayName) = await _googleTokenValidator.ValidateTokenAsync(idToken);

                // Check / create user (same logic as POST /api/auth/google)
                ApplicationUser existingUser;
                try
                {
                    existingUser = await _userAuthenticationService.FindOrCreateUserFromGoogleAsync(googleSub, email, displayName);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning("Access denied in OAuth callback: {Message}", ex.Message);
                    var errorParam = ex.Message.Contains("deactivated") ? "access_deactivated" : "not_invited";
                    return Redirect($"{frontendOrigin}/?error={errorParam}");
                }

                var jwt = _tokenService.GenerateToken(existingUser);

                _logger.LogInformation("Google OAuth callback succeeded for {Email}", email);
                return Redirect($"{frontendOrigin}/auth/callback?token={Uri.EscapeDataString(jwt)}");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Invalid Google token in OAuth callback");
                return Redirect($"{frontendOrigin}/?error=invalid_token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google OAuth callback");
                return Redirect($"{frontendOrigin}/?error=auth_failed");
            }
        }

        /// <summary>
        /// Exchanges a Google authorization code for an ID token by calling Google's token endpoint.
        /// </summary>
        /// <param name="code">The authorization code returned by Google</param>
        /// <returns>The ID token string</returns>
        private async Task<string> ExchangeCodeForIdTokenAsync(string code)
        {
            var clientId = _configuration["Google:ClientId"];
            var clientSecret = _configuration["Google:ClientSecret"];
            var redirectUri = _configuration["Google:RedirectUri"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
            {
                throw new InvalidOperationException("Google OAuth configuration is incomplete (ClientId, ClientSecret or RedirectUri missing).");
            }

            using var client = _httpClientFactory.CreateClient();

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });

            var response = await client.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google token exchange failed ({Status}): {Body}", response.StatusCode, body);
                throw new UnauthorizedAccessException("Failed to exchange Google authorization code for tokens.");
            }

            var tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(body)
                ?? throw new InvalidOperationException("Failed to deserialize Google token response.");

            if (string.IsNullOrEmpty(tokenData.IdToken))
            {
                throw new InvalidOperationException("Google token response did not contain an id_token.");
            }

            return tokenData.IdToken;
        }

        /// <summary>
        /// Returns the first configured frontend origin for redirect purposes.
        /// </summary>
        private string GetFrontendOrigin()
        {
            var raw = _configuration["Frontend:Origins"]
                   ?? _configuration["Frontend:Origin"]
                   ?? _configuration["FRONTEND_ORIGINS"]
                   ?? _configuration["FRONTEND_ORIGIN"]
                   ?? "http://localhost:3000";

            return raw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)[0].TrimEnd('/');
        }

        /// <summary>
        /// Minimal DTO for deserializing Google's token endpoint response.
        /// </summary>
        private sealed class GoogleTokenResponse
        {
            [JsonPropertyName("id_token")]
            public string? IdToken { get; init; }

            [JsonPropertyName("access_token")]
            public string? AccessToken { get; init; }
        }

        /// <summary>
        /// Initiates passwordless authentication by sending a magic link to the provided email address.
        /// Only invited users can request a magic link. The response is deliberately vague (always HTTP 200)
        /// to prevent email enumeration — callers cannot determine whether a given email is registered.
        /// </summary>
        /// <param name="request">The magic link request containing the email address</param>
        /// <returns>200 OK with a generic message regardless of whether the email is on the invite list</returns>
        [HttpPost("magic-link/request")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestMagicLink([FromBody] MagicLinkRequestDto request)
        {
            try
            {
                var email = request.Email.ToLowerInvariant();

                // Check if the email is on the invitation list
                var invitation = await _userInvitationRepository.FindByEmailAsync(email);
                if (invitation == null)
                {
                    _logger.LogWarning("Magic link requested for uninvited email: {Email}", email);
                    // Return 200 to avoid email enumeration (don't reveal whether email is registered)
                    return Ok(new { message = "If your email is registered, you will receive a sign-in link shortly." });
                }

                var frontendOrigin = GetFrontendOrigin();
                await _magicLinkService.CreateAndSendTokenAsync(email, frontendOrigin);

                return Ok(new { message = "If your email is registered, you will receive a sign-in link shortly." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing magic link request");
                return BadRequest(new { message = "Failed to process request. Please try again." });
            }
        }

        /// <summary>
        /// Verifies a magic link token and returns a JWT for the authenticated user.
        /// The token must not have expired and must not have been used before (single-use enforcement).
        /// On first sign-in, a new user account and default profile are automatically created.
        /// The token is marked as used immediately upon successful verification to prevent replay attacks.
        /// </summary>
        /// <param name="request">The verify request containing the token</param>
        /// <returns>Authentication response with JWT token and user profile</returns>
        [HttpPost("magic-link/verify")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AuthResponse>> VerifyMagicLink([FromBody] MagicLinkVerifyDto request)
        {
            try
            {
                // Validate the token and retrieve the associated email
                var email = await _magicLinkService.ValidateTokenAsync(request.Token);
                if (email == null)
                {
                    _logger.LogWarning("Magic link token validation failed");
                    return Unauthorized(new { message = "Invalid or expired sign-in link. Please request a new one." });
                }

                // Find or create the user
                ApplicationUser user;
                try
                {
                    user = await _userAuthenticationService.FindOrCreateUserFromEmailAsync(email);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning("Access denied during magic link verify: {Message}", ex.Message);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
                }

                // Mark the token as used (single-use enforcement)
                await _magicLinkService.MarkTokenAsUsedAsync(request.Token);

                // Generate JWT token
                var jwt = _tokenService.GenerateToken(user);

                // Get user profile
                var userProfile = await _userProfileRepository.GetByUserIdAsync(user.Id);

                var profileDto = new UserProfileDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    SelectedKollectionId = userProfile?.SelectedKollectionId,
                    IsAdmin = user.IsAdmin
                };

                _logger.LogInformation("Magic link authentication successful for {Email}", email);

                return Ok(new AuthResponse
                {
                    Token = jwt,
                    Profile = profileDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying magic link token");
                return BadRequest(new { message = "Authentication failed. Please try again." });
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
                    GoogleSub = user.GoogleSub ?? string.Empty
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
