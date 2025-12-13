using System.Security.Claims;
using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Http;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service implementation for accessing current authenticated user information
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public Guid? GetUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }

        /// <inheritdoc />
        public Guid GetUserIdOrThrow()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }
            return userId.Value;
        }

        /// <inheritdoc />
        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }
    }
}
