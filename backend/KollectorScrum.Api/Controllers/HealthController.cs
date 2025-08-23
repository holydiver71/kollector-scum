using Microsoft.AspNetCore.Mvc;

namespace KollectorScrum.Api.Controllers
{
    /// <summary>
    /// Health check controller for monitoring application status
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        /// <summary>
        /// Initializes a new instance of the HealthController class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Basic health check endpoint
        /// </summary>
        /// <returns>Health status response</returns>
        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("Health check requested at {Timestamp}", DateTime.UtcNow);
            
            return Ok(new 
            { 
                Status = "Healthy", 
                Timestamp = DateTime.UtcNow,
                Service = "Kollector Scrum API",
                Version = "1.0.0"
            });
        }
    }
}
