using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for processing batches of music release imports
    /// Handles batch processing, transaction management, and validation
    /// </summary>
    public class MusicReleaseBatchProcessor : IMusicReleaseBatchProcessor
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MusicReleaseBatchProcessor> _logger;

        public MusicReleaseBatchProcessor(
            IUnitOfWork unitOfWork,
            ILogger<MusicReleaseBatchProcessor> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> ProcessBatchAsync(List<MusicReleaseImportDto> releases)
        {
            if (releases == null || releases.Count == 0)
            {
                return 0;
            }

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

        public async Task<int> UpdateUpcBatchAsync(List<MusicReleaseImportDto> releases)
        {
            if (releases == null || releases.Count == 0)
            {
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
                _logger.LogInformation("UPC Batch complete: Updated={UpdatedCount}, Skipped={SkippedCount}, NotFound={NotFoundCount}, Total={TotalCount}", 
                    updatedCount, skippedCount, notFoundCount, releases.Count);
                return updatedCount;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

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

        #region Private Helper Methods

        /// <summary>
        /// Maps a DTO to a MusicRelease entity
        /// </summary>
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
                    LabelId = dto.LabelId == 0 ? null : dto.LabelId,
                    CountryId = dto.CountryId == 0 ? null : dto.CountryId,
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
        private DateTime? ParseDateString(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            if (DateTime.TryParse(dateString, out var date))
                return date;

            return null;
        }

        #endregion
    }
}
