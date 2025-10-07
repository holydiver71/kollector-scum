using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents an artist entity for music releases
    /// </summary>
    public class Artist
    {
        /// <summary>
        /// Gets or sets the unique identifier for the artist
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the artist
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for music releases by this artist
        /// </summary>
        public virtual ICollection<MusicRelease> MusicReleases { get; set; } = new List<MusicRelease>();
    }
}
