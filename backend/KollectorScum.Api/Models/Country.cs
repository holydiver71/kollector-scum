using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a country entity for music release origins
    /// </summary>
    public class Country
    {
        /// <summary>
        /// Gets or sets the unique identifier for the country
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who owns this country
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the country
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for music releases from this country
        /// </summary>
        public virtual ICollection<MusicRelease> MusicReleases { get; set; } = new List<MusicRelease>();
    }
}
