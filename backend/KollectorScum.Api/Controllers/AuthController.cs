using System.Text.Json;
using System.Text.Json.Serialization;
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
        private readonly IUserInvitationRepository _userInvitationRepository;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;
        private readonly ILogger<AuthController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(
            IGoogleTokenValidator googleTokenValidator,
            IUserRepository userRepository,
            IUserProfileRepository userProfileRepository,
            IUserInvitationRepository userInvitationRepository,
            ITokenService tokenService,
            IConfiguration configuration,
            IHostEnvironment env,
            ILogger<AuthController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _googleTokenValidator = googleTokenValidator;
            _userRepository = userRepository;
            _userProfileRepository = userProfileRepository;
            _userInvitationRepository = userInvitationRepository;
            _tokenService = tokenService;
            _configuration = configuration;
            _env = env;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AuthResponse>> GoogleAuth([FromBody] GoogleAuthRequest request)
        {
            try
            {
                // Validate the Google ID token
                var (googleSub, email, displayName) = await _googleTokenValidator.ValidateTokenAsync(request.IdToken);

                // Check if user already exists
                var existingUser = await _userRepository.FindByGoogleSubAsync(googleSub);
                
                // If user doesn't exist, check if they're invited
                if (existingUser == null)
                {
                    // Check if email is invited
                    var invitation = await _userInvitationRepository.FindByEmailAsync(email);
                    if (invitation == null)
                    {
                        _logger.LogWarning("Access denied for uninvited user: {Email}", email);
                        return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access is by invitation only. Please contact the administrator for access." });
                    }

                    // Also check if user was previously registered but then deactivated
                    // (their account was deleted but they're trying to sign in again)
                    var userByEmail = await _userRepository.FindByEmailAsync(email);
                    if (userByEmail == null && invitation.IsUsed)
                    {
                        _logger.LogWarning("Access denied for deactivated user: {Email}", email);
                        return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your access has been deactivated. Please contact the administrator." });
                    }

                    // Create new user
                    _logger.LogInformation("Creating new user for invited email {Email}", email);
                    existingUser = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        GoogleSub = googleSub,
                        Email = email,
                        DisplayName = displayName
                    };
                    existingUser = await _userRepository.CreateAsync(existingUser);

                    // Create a default user profile
                    var profile = new UserProfile
                    {
                        UserId = existingUser.Id,
                        SelectedKollectionId = null
                    };
                    await _userProfileRepository.CreateAsync(profile);

                    // Mark invitation as used
                    invitation.IsUsed = true;
                    invitation.UsedAt = DateTime.UtcNow;
                    await _userInvitationRepository.UpdateAsync(invitation);
                }
                else
                {
                    // Update user info if changed
                    if (existingUser.Email != email || existingUser.DisplayName != displayName)
                    {
                        existingUser.Email = email;
                        existingUser.DisplayName = displayName;
                        await _userRepository.UpdateAsync(existingUser);
                    }
                }

                // Generate JWT token
                var token = _tokenService.GenerateToken(existingUser);

                // Get user profile
                var userProfile = await _userProfileRepository.GetByUserIdAsync(existingUser.Id);

                var profileDto = new UserProfileDto
                {
                    UserId = existingUser.Id,
                    Email = existingUser.Email,
                    DisplayName = existingUser.DisplayName,
                    SelectedKollectionId = userProfile?.SelectedKollectionId,
                    IsAdmin = existingUser.IsAdmin
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
                var existingUser = await _userRepository.FindByGoogleSubAsync(googleSub);

                if (existingUser == null)
                {
                    var invitation = await _userInvitationRepository.FindByEmailAsync(email);
                    if (invitation == null)
                    {
                        _logger.LogWarning("Access denied for uninvited user: {Email}", email);
                        return Redirect($"{frontendOrigin}/?error=not_invited");
                    }

                    var userByEmail = await _userRepository.FindByEmailAsync(email);
                    if (userByEmail == null && invitation.IsUsed)
                    {
                        _logger.LogWarning("Access denied for deactivated user: {Email}", email);
                        return Redirect($"{frontendOrigin}/?error=access_deactivated");
                    }

                    _logger.LogInformation("Creating new user for invited email {Email}", email);
                    existingUser = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        GoogleSub = googleSub,
                        Email = email,
                        DisplayName = displayName
                    };
                    existingUser = await _userRepository.CreateAsync(existingUser);

                    var profile = new UserProfile
                    {
                        UserId = existingUser.Id,
                        SelectedKollectionId = null
                    };
                    await _userProfileRepository.CreateAsync(profile);

                    invitation.IsUsed = true;
                    invitation.UsedAt = DateTime.UtcNow;
                    await _userInvitationRepository.UpdateAsync(invitation);
                }
                else
                {
                    if (existingUser.Email != email || existingUser.DisplayName != displayName)
                    {
                        existingUser.Email = email;
                        existingUser.DisplayName = displayName;
                        await _userRepository.UpdateAsync(existingUser);
                    }
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
        /// Initiates Facebook OAuth authorization code flow.
        /// Redirects the browser to Facebook's consent screen.
        /// </summary>
        [HttpGet("facebook/login")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult FacebookLogin()
        {
            var appId = _configuration["Facebook:AppId"];
            var redirectUri = _configuration["Facebook:RedirectUri"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(redirectUri))
            {
                _logger.LogError("Facebook:AppId or Facebook:RedirectUri is not configured");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Facebook OAuth is not configured on the server." });
            }

            var authUrl = "https://www.facebook.com/v19.0/dialog/oauth" +
                $"?client_id={Uri.EscapeDataString(appId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                "&response_type=code" +
                $"&scope={Uri.EscapeDataString("email,public_profile")}";

            _logger.LogInformation("Redirecting to Facebook OAuth consent screen");
            return Redirect(authUrl);
        }

        /// <summary>
        /// Handles the OAuth callback from Facebook.
        /// Exchanges the authorization code for an access token, retrieves the user profile,
        /// creates/updates the user, generates a JWT and redirects to the frontend callback page.
        /// </summary>
        [HttpGet("facebook/callback")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public async Task<IActionResult> FacebookCallback([FromQuery] string? code, [FromQuery] string? error)
        {
            var frontendOrigin = GetFrontendOrigin();

            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("Facebook OAuth callback error: {Error}", error ?? "no code");
                return Redirect($"{frontendOrigin}/?error=facebook_auth_failed");
            }

            try
            {
                // Exchange authorization code for access token and retrieve user profile
                var (facebookSub, email, displayName) = await ExchangeCodeForFacebookProfileAsync(code);

                // Check / create user (same invitation logic as Google)
                var existingUser = await _userRepository.FindByFacebookSubAsync(facebookSub);

                if (existingUser == null)
                {
                    // Also check whether this email already has a Google account — link it if so
                    existingUser = await _userRepository.FindByEmailAsync(email);
                    if (existingUser != null)
                    {
                        // Link Facebook identity to the existing account
                        existingUser.FacebookSub = facebookSub;
                        await _userRepository.UpdateAsync(existingUser);
                    }
                    else
                    {
                        var invitation = await _userInvitationRepository.FindByEmailAsync(email);
                        if (invitation == null)
                        {
                            _logger.LogWarning("Access denied for uninvited Facebook user: {Email}", email);
                            return Redirect($"{frontendOrigin}/?error=not_invited");
                        }

                        var userByEmail = await _userRepository.FindByEmailAsync(email);
                        if (userByEmail == null && invitation.IsUsed)
                        {
                            _logger.LogWarning("Access denied for deactivated Facebook user: {Email}", email);
                            return Redirect($"{frontendOrigin}/?error=access_deactivated");
                        }

                        _logger.LogInformation("Creating new user for invited Facebook user {Email}", email);
                        existingUser = new ApplicationUser
                        {
                            Id = Guid.NewGuid(),
                            FacebookSub = facebookSub,
                            Email = email,
                            DisplayName = displayName
                        };
                        existingUser = await _userRepository.CreateAsync(existingUser);

                        var profile = new UserProfile
                        {
                            UserId = existingUser.Id,
                            SelectedKollectionId = null
                        };
                        await _userProfileRepository.CreateAsync(profile);

                        invitation.IsUsed = true;
                        invitation.UsedAt = DateTime.UtcNow;
                        await _userInvitationRepository.UpdateAsync(invitation);
                    }
                }
                else
                {
                    // Update user info if changed
                    if (existingUser.Email != email || existingUser.DisplayName != displayName)
                    {
                        existingUser.Email = email;
                        existingUser.DisplayName = displayName;
                        await _userRepository.UpdateAsync(existingUser);
                    }
                }

                var jwt = _tokenService.GenerateToken(existingUser);

                _logger.LogInformation("Facebook OAuth callback succeeded for {Email}", email);
                return Redirect($"{frontendOrigin}/auth/callback?token={Uri.EscapeDataString(jwt)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Facebook OAuth callback");
                return Redirect($"{frontendOrigin}/?error=auth_failed");
            }
        }

        /// <summary>
        /// Exchanges a Facebook authorization code for an access token and retrieves the user's profile.
        /// </summary>
        /// <param name="code">The authorization code returned by Facebook</param>
        /// <returns>A tuple containing the Facebook user ID, email, and display name</returns>
        private async Task<(string FacebookSub, string Email, string? DisplayName)> ExchangeCodeForFacebookProfileAsync(string code)
        {
            var appId = _configuration["Facebook:AppId"];
            var appSecret = _configuration["Facebook:AppSecret"];
            var redirectUri = _configuration["Facebook:RedirectUri"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(redirectUri))
            {
                throw new InvalidOperationException("Facebook OAuth configuration is incomplete (AppId, AppSecret or RedirectUri missing).");
            }

            using var client = _httpClientFactory.CreateClient();

            // Exchange code for access token
            var tokenUrl = "https://graph.facebook.com/v19.0/oauth/access_token" +
                $"?client_id={Uri.EscapeDataString(appId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&client_secret={Uri.EscapeDataString(appSecret)}" +
                $"&code={Uri.EscapeDataString(code)}";

            var tokenResponse = await client.GetAsync(tokenUrl);
            var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Facebook token exchange failed ({Status}): {Body}", tokenResponse.StatusCode, tokenBody);
                throw new UnauthorizedAccessException("Failed to exchange Facebook authorization code for access token.");
            }

            var tokenData = JsonSerializer.Deserialize<FacebookTokenResponse>(tokenBody)
                ?? throw new InvalidOperationException("Failed to deserialize Facebook token response.");

            if (string.IsNullOrEmpty(tokenData.AccessToken))
            {
                throw new InvalidOperationException("Facebook token response did not contain an access_token.");
            }

            // Retrieve user profile from the Graph API
            var profileUrl = "https://graph.facebook.com/v19.0/me" +
                $"?fields=id,email,name" +
                $"&access_token={Uri.EscapeDataString(tokenData.AccessToken)}";

            var profileResponse = await client.GetAsync(profileUrl);
            var profileBody = await profileResponse.Content.ReadAsStringAsync();

            if (!profileResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Facebook profile request failed ({Status}): {Body}", profileResponse.StatusCode, profileBody);
                throw new UnauthorizedAccessException("Failed to retrieve Facebook user profile.");
            }

            var profileData = JsonSerializer.Deserialize<FacebookProfileResponse>(profileBody)
                ?? throw new InvalidOperationException("Failed to deserialize Facebook profile response.");

            if (string.IsNullOrEmpty(profileData.Id))
            {
                throw new InvalidOperationException("Facebook profile response did not contain a user ID.");
            }

            if (string.IsNullOrEmpty(profileData.Email))
            {
                throw new UnauthorizedAccessException("Facebook account does not have a verified email address. Please ensure your Facebook account has a verified email.");
            }

            return (profileData.Id, profileData.Email, profileData.Name);
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

        /// <summary>
        /// Minimal DTO for deserializing Facebook's token endpoint response.
        /// </summary>
        private sealed class FacebookTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; init; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; init; }
        }

        /// <summary>
        /// Minimal DTO for deserializing Facebook Graph API user profile response.
        /// </summary>
        private sealed class FacebookProfileResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; init; }

            [JsonPropertyName("email")]
            public string? Email { get; init; }

            [JsonPropertyName("name")]
            public string? Name { get; init; }
        }
    }
}
