using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for admin user impersonation.
    /// </summary>
    public class UserImpersonationService : IUserImpersonationService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserImpersonationService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="UserImpersonationService"/>.
        /// </summary>
        public UserImpersonationService(
            IUserRepository userRepository,
            ILogger<UserImpersonationService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<ImpersonationDto?> ImpersonateUserAsync(Guid adminId, Guid targetUserId)
        {
            if (adminId == targetUserId)
            {
                throw new InvalidOperationException("Cannot impersonate yourself");
            }

            var targetUser = await _userRepository.FindByIdAsync(targetUserId);
            if (targetUser == null)
            {
                return null;
            }

            if (targetUser.IsAdmin)
            {
                throw new InvalidOperationException("Cannot impersonate an admin user");
            }

            _logger.LogWarning(
                "Admin {AdminId} initiated impersonation of user {TargetId} ({TargetEmail})",
                adminId, targetUserId, targetUser.Email);

            return new ImpersonationDto
            {
                UserId = targetUser.Id,
                Email = targetUser.Email,
                DisplayName = targetUser.DisplayName
            };
        }
    }
}
