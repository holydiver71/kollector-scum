using Google.Apis.Auth;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service implementation for validating Google ID tokens
    /// </summary>
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleTokenValidator> _logger;

        public GoogleTokenValidator(IConfiguration configuration, ILogger<GoogleTokenValidator> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<(string GoogleSub, string Email, string? DisplayName)> ValidateTokenAsync(string idToken)
        {
            var clientId = _configuration["Google:ClientId"];
            if (string.IsNullOrEmpty(clientId))
            {
                throw new InvalidOperationException("Google ClientId is not configured");
            }

            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                _logger.LogInformation("Successfully validated Google token for user {Email}", payload.Email);

                return (payload.Subject, payload.Email, payload.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate Google token");
                throw new UnauthorizedAccessException("Invalid Google token", ex);
            }
        }
    }
}
