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

            // Allow configuring additional accepted audiences (comma-separated) for local/dev tokens
            var allowedAudiencesRaw = _configuration["Google:AllowedAudiences"] ?? _configuration["Google__AllowedAudiences"];
            string[] audiences;
            if (!string.IsNullOrWhiteSpace(allowedAudiencesRaw))
            {
                audiences = allowedAudiencesRaw.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                // Ensure the configured clientId is always accepted
                if (!audiences.Contains(clientId))
                {
                    audiences = audiences.Append(clientId).ToArray();
                }
            }
            else
            {
                audiences = new[] { clientId };
            }

            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = audiences
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
