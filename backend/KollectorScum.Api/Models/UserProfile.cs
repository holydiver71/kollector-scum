using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a user's profile preferences
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// Gets or sets the unique identifier for the profile
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID (foreign key to ApplicationUser)
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the selected kollection ID (nullable)
        /// </summary>
        public int? SelectedKollectionId { get; set; }

        /// <summary>
        /// Navigation property for the user
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// Navigation property for the selected kollection
        /// </summary>
        [ForeignKey(nameof(SelectedKollectionId))]
        public Kollection? SelectedKollection { get; set; }
    }
}
