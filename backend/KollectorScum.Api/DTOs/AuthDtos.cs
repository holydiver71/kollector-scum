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

    /// <summary>
    /// Request DTO for initiating passwordless magic link authentication
    /// </summary>
    public class MagicLinkRequestDto
    {
        /// <summary>
        /// Gets or sets the email address to send the magic link to
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for verifying a magic link token
    /// </summary>
    public class MagicLinkVerifyDto
    {
        /// <summary>
        /// Gets or sets the magic link token to verify
        /// </summary>
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
