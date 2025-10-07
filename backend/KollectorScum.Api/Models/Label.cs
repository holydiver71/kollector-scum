using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a record label entity for music releases
    /// </summary>
    public class Label
    {
        /// <summary>
        /// Gets or sets the unique identifier for the label
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the label
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for music releases from this label
        /// </summary>
        public virtual ICollection<MusicRelease> MusicReleases { get; set; } = new List<MusicRelease>();
    }
}
