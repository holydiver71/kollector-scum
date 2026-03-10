using System.Security.Cryptography;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service implementation for magic link token management.
    /// Generates cryptographically secure 256-bit tokens (64-character hex strings),
    /// coordinates sending the magic link via <see cref="IEmailService"/>, validates tokens
    /// (checking expiry and single-use status), and marks tokens as used upon redemption.
    /// Token lifetime is configurable via <c>Email:MagicLinkExpiryMinutes</c> (default 15 minutes).
    /// </summary>
    public class MagicLinkService : IMagicLinkService
    {
        private readonly IMagicLinkTokenRepository _tokenRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MagicLinkService> _logger;

        /// <summary>
        /// Token length in bytes (produces a 64-character hex string)
        /// </summary>
        private const int TokenByteLength = 32;

        public MagicLinkService(
            IMagicLinkTokenRepository tokenRepository,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<MagicLinkService> logger)
        {
            _tokenRepository = tokenRepository;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<MagicLinkToken> CreateAndSendTokenAsync(string email, string frontendOrigin)
        {
            var expiryMinutes = GetExpiryMinutes();
            var tokenValue = GenerateSecureToken();

            var magicLinkToken = new MagicLinkToken
            {
                Email = email.ToLowerInvariant(),
                Token = tokenValue,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };

            var created = await _tokenRepository.CreateAsync(magicLinkToken);

            var magicLink = $"{frontendOrigin}/auth/magic-link?token={Uri.EscapeDataString(tokenValue)}";

            try
            {
                await _emailService.SendMagicLinkEmailAsync(email, magicLink);
                _logger.LogInformation("Magic link token created and emailed for {Email}, expires in {Minutes} minutes", email, expiryMinutes);
            }
            catch (Exception ex)
            {
                // Email delivery failed (e.g. SMTP not configured on staging).
                // The token is already persisted — log the full magic link so it can be
                // retrieved from server logs and used manually during testing.
                _logger.LogError(ex,
                    "Email delivery failed for {Email}. Token is valid for {Minutes} minutes. " +
                    "Manual sign-in link: {MagicLink}",
                    email, expiryMinutes, magicLink);
            }

            return created;
        }

        /// <inheritdoc />
        public async Task<string?> ValidateTokenAsync(string token)
        {
            var record = await _tokenRepository.FindByTokenAsync(token);

            if (record == null)
            {
                _logger.LogWarning("Magic link token not found");
                return null;
            }

            if (record.IsUsed)
            {
                _logger.LogWarning("Magic link token has already been used for {Email}", record.Email);
                return null;
            }

            if (record.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Magic link token has expired for {Email}", record.Email);
                return null;
            }

            return record.Email;
        }

        /// <inheritdoc />
        public async Task MarkTokenAsUsedAsync(string token)
        {
            var record = await _tokenRepository.FindByTokenAsync(token);
            if (record == null)
            {
                return;
            }

            record.IsUsed = true;
            record.UsedAt = DateTime.UtcNow;
            await _tokenRepository.UpdateAsync(record);
        }

        /// <summary>
        /// Generates a cryptographically secure random token
        /// </summary>
        /// <returns>A URL-safe hex-encoded token string</returns>
        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /// <summary>
        /// Gets the configured token expiry in minutes, defaulting to 15
        /// </summary>
        private int GetExpiryMinutes()
        {
            var raw = _configuration["Email:MagicLinkExpiryMinutes"];
            if (int.TryParse(raw, out var minutes) && minutes > 0)
            {
                return minutes;
            }

            return 15;
        }
    }
}
