using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a store entity where music releases can be purchased
    /// </summary>
    public class Store : IUserOwnedEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the store
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who owns this store
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the store
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for music releases from this store
        /// </summary>
        public virtual ICollection<MusicRelease> MusicReleases { get; set; } = new List<MusicRelease>();
    }
}
