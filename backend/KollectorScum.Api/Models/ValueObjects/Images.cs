using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models.ValueObjects
{
    /// <summary>
    /// Value object representing image information for a music release
    /// </summary>
    public class Images
    {
        /// <summary>
        /// Gets or sets the front cover image filename
        /// </summary>
        [StringLength(255)]
        public string? CoverFront { get; set; }

        /// <summary>
        /// Gets or sets the back cover image filename
        /// </summary>
        [StringLength(255)]
        public string? CoverBack { get; set; }

        /// <summary>
        /// Gets or sets the thumbnail image filename
        /// </summary>
        [StringLength(255)]
        public string? Thumbnail { get; set; }
    }
}
