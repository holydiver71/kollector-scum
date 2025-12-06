using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a user-defined list/collection of music releases
    /// </summary>
    public class Kollection
    {
        /// <summary>
        /// Gets or sets the unique identifier for the kollection
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the kollection
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when this kollection was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets when this kollection was last modified
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property for kollection items
        /// </summary>
        public virtual ICollection<KollectionItem> Items { get; set; } = new List<KollectionItem>();
    }
}
