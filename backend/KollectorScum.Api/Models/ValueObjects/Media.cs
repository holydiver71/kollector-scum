using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models.ValueObjects
{
    /// <summary>
    /// Represents a media item (disc/tape/etc.) within a music release
    /// </summary>
    public class Media
    {
        /// <summary>
        /// Gets or sets the title of the media item
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the format ID for this media
        /// </summary>
        public int FormatId { get; set; }

        /// <summary>
        /// Gets or sets the media index/position
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the tracks on this media
        /// </summary>
        public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
    }
}
