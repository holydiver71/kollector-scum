using System.Security.Claims;
using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Logging;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Implementation of IUserContext that extracts user information from HTTP context claims
    /// </summary>
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserContext> _logger;

        public UserContext(IHttpContextAccessor httpContextAccessor, ILogger<UserContext> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <inheritdoc />
        public Guid? GetUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("UserContext: No NameIdentifier claim found.");
                return null;
            }

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogInformation("UserContext: Resolved UserId {UserId}", userId);
                return userId;
            }
            
            _logger.LogError("UserContext: Failed to parse UserId claim: {UserIdClaim}", userIdClaim);
            return null;
        }

        /// <inheritdoc />
        public bool IsAdmin()
        {
            var isAdminClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("IsAdmin")?.Value;
            return bool.TryParse(isAdminClaim, out var isAdmin) && isAdmin;
        }

        /// <inheritdoc />
        public Guid? GetActingUserId()
        {
            // TODO: Add audit logging for admin impersonation
            // TODO: Consider rate limiting for impersonation attempts
            // TODO: Add security validation beyond just IsAdmin check
            
            // Check if admin is impersonating another user via header
            var actAsHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Admin-Act-As"].FirstOrDefault();
            
            if (!string.IsNullOrEmpty(actAsHeader) && IsAdmin())
            {
                if (Guid.TryParse(actAsHeader, out var actAsUserId))
                {
                    // TODO: Log admin impersonation: admin ID, target user ID, timestamp
                    return actAsUserId;
                }
            }

            // Check if admin is impersonating via query parameter
            var actAsQuery = _httpContextAccessor.HttpContext?.Request.Query["userId"].FirstOrDefault();
            
            if (!string.IsNullOrEmpty(actAsQuery) && IsAdmin())
            {
                if (Guid.TryParse(actAsQuery, out var actAsUserId))
                {
                    // TODO: Log admin impersonation: admin ID, target user ID, timestamp
                    return actAsUserId;
                }
            }

            // Return the current user's ID
            return GetUserId();
        }
    }
}
