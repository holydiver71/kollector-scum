using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for importing MusicRelease data from JSON files
    /// </summary>
    public class MusicReleaseImportService : IMusicReleaseImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MusicReleaseImportService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _dataPath;

        public MusicReleaseImportService(
            IUnitOfWork unitOfWork,
            ILogger<MusicReleaseImportService> logger,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dataPath = _configuration["DataPath"] ?? throw new InvalidOperationException("DataPath configuration is missing");
        }

        // Constructor for testing that allows specifying the data path directly
        public MusicReleaseImportService(
            IUnitOfWork unitOfWork,
            ILogger<MusicReleaseImportService> logger,
            string? dataPath = null)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = null!; // Not used in test constructor
            _dataPath = dataPath ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "data");
        }

        /// <summary>
        /// Imports music releases from JSON file
        /// </summary>
        /// <returns>Number of releases imported</returns>
        public async Task<int> ImportMusicReleasesAsync()
        {
            var filePath = Path.Combine(_dataPath, "musicreleases.json");
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("MusicReleases JSON file not found at: {FilePath}", filePath);
                return 0;
            }

            _logger.LogInformation("Starting import of music releases from: {FilePath}", filePath);

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var releases = JsonSerializer.Deserialize<List<MusicReleaseImportDto>>(jsonContent);

                if (releases == null || releases.Count == 0)
                {
                    _logger.LogWarning("No music releases found in JSON file");
                    return 0;
                }

                var importedCount = 0;
                var batchSize = 100; // Process in batches to avoid memory issues

                for (int i = 0; i < releases.Count; i += batchSize)
                {
                    var batch = releases.Skip(i).Take(batchSize).ToList();
                    var batchCount = await ImportBatchAsync(batch);
                    importedCount += batchCount;
                    
                    _logger.LogInformation("Imported batch {BatchNumber}: {BatchCount} releases (Total: {ImportedCount}/{TotalCount})", 
                        (i / batchSize) + 1, batchCount, importedCount, releases.Count);
                }

                _logger.LogInformation("Completed import of {ImportedCount} music releases", importedCount);
                return importedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing music releases from {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Imports a batch of music releases
        /// </summary>
        /// <param name="batchSize">Size of batch to import</param>
        /// <param name="skipCount">Number of records to skip</param>
        /// <returns>Number of releases imported in this batch</returns>
        public async Task<int> ImportMusicReleasesBatchAsync(int batchSize, int skipCount = 0)
        {
            var filePath = Path.Combine(_dataPath, "musicreleases.json");
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("MusicReleases JSON file not found at: {FilePath}", filePath);
                return 0;
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var releases = JsonSerializer.Deserialize<List<MusicReleaseImportDto>>(jsonContent);

                if (releases == null || releases.Count == 0)
                {
                    return 0;
                }

                var batch = releases.Skip(skipCount).Take(batchSize).ToList();
                return await ImportBatchAsync(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing music release batch (skip: {SkipCount}, size: {BatchSize})", skipCount, batchSize);
                throw;
            }
        }

        /// <summary>
        /// Imports a batch of music releases
        /// </summary>
        /// <param name="releases">Batch of releases to import</param>
        /// <returns>Number of releases imported</returns>
        private async Task<int> ImportBatchAsync(List<MusicReleaseImportDto> releases)
        {
            await _unitOfWork.BeginTransactionAsync();
            
            try
            {
                var importedCount = 0;

                foreach (var releaseDto in releases)
                {
                    try
                    {
                        // Check if release already exists
                        var existingRelease = await _unitOfWork.MusicReleases.GetByIdAsync(releaseDto.Id);
                        if (existingRelease != null)
                        {
                            _logger.LogDebug("Skipping existing release: {ReleaseId} - {Title}", releaseDto.Id, releaseDto.Title);
                            continue;
                        }

                        var musicRelease = await MapToMusicReleaseAsync(releaseDto);
                        
                        if (musicRelease != null)
                        {
                            await _unitOfWork.MusicReleases.AddAsync(musicRelease);
                            importedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error importing release {ReleaseId} - {Title}", releaseDto.Id, releaseDto.Title);
                        // Continue with next release instead of failing entire batch
                    }
                }

                await _unitOfWork.CommitTransactionAsync();
                return importedCount;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Maps a DTO to a MusicRelease entity
        /// </summary>
        /// <param name="dto">DTO to map</param>
        /// <returns>Mapped MusicRelease entity or null if mapping fails</returns>
        private async Task<MusicRelease?> MapToMusicReleaseAsync(MusicReleaseImportDto dto)
        {
            try
            {
                // Validate required lookup data exists
                if (!await ValidateLookupDataForReleaseAsync(dto))
                {
                    return null;
                }

                // Parse dates
                DateTime? releaseDate = ParseDateString(dto.ReleaseYear);
                DateTime? originalReleaseDate = ParseDateString(dto.OrigReleaseYear);
                
                // Parse length
                int? lengthInSeconds = null;
                if (int.TryParse(dto.LengthInSeconds, out var parsedLength))
                {
                    lengthInSeconds = parsedLength;
                }

                // Create the MusicRelease entity
                var musicRelease = new MusicRelease
                {
                    Id = dto.Id,
                    Title = dto.Title,
                    ReleaseYear = releaseDate,
                    OrigReleaseYear = originalReleaseDate,
                    Live = dto.Live,
                    LabelId = dto.LabelId == 0 ? null : dto.LabelId, // Handle 0 as null
                    CountryId = dto.CountryId == 0 ? null : dto.CountryId, // Handle 0 as null
                    LabelNumber = dto.LabelNumber,
                    LengthInSeconds = lengthInSeconds,
                    FormatId = dto.FormatId,
                    PackagingId = dto.PackagingId,
                    Upc = dto.Upc,
                    DateAdded = dto.DateAdded,
                    LastModified = dto.LastModified,
                    Artists = dto.Artists?.Count > 0 ? JsonSerializer.Serialize(dto.Artists) : null,
                    Genres = dto.Genres?.Count > 0 ? JsonSerializer.Serialize(dto.Genres) : null
                };

                // Map Links as JSON
                if (dto.Links?.Count > 0)
                {
                    musicRelease.Links = JsonSerializer.Serialize(dto.Links);
                }

                // Map Media and Tracks as JSON
                if (dto.Media?.Count > 0)
                {
                    musicRelease.Media = JsonSerializer.Serialize(dto.Media);
                }

                return musicRelease;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping release {ReleaseId} - {Title}", dto.Id, dto.Title);
                return null;
            }
        }

        /// <summary>
        /// Validates that required lookup data exists for a release
        /// </summary>
        /// <param name="dto">Release DTO to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        private async Task<bool> ValidateLookupDataForReleaseAsync(MusicReleaseImportDto dto)
        {
            // Check format exists
            if (!(await _unitOfWork.Formats.AnyAsync(f => f.Id == dto.FormatId)))
            {
                _logger.LogWarning("Format {FormatId} not found for release {ReleaseId}", dto.FormatId, dto.Id);
                return false;
            }

            // Check packaging exists
            if (!(await _unitOfWork.Packagings.AnyAsync(p => p.Id == dto.PackagingId)))
            {
                _logger.LogWarning("Packaging {PackagingId} not found for release {ReleaseId}", dto.PackagingId, dto.Id);
                return false;
            }

            // Check label exists (if specified)
            if (dto.LabelId > 0 && !(await _unitOfWork.Labels.AnyAsync(l => l.Id == dto.LabelId)))
            {
                _logger.LogWarning("Label {LabelId} not found for release {ReleaseId}", dto.LabelId, dto.Id);
                return false;
            }

            // Check country exists (if specified)
            if (dto.CountryId > 0 && !(await _unitOfWork.Countries.AnyAsync(c => c.Id == dto.CountryId)))
            {
                _logger.LogWarning("Country {CountryId} not found for release {ReleaseId}", dto.CountryId, dto.Id);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses a date string to DateTime
        /// </summary>
        /// <param name="dateString">Date string to parse</param>
        /// <returns>Parsed DateTime or null</returns>
        private DateTime? ParseDateString(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            if (DateTime.TryParse(dateString, out var date))
                return date;

            return null;
        }

        /// <summary>
        /// Gets the total count of music releases in the JSON file
        /// </summary>
        /// <returns>Total count of releases</returns>
        public async Task<int> GetMusicReleaseCountAsync()
        {
            var filePath = Path.Combine(_dataPath, "musicreleases.json");
            
            if (!File.Exists(filePath))
            {
                return 0;
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var releases = JsonSerializer.Deserialize<List<MusicReleaseImportDto>>(jsonContent);
                return releases?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting music release count from {FilePath}", filePath);
                return 0;
            }
        }

        /// <summary>
        /// Validates that all required lookup data exists for import
        /// </summary>
        /// <returns>Validation result with details</returns>
        public async Task<(bool IsValid, List<string> Errors)> ValidateLookupDataAsync()
        {
            var errors = new List<string>();

            try
            {
                // Check if lookup tables have data
                var countryCount = await _unitOfWork.Countries.CountAsync();
                var formatCount = await _unitOfWork.Formats.CountAsync();
                var labelCount = await _unitOfWork.Labels.CountAsync();
                var packagingCount = await _unitOfWork.Packagings.CountAsync();

                if (countryCount == 0) errors.Add("No countries found in database");
                if (formatCount == 0) errors.Add("No formats found in database");
                if (labelCount == 0) errors.Add("No labels found in database");
                if (packagingCount == 0) errors.Add("No packagings found in database");

                _logger.LogInformation("Lookup data validation: Countries={CountryCount}, Formats={FormatCount}, Labels={LabelCount}, Packagings={PackagingCount}",
                    countryCount, formatCount, labelCount, packagingCount);

                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating lookup data");
                errors.Add($"Error validating lookup data: {ex.Message}");
                return (false, errors);
            }
        }

        /// <summary>
        /// Gets import progress information
        /// </summary>
        /// <returns>Import progress details</returns>
        public async Task<ImportProgressInfo> GetImportProgressAsync()
        {
            try
            {
                var totalRecords = await GetMusicReleaseCountAsync();
                var importedRecords = await _unitOfWork.MusicReleases.CountAsync();

                return new ImportProgressInfo
                {
                    TotalRecords = totalRecords,
                    ImportedRecords = importedRecords,
                    Errors = new List<string>() // Could be enhanced to track actual errors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting import progress");
                return new ImportProgressInfo
                {
                    Errors = new List<string> { $"Error getting progress: {ex.Message}" }
                };
            }
        }

        /// <summary>
        /// Updates UPC values for existing music releases from JSON file
        /// </summary>
        /// <returns>Number of releases updated</returns>
        public async Task<int> UpdateUpcValuesAsync()
        {
            var filePath = Path.Combine(_dataPath, "musicreleases.json");
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("MusicReleases JSON file not found at: {FilePath}", filePath);
                return 0;
            }

            _logger.LogInformation("Starting UPC update from: {FilePath}", filePath);

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var releases = JsonSerializer.Deserialize<List<MusicReleaseImportDto>>(jsonContent);

                if (releases == null || releases.Count == 0)
                {
                    _logger.LogWarning("No music releases found in JSON file");
                    return 0;
                }

                var updatedCount = 0;
                var skippedCount = 0;
                var notFoundCount = 0;

                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    foreach (var releaseDto in releases)
                    {
                        try
                        {
                            // Get existing release
                            var existingRelease = await _unitOfWork.MusicReleases.GetByIdAsync(releaseDto.Id);
                            if (existingRelease == null)
                            {
                                notFoundCount++;
                                continue;
                            }

                            if (!string.IsNullOrEmpty(releaseDto.Upc))
                            {
                                existingRelease.Upc = releaseDto.Upc;
                                _unitOfWork.MusicReleases.Update(existingRelease);
                                updatedCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error updating UPC for release {ReleaseId}", releaseDto.Id);
                        }
                    }

                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("UPC Update complete: Updated={UpdatedCount}, Skipped={SkippedCount}, NotFound={NotFoundCount}, Total={TotalCount}", 
                        updatedCount, skippedCount, notFoundCount, releases.Count);
                    return updatedCount;
                }
                catch (Exception)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating UPC values from {FilePath}", filePath);
                throw;
            }
        }
    }
}

