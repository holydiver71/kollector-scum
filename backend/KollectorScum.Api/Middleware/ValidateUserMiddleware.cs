using System.Security.Claims;
using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace KollectorScum.Api.Middleware
{
    /// <summary>
    /// Middleware that validates authenticated users still exist in the database
    /// Prevents revoked users from using cached JWT tokens
    /// </summary>
    public class ValidateUserMiddleware
    {
        private const string UserExistsCacheKeyPrefix = "validate-user-exists:";
        private static readonly TimeSpan UserExistsCacheTtl = TimeSpan.FromMinutes(5);

        private readonly RequestDelegate _next;
        private readonly ILogger<ValidateUserMiddleware> _logger;
        private readonly IMemoryCache _memoryCache;

        public ValidateUserMiddleware(
            RequestDelegate next,
            ILogger<ValidateUserMiddleware> logger,
            IMemoryCache memoryCache)
        {
            _next = next;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
        {
            // Only validate if the user is authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    var userExists = await GetOrCacheUserExistsAsync(userId, userRepository);

                    if (!userExists)
                    {
                        _logger.LogWarning("Authenticated request rejected: User {UserId} no longer exists (deactivated)", userId);
                        
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new 
                        { 
                            message = "Your access has been deactivated. Please sign in again or contact the administrator." 
                        });
                        return;
                    }
                }
            }

            await _next(context);
        }

        private async Task<bool> GetOrCacheUserExistsAsync(Guid userId, IUserRepository userRepository)
        {
            var cacheKey = $"{UserExistsCacheKeyPrefix}{userId}";
            if (_memoryCache.TryGetValue(cacheKey, out bool cachedExists))
            {
                return cachedExists;
            }

            var user = await userRepository.FindByIdAsync(userId);
            var exists = user != null;
            _memoryCache.Set(cacheKey, exists, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = UserExistsCacheTtl
            });

            return exists;
        }
    }

    /// <summary>
    /// Extension method to register the ValidateUserMiddleware
    /// </summary>
    public static class ValidateUserMiddlewareExtensions
    {
        public static IApplicationBuilder UseValidateUser(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ValidateUserMiddleware>();
        }
    }
}
