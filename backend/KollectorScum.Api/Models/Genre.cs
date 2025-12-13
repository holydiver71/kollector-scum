using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a genre entity for categorizing music releases
    /// </summary>
    public class Genre
    {
        /// <summary>
        /// Gets or sets the unique identifier for the genre
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who owns this genre
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the genre
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for music releases in this genre
        /// </summary>
        public virtual ICollection<MusicRelease> MusicReleases { get; set; } = new List<MusicRelease>();
    }
}
