using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a collection of releases filtered by genres
    /// </summary>
    public class Kollection
    {
        /// <summary>
        /// Gets or sets the unique identifier for the kollection
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID who owns this kollection
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Navigation property for the user who owns this kollection
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the kollection
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for genres in this kollection
        /// </summary>
        public virtual ICollection<KollectionGenre> KollectionGenres { get; set; } = new List<KollectionGenre>();
    }

    /// <summary>
    /// Join table for many-to-many relationship between Kollections and Genres
    /// </summary>
    public class KollectionGenre
    {
        /// <summary>
        /// Gets or sets the kollection ID
        /// </summary>
        public int KollectionId { get; set; }

        /// <summary>
        /// Navigation property for the kollection
        /// </summary>
        public virtual Kollection Kollection { get; set; } = null!;

        /// <summary>
        /// Gets or sets the genre ID
        /// </summary>
        public int GenreId { get; set; }

        /// <summary>
        /// Navigation property for the genre
        /// </summary>
        public virtual Genre Genre { get; set; } = null!;
    }
}
