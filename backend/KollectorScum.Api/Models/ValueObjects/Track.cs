using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models.ValueObjects
{
    /// <summary>
    /// Represents a track within a media item
    /// </summary>
    public class Track
    {
        /// <summary>
        /// Gets or sets the title of the track
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the release year of the track
        /// </summary>
        public DateTime? ReleaseYear { get; set; }

        /// <summary>
        /// Gets or sets the artist IDs for this track (as JSON array)
        /// </summary>
        public string? Artists { get; set; }

        /// <summary>
        /// Gets or sets the genre IDs for this track (as JSON array)
        /// </summary>
        public string? Genres { get; set; }

        /// <summary>
        /// Gets or sets whether this is a live recording
        /// </summary>
        public bool Live { get; set; }

        /// <summary>
        /// Gets or sets the length in seconds
        /// </summary>
        public int? LengthSecs { get; set; }

        /// <summary>
        /// Gets or sets the track index/position
        /// </summary>
        public int Index { get; set; }
    }
}
