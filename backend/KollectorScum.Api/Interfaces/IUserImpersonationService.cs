using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for admin user impersonation operations.
    /// </summary>
    public interface IUserImpersonationService
    {
        /// <summary>
        /// Initiates impersonation of the specified user by an admin.
        /// Returns impersonation details when successful, or null when the user was not found.
        /// Throws <see cref="InvalidOperationException"/> if impersonation is not allowed
        /// (e.g., cannot impersonate self or another admin).
        /// </summary>
        /// <param name="adminId">The ID of the admin requesting impersonation.</param>
        /// <param name="targetUserId">The ID of the user to impersonate.</param>
        /// <returns>Impersonation details, or null if user not found.</returns>
        Task<ImpersonationDto?> ImpersonateUserAsync(Guid adminId, Guid targetUserId);
    }
}
