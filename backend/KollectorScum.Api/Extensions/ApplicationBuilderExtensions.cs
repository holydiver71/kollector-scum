using KollectorScum.Api.Middleware;

namespace KollectorScum.Api.Extensions;

/// <summary>
/// Extension methods that configure the HTTP request pipeline in Program.cs.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Applies the full Kollector Scum middleware pipeline in the correct order:
    /// security headers → HTTPS enforcement → error handling → Swagger (dev) →
    /// CORS → rate limiting → compression → caching → static files →
    /// authentication → authorisation → user validation.
    /// </summary>
    public static WebApplication UseKollectorApiPipeline(this WebApplication app)
    {
        // Security headers must be first so every response carries them.
        app.UseSecurityHeaders();

        // HSTS / HTTPS redirection only applies outside Development where TLS
        // is terminated at the server or a reverse proxy.
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseMiddleware<ErrorHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kollector Scum API v1"));
        }

        app.UseCors("FrontendCorsPolicy");
        app.UseRateLimiter();
        app.UseResponseCompression();
        app.UseResponseCaching();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseValidateUser();

        return app;
    }
}
