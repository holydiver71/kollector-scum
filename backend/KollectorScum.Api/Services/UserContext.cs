using System.Security.Claims;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Implementation of IUserContext that extracts user information from HTTP context claims
    /// </summary>
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public Guid? GetUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
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
