using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents an email invitation for a user to access the application
    /// </summary>
    public class UserInvitation
    {
        /// <summary>
        /// Gets or sets the unique identifier for the invitation
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the email address of the invited user
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the invitation was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the ID of the admin who created the invitation
        /// </summary>
        [Required]
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Gets or sets whether the invitation has been used (user has signed in)
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Gets or sets when the invitation was used
        /// </summary>
        public DateTime? UsedAt { get; set; }
    }
}
