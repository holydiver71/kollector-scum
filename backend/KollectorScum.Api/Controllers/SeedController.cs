using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Controller for data seeding operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SeedController : ControllerBase
    {
        private readonly IDataSeedingService _dataSeedingService;
        private readonly ILogger<SeedController> _logger;

        public SeedController(IDataSeedingService dataSeedingService, ILogger<SeedController> logger)
        {
            _dataSeedingService = dataSeedingService;
            _logger = logger;
        }

        /// <summary>
        /// Seeds all lookup table data from JSON files
        /// </summary>
        /// <returns>Result of seeding operation</returns>
        [HttpPost("lookup-data")]
        public async Task<ActionResult> SeedLookupData()
        {
            try
            {
                _logger.LogInformation("Starting lookup data seeding via API");
                await _dataSeedingService.SeedLookupDataAsync();
                return Ok(new { Message = "Lookup data seeding completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during lookup data seeding");
                return StatusCode(500, new { Message = "Error occurred during seeding", Error = ex.Message });
            }
        }

        /// <summary>
        /// Seeds country data from JSON file
        /// </summary>
        /// <returns>Result of seeding operation</returns>
        [HttpPost("countries")]
        public async Task<ActionResult> SeedCountries()
        {
            try
            {
                await _dataSeedingService.SeedCountriesAsync();
                return Ok(new { Message = "Countries seeding completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during countries seeding");
                return StatusCode(500, new { Message = "Error occurred during countries seeding", Error = ex.Message });
            }
        }

        /// <summary>
        /// Seeds store data from JSON file
        /// </summary>
        /// <returns>Result of seeding operation</returns>
        [HttpPost("stores")]
        public async Task<ActionResult> SeedStores()
        {
            try
            {
                await _dataSeedingService.SeedStoresAsync();
                return Ok(new { Message = "Stores seeding completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during stores seeding");
                return StatusCode(500, new { Message = "Error occurred during stores seeding", Error = ex.Message });
            }
        }

        /// <summary>
        /// Seeds format data from JSON file
        /// </summary>
        /// <returns>Result of seeding operation</returns>
        [HttpPost("formats")]
        public async Task<ActionResult> SeedFormats()
        {
            try
            {
                await _dataSeedingService.SeedFormatsAsync();
                return Ok(new { Message = "Formats seeding completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during formats seeding");
                return StatusCode(500, new { Message = "Error occurred during formats seeding", Error = ex.Message });
            }
        }

        /// <summary>
        /// Seeds genre data from JSON file
        /// </summary>
        /// <returns>Result of seeding operation</returns>
        [HttpPost("genres")]
        public async Task<ActionResult> SeedGenres()
        {
            try
            {
                await _dataSeedingService.SeedGenresAsync();
                return Ok(new { Message = "Genres seeding completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during genres seeding");
                return StatusCode(500, new { Message = "Error occurred during genres seeding", Error = ex.Message });
            }
        }

        /// <summary>
        /// Seeds label data from JSON file
        /// </summary>
        /// <returns>Result of seeding operation</returns>
        [HttpPost("labels")]
        public async Task<ActionResult> SeedLabels()
        {
            try
            {
                await _dataSeedingService.SeedLabelsAsync();
                return Ok(new { Message = "Labels seeding completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during labels seeding");
                return StatusCode(500, new { Message = "Error occurred during labels seeding", Error = ex.Message });
            }
        }

        /// <summary>
        /// Seeds artist data from JSON file
        /// </summary>
        /// <returns>Result of seeding operation</returns>
        [HttpPost("artists")]
        public async Task<ActionResult> SeedArtists()
        {
            try
            {
                await _dataSeedingService.SeedArtistsAsync();
                return Ok(new { Message = "Artists seeding completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during artists seeding");
                return StatusCode(500, new { Message = "Error occurred during artists seeding", Error = ex.Message });
            }
        }

        /// <summary>
        /// Seeds packaging data from JSON file
        /// </summary>
        /// <returns>Result of seeding operation</returns>
        [HttpPost("packagings")]
        public async Task<ActionResult> SeedPackagings()
        {
            try
            {
                await _dataSeedingService.SeedPackagingsAsync();
                return Ok(new { Message = "Packagings seeding completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during packagings seeding");
                return StatusCode(500, new { Message = "Error occurred during packagings seeding", Error = ex.Message });
            }
        }
    }
}
