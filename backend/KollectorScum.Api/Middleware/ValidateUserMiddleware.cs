using System.Security.Claims;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Middleware
{
    /// <summary>
    /// Middleware that validates authenticated users still exist in the database
    /// Prevents revoked users from using cached JWT tokens
    /// </summary>
    public class ValidateUserMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ValidateUserMiddleware> _logger;

        public ValidateUserMiddleware(RequestDelegate next, ILogger<ValidateUserMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
        {
            // Only validate if the user is authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    // Check if user still exists in database
                    var user = await userRepository.FindByIdAsync(userId);
                    
                    if (user == null)
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
