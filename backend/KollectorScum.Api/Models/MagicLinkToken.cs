using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a magic link token used for passwordless email authentication
    /// </summary>
    public class MagicLinkToken
    {
        /// <summary>
        /// Gets or sets the unique identifier for the token record
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the email address this token was issued for
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the secure random token value
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the token was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the token expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets whether the token has already been used
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Gets or sets when the token was used
        /// </summary>
        public DateTime? UsedAt { get; set; }
    }
}
