using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for magic link token operations
    /// </summary>
    public interface IMagicLinkService
    {
        /// <summary>
        /// Creates a new magic link token for the given email and sends the link via email
        /// </summary>
        /// <param name="email">The email address to send the magic link to</param>
        /// <param name="frontendOrigin">The base URL of the frontend, used to construct the magic link</param>
        /// <returns>The created token record</returns>
        Task<MagicLinkToken> CreateAndSendTokenAsync(string email, string frontendOrigin);

        /// <summary>
        /// Validates a magic link token
        /// </summary>
        /// <param name="token">The token value to validate</param>
        /// <returns>The email address the token was issued for, or null if invalid/expired</returns>
        Task<string?> ValidateTokenAsync(string token);

        /// <summary>
        /// Marks a token as used so it cannot be used again
        /// </summary>
        /// <param name="token">The token value to mark as used</param>
        Task MarkTokenAsUsedAsync(string token);
    }
}
