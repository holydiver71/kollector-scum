using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Repository interface for user invitation operations
    /// </summary>
    public interface IUserInvitationRepository
    {
        /// <summary>
        /// Finds an invitation by email address
        /// </summary>
        Task<UserInvitation?> FindByEmailAsync(string email);

        /// <summary>
        /// Creates a new invitation
        /// </summary>
        Task<UserInvitation> CreateAsync(UserInvitation invitation);

        /// <summary>
        /// Deletes an invitation by ID
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Gets all invitations
        /// </summary>
        Task<List<UserInvitation>> GetAllAsync();

        /// <summary>
        /// Updates an invitation
        /// </summary>
        Task<UserInvitation> UpdateAsync(UserInvitation invitation);

        /// <summary>
        /// Checks if an email is invited
        /// </summary>
        Task<bool> IsEmailInvitedAsync(string email);
    }
}
