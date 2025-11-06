using Microsoft.AspNetCore.Mvc;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Base API controller providing common functionality for all controllers
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly ILogger _logger;

        protected BaseApiController(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles errors and returns appropriate status code with error message
        /// </summary>
        protected ActionResult HandleError(Exception ex, string context)
        {
            _logger.LogError(ex, "Error in {Context}", context);

            return ex switch
            {
                ArgumentException => BadRequest(ex.Message),
                KeyNotFoundException => NotFound(ex.Message),
                InvalidOperationException => BadRequest(ex.Message),
                _ => StatusCode(500, "An error occurred while processing your request")
            };
        }

        /// <summary>
        /// Validates pagination parameters
        /// </summary>
        protected ActionResult? ValidatePaginationParameters(int page, int pageSize, int maxPageSize = 5000)
        {
            if (page < 1)
            {
                return BadRequest("Page must be greater than 0");
            }

            if (pageSize < 1 || pageSize > maxPageSize)
            {
                return BadRequest($"Page size must be between 1 and {maxPageSize}");
            }

            return null;
        }

        /// <summary>
        /// Logs the operation being performed
        /// </summary>
        protected void LogOperation(string operation, object? parameters = null)
        {
            if (parameters != null)
            {
                _logger.LogInformation("Operation: {Operation}, Parameters: {@Parameters}", operation, parameters);
            }
            else
            {
                _logger.LogInformation("Operation: {Operation}", operation);
            }
        }
    }
}
