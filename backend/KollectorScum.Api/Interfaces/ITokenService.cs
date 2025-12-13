using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for JWT token operations
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT token for a user
        /// </summary>
        /// <param name="user">The user to generate a token for</param>
        /// <returns>The generated JWT token</returns>
        string GenerateToken(ApplicationUser user);
    }
}
