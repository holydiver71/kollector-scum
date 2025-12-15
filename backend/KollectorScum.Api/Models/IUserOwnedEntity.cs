namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Interface for entities that are owned by a user
    /// </summary>
    public interface IUserOwnedEntity
    {
        /// <summary>
        /// Gets or sets the user identifier who owns this entity
        /// </summary>
        Guid UserId { get; set; }
    }
}
