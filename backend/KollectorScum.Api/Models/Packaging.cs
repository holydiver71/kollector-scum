using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a packaging entity for music releases (jewel case, digipak, etc.)
    /// </summary>
    public class Packaging
    {
        /// <summary>
        /// Gets or sets the unique identifier for the packaging
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the packaging type
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for music releases with this packaging
        /// </summary>
        public virtual ICollection<MusicRelease> MusicReleases { get; set; } = new List<MusicRelease>();
    }
}
