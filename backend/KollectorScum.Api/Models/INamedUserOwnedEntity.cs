namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Interface for named lookup entities that are owned by a user.
    /// Extends <see cref="IUserOwnedEntity"/> to include an integer primary key and a name.
    /// </summary>
    public interface INamedUserOwnedEntity : IUserOwnedEntity
    {
        /// <summary>
        /// Gets or sets the unique integer identifier for this entity.
        /// </summary>
        int Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of this entity.
        /// </summary>
        string Name { get; set; }
    }
}
