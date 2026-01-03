namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// DTO for user invitation
    /// </summary>
    public class UserInvitationDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
    }

    /// <summary>
    /// Request to create a new invitation
    /// </summary>
    public class CreateInvitationRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for user with access information
    /// </summary>
    public class UserAccessDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAdmin { get; set; }
    }
}
