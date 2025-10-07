using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models.ValueObjects
{
    /// <summary>
    /// Value object representing a link associated with a music release
    /// </summary>
    public class Link
    {
        /// <summary>
        /// Gets or sets the description of the link
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL type
        /// </summary>
        [StringLength(50)]
        public string? UrlType { get; set; }
    }
}
