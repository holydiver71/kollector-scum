namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// Data transfer object returned when an admin initiates impersonation of a user
    /// </summary>
    public class ImpersonationDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the impersonated user
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the email address of the impersonated user
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the impersonated user, if set
        /// </summary>
        public string? DisplayName { get; set; }
    }
}
