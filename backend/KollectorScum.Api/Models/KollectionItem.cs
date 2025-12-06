using System.ComponentModel.DataAnnotations.Schema;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a music release in a kollection (join table)
    /// </summary>
    public class KollectionItem
    {
        /// <summary>
        /// Gets or sets the unique identifier for the kollection item
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the kollection ID
        /// </summary>
        public int KollectionId { get; set; }

        /// <summary>
        /// Navigation property for the kollection
        /// </summary>
        [ForeignKey("KollectionId")]
        public virtual Kollection Kollection { get; set; } = null!;

        /// <summary>
        /// Gets or sets the music release ID
        /// </summary>
        public int MusicReleaseId { get; set; }

        /// <summary>
        /// Navigation property for the music release
        /// </summary>
        [ForeignKey("MusicReleaseId")]
        public virtual MusicRelease MusicRelease { get; set; } = null!;

        /// <summary>
        /// Gets or sets when this item was added to the kollection
        /// </summary>
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
