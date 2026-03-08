using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
        private readonly HealthCheckService _healthCheckService;

        /// <summary>
        /// Initializes a new instance of the HealthController class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="dataSeedingService">The data seeding service</param>
        /// <param name="healthCheckService">The ASP.NET Core health check service</param>
        public HealthController(
            ILogger<HealthController> logger,
            IDataSeedingService dataSeedingService,
            HealthCheckService healthCheckService)
        {
            _logger = logger;
            _dataSeedingService = dataSeedingService;
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Health check endpoint. Runs all registered health checks (including database)
        /// and returns status for the API and each dependency.
        /// </summary>
        /// <returns>Health status response including DbStatus</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogInformation("Health check requested at {Timestamp}", DateTime.UtcNow);

            HealthReport report;
            try
            {
                report = await _healthCheckService.CheckHealthAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check service threw an exception");
                return Ok(new
                {
                    Status = "Degraded",
                    DbStatus = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Service = "Kollector Scum API",
                    Version = "1.0.0"
                });
            }

            var dbStatus = report.Entries.TryGetValue("database", out var dbEntry)
                && dbEntry.Status == HealthStatus.Healthy
                    ? "Healthy"
                    : "Unhealthy";

            var overallStatus = report.Status == HealthStatus.Healthy ? "Healthy" : "Degraded";

            return Ok(new
            {
                Status = overallStatus,
                DbStatus = dbStatus,
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
