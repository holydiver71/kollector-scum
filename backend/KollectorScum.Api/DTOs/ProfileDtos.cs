namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// DTO for user profile information
    /// </summary>
    public class UserProfileDto
    {
        /// <summary>
        /// Gets or sets the user ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the selected kollection ID
        /// </summary>
        public int? SelectedKollectionId { get; set; }
    }

    /// <summary>
    /// Request DTO for updating user profile
    /// </summary>
    public class UpdateProfileRequest
    {
        /// <summary>
        /// Gets or sets the selected kollection ID
        /// </summary>
        public int? SelectedKollectionId { get; set; }
    }

    /// <summary>
    /// Response DTO for collection deletion
    /// </summary>
    public class DeleteCollectionResponse
    {
        /// <summary>
        /// Gets or sets the number of albums deleted
        /// </summary>
        public int AlbumsDeleted { get; set; }

        /// <summary>
        /// Gets or sets whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets an optional message
        /// </summary>
        public string? Message { get; set; }
    }
}
