using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Logging;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for validating music release operations
    /// Handles: Pre-create validation, pre-update validation, duplicate checks
    /// </summary>
    public class MusicReleaseValidator : IMusicReleaseValidator
    {
        private readonly IMusicReleaseDuplicateDetector _duplicateDetector;
        private readonly IMusicReleaseMapperService _mapper;
        private readonly ILogger<MusicReleaseValidator> _logger;

        public MusicReleaseValidator(
            IMusicReleaseDuplicateDetector duplicateDetector,
            IMusicReleaseMapperService mapper,
            ILogger<MusicReleaseValidator> logger)
        {
            _duplicateDetector = duplicateDetector ?? throw new ArgumentNullException(nameof(duplicateDetector));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(bool IsValid, string? ErrorMessage, List<MusicReleaseSummaryDto>? Duplicates)> 
            ValidateCreateAsync(CreateMusicReleaseDto createDto)
        {
            _logger.LogInformation("Validating create request for: {Title}", createDto.Title);

            // Basic validation
            if (string.IsNullOrWhiteSpace(createDto.Title))
            {
                return (false, "Title is required", null);
            }

            // Check for duplicates
            var duplicateReleases = await _duplicateDetector.FindDuplicatesAsync(
                createDto.LabelNumber, 
                createDto.Title, 
                createDto.ArtistNames);

            if (duplicateReleases.Any())
            {
                _logger.LogWarning("Potential duplicates found for: {Title}", createDto.Title);
                
                var duplicateDtos = duplicateReleases.Select(d => _mapper.MapToSummaryDto(d)).ToList();
                var errorMessage = $"Potential duplicate release found. Similar release(s) exist: {string.Join(", ", duplicateReleases.Select(d => $"'{d.Title}' (ID: {d.Id})"))}";
                
                return (false, errorMessage, duplicateDtos);
            }

            return (true, null, null);
        }

        public async Task<(bool IsValid, string? ErrorMessage)> 
            ValidateUpdateAsync(int id, UpdateMusicReleaseDto updateDto)
        {
            _logger.LogInformation("Validating update request for ID: {Id}", id);

            // Basic validation
            if (string.IsNullOrWhiteSpace(updateDto.Title))
            {
                return (false, "Title is required");
            }

            // Additional validation rules can be added here
            // For example, checking if referenced entities exist

            return await Task.FromResult((true, (string?)null));
        }
    }
}
