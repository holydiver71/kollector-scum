using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Repository interface for MagicLinkToken operations
    /// </summary>
    public interface IMagicLinkTokenRepository
    {
        /// <summary>
        /// Creates a new magic link token
        /// </summary>
        /// <param name="token">The token to create</param>
        /// <returns>The created token</returns>
        Task<MagicLinkToken> CreateAsync(MagicLinkToken token);

        /// <summary>
        /// Finds a token by its token value
        /// </summary>
        /// <param name="token">The token value</param>
        /// <returns>The token record if found, null otherwise</returns>
        Task<MagicLinkToken?> FindByTokenAsync(string token);

        /// <summary>
        /// Updates an existing token
        /// </summary>
        /// <param name="token">The token to update</param>
        /// <returns>The updated token</returns>
        Task<MagicLinkToken> UpdateAsync(MagicLinkToken token);

        /// <summary>
        /// Deletes all expired tokens to keep the table clean
        /// </summary>
        Task DeleteExpiredTokensAsync();
    }
}
