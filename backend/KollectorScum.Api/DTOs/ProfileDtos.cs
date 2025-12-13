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
}
