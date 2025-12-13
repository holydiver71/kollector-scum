using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents an authenticated user in the system
    /// </summary>
    public class ApplicationUser
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the Google subject identifier (unique per user)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string GoogleSub { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's email address
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's display name
        /// </summary>
        [MaxLength(255)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets when the user was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the user was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Navigation property for the user's profile
        /// </summary>
        public UserProfile? UserProfile { get; set; }
    }
}
