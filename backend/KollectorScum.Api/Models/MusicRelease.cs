using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KollectorScum.Api.Models.ValueObjects;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a music release entity
    /// </summary>
    public class MusicRelease
    {
        /// <summary>
        /// Gets or sets the unique identifier for the music release
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the title of the release
        /// </summary>
        [Required]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the release year
        /// </summary>
        public DateTime? ReleaseYear { get; set; }

        /// <summary>
        /// Gets or sets the original release year
        /// </summary>
        public DateTime? OrigReleaseYear { get; set; }

        /// <summary>
        /// Gets or sets the artist IDs (stored as JSON array)
        /// </summary>
        public string? Artists { get; set; }

        /// <summary>
        /// Gets or sets the genre IDs (stored as JSON array)
        /// </summary>
        public string? Genres { get; set; }

        /// <summary>
        /// Gets or sets whether this is a live recording
        /// </summary>
        public bool Live { get; set; }

        /// <summary>
        /// Gets or sets the label ID
        /// </summary>
        public int? LabelId { get; set; }

        /// <summary>
        /// Navigation property for the label
        /// </summary>
        [ForeignKey("LabelId")]
        public virtual Label? Label { get; set; }

        /// <summary>
        /// Gets or sets the country ID
        /// </summary>
        public int? CountryId { get; set; }

        /// <summary>
        /// Navigation property for the country
        /// </summary>
        [ForeignKey("CountryId")]
        public virtual Country? Country { get; set; }

        /// <summary>
        /// Gets or sets the label number/catalog number
        /// </summary>
        [StringLength(100)]
        public string? LabelNumber { get; set; }

        /// <summary>
        /// Gets or sets the total length in seconds
        /// </summary>
        public int? LengthInSeconds { get; set; }

        /// <summary>
        /// Gets or sets the format ID
        /// </summary>
        public int? FormatId { get; set; }

        /// <summary>
        /// Navigation property for the format
        /// </summary>
        [ForeignKey("FormatId")]
        public virtual Format? Format { get; set; }

        /// <summary>
        /// Gets or sets the purchase information (stored as JSON)
        /// </summary>
        public string? PurchaseInfo { get; set; }

        /// <summary>
        /// Gets or sets the packaging ID
        /// </summary>
        public int? PackagingId { get; set; }

        /// <summary>
        /// Navigation property for the packaging
        /// </summary>
        [ForeignKey("PackagingId")]
        public virtual Packaging? Packaging { get; set; }

        /// <summary>
        /// Gets or sets the UPC barcode
        /// </summary>
        [StringLength(50)]
        public string? Upc { get; set; }

        /// <summary>
        /// Gets or sets the image information (stored as JSON)
        /// </summary>
        public string? Images { get; set; }

        /// <summary>
        /// Gets or sets the links (stored as JSON)
        /// </summary>
        public string? Links { get; set; }

        /// <summary>
        /// Gets or sets when this release was added to the collection
        /// </summary>
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets when this release was last modified
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the media items (stored as JSON)
        /// </summary>
        public string? Media { get; set; }
    }
}
