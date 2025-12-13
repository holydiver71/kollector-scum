using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a format entity for music releases (vinyl, CD, digital, etc.)
    /// </summary>
    public class Format : IUserOwnedEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the format
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who owns this format
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the format
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for music releases in this format
        /// </summary>
        public virtual ICollection<MusicRelease> MusicReleases { get; set; } = new List<MusicRelease>();
    }
}
