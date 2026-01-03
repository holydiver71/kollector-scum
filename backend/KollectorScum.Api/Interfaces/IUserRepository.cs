using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Repository interface for ApplicationUser operations
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Finds a user by their Google subject identifier
        /// </summary>
        /// <param name="googleSub">The Google subject identifier</param>
        /// <returns>The user if found, null otherwise</returns>
        Task<ApplicationUser?> FindByGoogleSubAsync(string googleSub);

        /// <summary>
        /// Finds a user by their email address
        /// </summary>
        /// <param name="email">The email address</param>
        /// <returns>The user if found, null otherwise</returns>
        Task<ApplicationUser?> FindByEmailAsync(string email);

        /// <summary>
        /// Finds a user by their ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>The user if found, null otherwise</returns>
        Task<ApplicationUser?> FindByIdAsync(Guid userId);

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="user">The user to create</param>
        /// <returns>The created user</returns>
        Task<ApplicationUser> CreateAsync(ApplicationUser user);

        /// <summary>
        /// Updates an existing user
        /// </summary>
        /// <param name="user">The user to update</param>
        /// <returns>The updated user</returns>
        Task<ApplicationUser> UpdateAsync(ApplicationUser user);

        /// <summary>
        /// Gets all users
        /// </summary>
        /// <returns>List of all users</returns>
        Task<List<ApplicationUser>> GetAllAsync();

        /// <summary>
        /// Deletes a user by their ID
        /// </summary>
        /// <param name="userId">The user ID to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(Guid userId);
    }
}
