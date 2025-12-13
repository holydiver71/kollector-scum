using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a user-defined list of music releases
    /// </summary>
    public class List
    {
        /// <summary>
        /// Gets or sets the unique identifier for the list
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID who owns this list
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Navigation property for the user who owns this list
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the list
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when this list was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets when this list was last modified
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property for releases in this list
        /// </summary>
        public virtual ICollection<ListRelease> ListReleases { get; set; } = new List<ListRelease>();
    }

    /// <summary>
    /// Join table for many-to-many relationship between Lists and MusicReleases
    /// </summary>
    public class ListRelease
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the list ID
        /// </summary>
        public int ListId { get; set; }

        /// <summary>
        /// Navigation property for the list
        /// </summary>
        public virtual List List { get; set; } = null!;

        /// <summary>
        /// Gets or sets the release ID
        /// </summary>
        public int ReleaseId { get; set; }

        /// <summary>
        /// Navigation property for the release
        /// </summary>
        public virtual MusicRelease Release { get; set; } = null!;

        /// <summary>
        /// Gets or sets when the release was added to the list
        /// </summary>
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
