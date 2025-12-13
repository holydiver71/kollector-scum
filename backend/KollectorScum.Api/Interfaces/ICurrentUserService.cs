namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for accessing current authenticated user information
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Gets the current authenticated user's ID
        /// </summary>
        /// <returns>User ID if authenticated, null otherwise</returns>
        Guid? GetUserId();

        /// <summary>
        /// Gets the current authenticated user's ID, throwing if not authenticated
        /// </summary>
        /// <returns>User ID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated</exception>
        Guid GetUserIdOrThrow();

        /// <summary>
        /// Checks if the current user is authenticated
        /// </summary>
        /// <returns>True if authenticated, false otherwise</returns>
        bool IsAuthenticated();
    }
}
