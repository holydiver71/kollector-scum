using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Health check controller for monitoring application status
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly IDataSeedingService _dataSeedingService;

        /// <summary>
        /// Initializes a new instance of the HealthController class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="dataSeedingService">The data seeding service</param>
        public HealthController(ILogger<HealthController> logger, IDataSeedingService dataSeedingService)
        {
            _logger = logger;
            _dataSeedingService = dataSeedingService;
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
                Service = "Kollector Scum API",
                Version = "1.0.0"
            });
        }

        /// <summary>
        /// Seed database with lookup and music release data
        /// </summary>
        /// <returns>Seeding status response</returns>
        [HttpPost("seed")]
        public async Task<IActionResult> SeedData()
        {
            try
            {
                _logger.LogInformation("Data seeding requested at {Timestamp}", DateTime.UtcNow);
                
                await _dataSeedingService.SeedLookupDataAsync();
                
                return Ok(new 
                { 
                    Status = "Success", 
                    Message = "Data seeding completed successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during data seeding: {Message}. Inner Exception: {InnerException}. Stack Trace: {StackTrace}", 
                    ex.Message, 
                    ex.InnerException?.Message ?? "None",
                    ex.StackTrace);
                return StatusCode(500, new 
                { 
                    Status = "Error", 
                    Message = "An error occurred during data seeding",
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Seed only music releases (assumes lookup tables exist)
        /// </summary>
        /// <returns>Seeding status response</returns>
        [HttpPost("seed-music-releases")]
        public async Task<IActionResult> SeedMusicReleases()
        {
            try
            {
                _logger.LogInformation("Music releases seeding requested at {Timestamp}", DateTime.UtcNow);
                
                await _dataSeedingService.SeedMusicReleasesAsync();
                
                return Ok(new 
                { 
                    Status = "Success", 
                    Message = "Music releases seeding completed successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during music releases seeding: {Message}. Inner Exception: {InnerException}. Stack Trace: {StackTrace}", 
                    ex.Message, 
                    ex.InnerException?.Message ?? "None",
                    ex.StackTrace);
                return StatusCode(500, new 
                { 
                    Status = "Error", 
                    Message = "An error occurred during music releases seeding",
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
