using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// Request DTO for Google authentication
    /// </summary>
    public class GoogleAuthRequest
    {
        /// <summary>
        /// Gets or sets the Google ID token
        /// </summary>
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response DTO for authentication
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// Gets or sets the JWT token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user profile DTO
        /// </summary>
        public UserProfileDto Profile { get; set; } = new();
    }
}
