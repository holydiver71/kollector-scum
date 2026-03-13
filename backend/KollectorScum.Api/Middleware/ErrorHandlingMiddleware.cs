using System.Net;
using System.Text.Json;

namespace KollectorScum.Api.Middleware
{
    /// <summary>
    /// Global exception handling middleware that catches unhandled exceptions, logs them,
    /// and returns a safe JSON error response.
    /// In non-Development environments the internal exception message is suppressed to
    /// prevent information disclosure (OWASP A09).
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="env">The web host environment used to determine whether to include exception details.</param>
        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Invokes the middleware, forwarding the request and catching any unhandled exceptions.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex, _env.IsDevelopment());
            }
        }

        /// <summary>
        /// Handles exceptions and returns appropriate error responses.
        /// Exception details are only included in Development to avoid information disclosure.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="isDevelopment">Whether the application is running in the Development environment.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task HandleExceptionAsync(HttpContext context, Exception exception, bool isDevelopment)
        {
            context.Response.ContentType = "application/json";

            int statusCode;
            switch (exception)
            {
                case ArgumentException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    break;
                case NotImplementedException:
                    statusCode = (int)HttpStatusCode.NotImplemented;
                    break;
                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            context.Response.StatusCode = statusCode;

            // Only include the raw exception message in Development.
            // In all other environments return a generic message to prevent information disclosure.
            string? details = isDevelopment ? exception.Message : null;

            var response = new
            {
                message = "An error occurred while processing your request.",
                details
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
