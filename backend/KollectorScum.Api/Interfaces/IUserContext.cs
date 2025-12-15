namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Provides access to the current user's context from claims
    /// </summary>
    public interface IUserContext
    {
        /// <summary>
        /// Gets the current user's identifier from claims (sub claim)
        /// </summary>
        /// <returns>The user ID if authenticated, null otherwise</returns>
        Guid? GetUserId();

        /// <summary>
        /// Gets whether the current user is an admin
        /// </summary>
        /// <returns>True if the user is an admin, false otherwise</returns>
        bool IsAdmin();

        /// <summary>
        /// Gets the acting user ID for admin impersonation
        /// Returns the impersonated user ID if admin is acting as another user,
        /// otherwise returns the current user's ID
        /// </summary>
        /// <returns>The acting user ID</returns>
        Guid? GetActingUserId();
    }
}
