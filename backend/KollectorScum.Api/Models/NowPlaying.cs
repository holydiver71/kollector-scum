using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a now playing record tracking when a release was played
    /// </summary>
    public class NowPlaying
    {
        /// <summary>
        /// Gets or sets the unique identifier for the now playing record
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the music release ID that was played
        /// </summary>
        [Required]
        public int MusicReleaseId { get; set; }

        /// <summary>
        /// Navigation property for the music release
        /// </summary>
        [ForeignKey("MusicReleaseId")]
        public virtual MusicRelease? MusicRelease { get; set; }

        /// <summary>
        /// Gets or sets when this release was played
        /// </summary>
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    }
}
