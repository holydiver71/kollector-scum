namespace KollectorScum.Api.Middleware
{
    /// <summary>
    /// Middleware that adds HTTP security headers to every response.
    /// Addresses OWASP A05 (Security Misconfiguration) by setting defensive headers that
    /// prevent clickjacking, MIME-sniffing, and restrict resource loading.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initialises a new instance of <see cref="SecurityHeadersMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware delegate in the pipeline.</param>
        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        /// <summary>
        /// Processes the request, adds security headers, then passes control to the next middleware.
        /// </summary>
        /// <param name="context">The current <see cref="HttpContext"/>.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            // Prevent MIME-type sniffing – forces browser to respect declared Content-Type
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // Prevent the page from being embedded in a frame (clickjacking protection)
            context.Response.Headers["X-Frame-Options"] = "DENY";

            // Legacy XSS filter for older browsers
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

            // Limit information sent in the Referer header
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Disable unused browser features
            context.Response.Headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(), payment=()";

            // Content-Security-Policy: restrict where resources can be loaded from.
            // The API only returns JSON so a restrictive policy is safe.
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'none'; frame-ancestors 'none'";

            await _next(context);
        }
    }

    /// <summary>
    /// Extension methods for registering <see cref="SecurityHeadersMiddleware"/>.
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {
        /// <summary>
        /// Adds the <see cref="SecurityHeadersMiddleware"/> to the application pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
