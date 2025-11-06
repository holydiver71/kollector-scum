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
        private readonly IMusicReleaseImportOrchestrator _importOrchestrator;
        private readonly ILogger<SeedController> _logger;

        public SeedController(
            IDataSeedingService dataSeedingService, 
            IMusicReleaseImportOrchestrator importOrchestrator,
            ILogger<SeedController> logger)
        {
            _dataSeedingService = dataSeedingService;
            _importOrchestrator = importOrchestrator;
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

        /// <summary>
        /// Imports all music releases from JSON file
        /// </summary>
        /// <returns>Result of import operation</returns>
        [HttpPost("music-releases")]
        public async Task<ActionResult> ImportMusicReleases()
        {
            try
            {
                _logger.LogInformation("Starting music releases import via API");
                var importedCount = await _importOrchestrator.ImportMusicReleasesAsync();
                return Ok(new { Message = $"Music releases import completed successfully. Imported {importedCount} releases." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during music releases import");
                return StatusCode(500, new { Message = "Error occurred during music releases import", Error = ex.Message });
            }
        }

        /// <summary>
        /// Imports a batch of music releases from JSON file
        /// </summary>
        /// <param name="batchSize">Size of batch to import (default: 100)</param>
        /// <param name="skipCount">Number of records to skip (default: 0)</param>
        /// <returns>Result of import operation</returns>
        [HttpPost("music-releases/batch")]
        public async Task<ActionResult> ImportMusicReleasesBatch([FromQuery] int batchSize = 100, [FromQuery] int skipCount = 0)
        {
            try
            {
                _logger.LogInformation("Starting music releases batch import via API (size: {BatchSize}, skip: {SkipCount})", batchSize, skipCount);
                var importedCount = await _importOrchestrator.ImportMusicReleasesBatchAsync(batchSize, skipCount);
                return Ok(new { Message = $"Music releases batch import completed. Imported {importedCount} releases." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during music releases batch import");
                return StatusCode(500, new { Message = "Error occurred during music releases batch import", Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets import progress information
        /// </summary>
        /// <returns>Import progress details</returns>
        [HttpGet("music-releases/progress")]
        public async Task<ActionResult> GetImportProgress()
        {
            try
            {
                var progress = await _importOrchestrator.GetImportProgressAsync();
                return Ok(progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting import progress");
                return StatusCode(500, new { Message = "Error occurred while getting progress", Error = ex.Message });
            }
        }

        /// <summary>
        /// Validates that lookup data is ready for music release import
        /// </summary>
        /// <returns>Validation result</returns>
        [HttpGet("music-releases/validate")]
        public async Task<ActionResult> ValidateLookupData()
        {
            try
            {
                var (isValid, errors) = await _importOrchestrator.ValidateLookupDataAsync();
                
                if (isValid)
                {
                    return Ok(new { IsValid = true, Message = "Lookup data validation passed" });
                }
                else
                {
                    return BadRequest(new { IsValid = false, Errors = errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during lookup data validation");
                return StatusCode(500, new { Message = "Error occurred during validation", Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the total count of music releases available for import
        /// </summary>
        /// <returns>Count of music releases</returns>
        [HttpGet("music-releases/count")]
        public async Task<ActionResult> GetMusicReleaseCount()
        {
            try
            {
                var count = await _importOrchestrator.GetMusicReleaseCountAsync();
                return Ok(new { Count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting music release count");
                return StatusCode(500, new { Message = "Error occurred while getting count", Error = ex.Message });
            }
        }

        /// <summary>
        /// Updates UPC values for existing music releases from JSON file
        /// </summary>
        /// <returns>Result of update operation</returns>
        [HttpPost("music-releases/update-upc")]
        public async Task<ActionResult> UpdateUpcValues()
        {
            try
            {
                _logger.LogInformation("Starting UPC update via API");
                var updatedCount = await _importOrchestrator.UpdateUpcValuesAsync();
                return Ok(new { Message = $"UPC update completed successfully. Updated {updatedCount} releases." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during UPC update");
                return StatusCode(500, new { Message = "Error occurred during UPC update", Error = ex.Message });
            }
        }
    }
}

