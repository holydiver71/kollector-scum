using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Handles user find-or-create logic for Google OAuth and magic-link authentication flows.
    /// </summary>
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IUserInvitationRepository _userInvitationRepository;
        private readonly ILogger<UserAuthenticationService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="UserAuthenticationService"/>.
        /// </summary>
        public UserAuthenticationService(
            IUserRepository userRepository,
            IUserProfileRepository userProfileRepository,
            IUserInvitationRepository userInvitationRepository,
            ILogger<UserAuthenticationService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
            _userInvitationRepository = userInvitationRepository ?? throw new ArgumentNullException(nameof(userInvitationRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<ApplicationUser> FindOrCreateUserFromGoogleAsync(
            string googleSub,
            string email,
            string? displayName)
        {
            var existingUser = await _userRepository.FindByGoogleSubAsync(googleSub);

            // If found by Google sub but deactivated, deny access
            if (existingUser != null && !existingUser.IsActive)
            {
                _logger.LogWarning("Access denied for deactivated user: {Email}", email);
                throw new UnauthorizedAccessException("Your access has been deactivated. Please contact the administrator.");
            }

            if (existingUser == null)
            {
                // Check if a deactivated account exists for this email (e.g. linked via email, not yet by googleSub)
                var userByEmail = await _userRepository.FindByEmailAsync(email);
                if (userByEmail != null && !userByEmail.IsActive)
                {
                    _logger.LogWarning("Access denied for deactivated user: {Email}", email);
                    throw new UnauthorizedAccessException("Your access has been deactivated. Please contact the administrator.");
                }

                var invitation = await _userInvitationRepository.FindByEmailAsync(email);
                if (invitation == null)
                {
                    _logger.LogWarning("Access denied for uninvited user: {Email}", email);
                    throw new UnauthorizedAccessException("Access is by invitation only. Please contact the administrator for access.");
                }

                _logger.LogInformation("Creating new user for invited email {Email}", email);
                var newUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    GoogleSub = googleSub,
                    Email = email,
                    DisplayName = displayName,
                    IsActive = true
                };
                newUser = await _userRepository.CreateAsync(newUser);

                await _userProfileRepository.CreateAsync(new UserProfile
                {
                    UserId = newUser.Id,
                    SelectedKollectionId = null
                });

                invitation.IsUsed = true;
                invitation.UsedAt = DateTime.UtcNow;
                await _userInvitationRepository.UpdateAsync(invitation);

                return newUser;
            }

            // Update email/display name if changed
            if (existingUser.Email != email || existingUser.DisplayName != displayName)
            {
                existingUser.Email = email;
                existingUser.DisplayName = displayName;
                await _userRepository.UpdateAsync(existingUser);
            }

            return existingUser;
        }

        /// <inheritdoc />
        public async Task<ApplicationUser> FindOrCreateUserFromEmailAsync(string email)
        {
            var existingUser = await _userRepository.FindByEmailAsync(email);
            if (existingUser != null)
            {
                if (!existingUser.IsActive)
                {
                    _logger.LogWarning("Magic link verification denied for deactivated user: {Email}", email);
                    throw new UnauthorizedAccessException("Your access has been deactivated. Please contact the administrator.");
                }
                return existingUser;
            }

            var invitation = await _userInvitationRepository.FindByEmailAsync(email);
            if (invitation == null)
            {
                _logger.LogWarning("Magic link verification denied: no invitation for {Email}", email);
                throw new UnauthorizedAccessException("Access is by invitation only. Please contact the administrator.");
            }

            _logger.LogInformation("Creating new user via magic link for {Email}", email);
            var newUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                GoogleSub = null,
                Email = email,
                DisplayName = email,
                IsActive = true
            };
            newUser = await _userRepository.CreateAsync(newUser);

            await _userProfileRepository.CreateAsync(new UserProfile
            {
                UserId = newUser.Id,
                SelectedKollectionId = null
            });

            invitation.IsUsed = true;
            invitation.UsedAt = DateTime.UtcNow;
            await _userInvitationRepository.UpdateAsync(invitation);

            return newUser;
        }
    }
}
