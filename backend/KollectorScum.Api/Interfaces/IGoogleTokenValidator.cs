namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for validating Google ID tokens
    /// </summary>
    public interface IGoogleTokenValidator
    {
        /// <summary>
        /// Validates a Google ID token and returns the payload
        /// </summary>
        /// <param name="idToken">The Google ID token to validate</param>
        /// <returns>A tuple containing the Google sub, email, and display name</returns>
        Task<(string GoogleSub, string Email, string? DisplayName)> ValidateTokenAsync(string idToken);
    }
}
