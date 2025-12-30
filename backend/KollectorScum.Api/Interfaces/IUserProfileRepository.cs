using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Repository interface for UserProfile operations
    /// </summary>
    public interface IUserProfileRepository
    {
        /// <summary>
        /// Gets a user's profile by their user ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>The user profile if found, null otherwise</returns>
        Task<UserProfile?> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Creates a new user profile
        /// </summary>
        /// <param name="profile">The profile to create</param>
        /// <returns>The created profile</returns>
        Task<UserProfile> CreateAsync(UserProfile profile);

        /// <summary>
        /// Updates an existing user profile
        /// </summary>
        /// <param name="profile">The profile to update</param>
        /// <returns>The updated profile</returns>
        Task<UserProfile> UpdateAsync(UserProfile profile);

        /// <summary>
        /// Validates that a kollection exists
        /// </summary>
        /// <param name="kollectionId">The kollection ID to validate</param>
        /// <returns>True if the kollection exists, false otherwise</returns>
        Task<bool> KollectionExistsAsync(int kollectionId);

        /// <summary>
        /// Gets the count of music releases for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>The count of music releases</returns>
        Task<int> GetUserMusicReleaseCountAsync(Guid userId);

        /// <summary>
        /// Deletes all music releases for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>The number of releases deleted</returns>
        Task<int> DeleteAllUserMusicReleasesAsync(Guid userId);
    }
}
