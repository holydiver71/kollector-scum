using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service that encapsulates user find-or-create logic for authentication flows.
    /// </summary>
    public interface IUserAuthenticationService
    {
        /// <summary>
        /// Finds an existing user by Google subject identifier, or creates a new one
        /// if the email has a valid invitation. Updates email/display name if they changed.
        /// </summary>
        /// <param name="googleSub">Google subject identifier.</param>
        /// <param name="email">User email address.</param>
        /// <param name="displayName">Optional display name from Google.</param>
        /// <returns>The existing or newly created user.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the email has no invitation or the account was deactivated.
        /// </exception>
        Task<ApplicationUser> FindOrCreateUserFromGoogleAsync(string googleSub, string email, string? displayName);

        /// <summary>
        /// Finds an existing user by email, or creates a new one if the email has a valid invitation.
        /// </summary>
        /// <param name="email">User email address.</param>
        /// <returns>The existing or newly created user.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the email has no valid invitation.
        /// </exception>
        Task<ApplicationUser> FindOrCreateUserFromEmailAsync(string email);
    }
}
