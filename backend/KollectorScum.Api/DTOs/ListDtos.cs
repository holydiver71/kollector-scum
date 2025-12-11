using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// DTO for creating a new list
    /// </summary>
    public class CreateListDto
    {
        /// <summary>
        /// Gets or sets the name of the list
        /// </summary>
        [Required(ErrorMessage = "List name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "List name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating an existing list
    /// </summary>
    public class UpdateListDto
    {
        /// <summary>
        /// Gets or sets the name of the list
        /// </summary>
        [Required(ErrorMessage = "List name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "List name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for list summary (without releases)
    /// </summary>
    public class ListSummaryDto
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the list
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of releases in the list
        /// </summary>
        public int ReleaseCount { get; set; }

        /// <summary>
        /// Gets or sets when the list was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the list was last modified
        /// </summary>
        public DateTime LastModified { get; set; }
    }

    /// <summary>
    /// DTO for list with releases
    /// </summary>
    public class ListDto
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the list
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the list was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the list was last modified
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets or sets the release IDs in this list
        /// </summary>
        public List<int> ReleaseIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// DTO for adding a release to a list
    /// </summary>
    public class AddReleaseToListDto
    {
        /// <summary>
        /// Gets or sets the release ID
        /// </summary>
        [Required(ErrorMessage = "Release ID is required")]
        public int ReleaseId { get; set; }
    }

    /// <summary>
    /// DTO for adding a release to a list (with optional list creation)
    /// </summary>
    public class AddToListRequestDto
    {
        /// <summary>
        /// Gets or sets the release ID to add
        /// </summary>
        [Required(ErrorMessage = "Release ID is required")]
        public int ReleaseId { get; set; }

        /// <summary>
        /// Gets or sets the list ID (if adding to existing list)
        /// </summary>
        public int? ListId { get; set; }

        /// <summary>
        /// Gets or sets the new list name (if creating a new list)
        /// </summary>
        [StringLength(200, MinimumLength = 1, ErrorMessage = "List name must be between 1 and 200 characters")]
        public string? NewListName { get; set; }
    }
}
