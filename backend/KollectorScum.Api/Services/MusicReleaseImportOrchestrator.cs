using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Logging;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Orchestrator service for music release imports
    /// Slim coordinator that delegates to FileReader and BatchProcessor
    /// </summary>
    public class MusicReleaseImportOrchestrator : IMusicReleaseImportOrchestrator
    {
        private readonly IJsonFileReader _fileReader;
        private readonly IMusicReleaseBatchProcessor _batchProcessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MusicReleaseImportOrchestrator> _logger;
        private readonly string _dataPath;

        public MusicReleaseImportOrchestrator(
            IJsonFileReader fileReader,
            IMusicReleaseBatchProcessor batchProcessor,
            IUnitOfWork unitOfWork,
            ILogger<MusicReleaseImportOrchestrator> logger,
            IConfiguration configuration)
        {
            _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
            _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            var config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dataPath = config["DataPath"] ?? throw new InvalidOperationException("DataPath configuration is missing");
        }

        // Constructor for testing that allows specifying the data path directly
        public MusicReleaseImportOrchestrator(
            IJsonFileReader fileReader,
            IMusicReleaseBatchProcessor batchProcessor,
            IUnitOfWork unitOfWork,
            ILogger<MusicReleaseImportOrchestrator> logger,
            string? dataPath = null)
        {
            _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
            _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataPath = dataPath ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "data");
        }

        public async Task<int> ImportMusicReleasesAsync()
        {
            var filePath = Path.Combine(_dataPath, "musicreleases.json");
            
            if (!_fileReader.FileExists(filePath))
            {
                _logger.LogWarning("MusicReleases JSON file not found at: {FilePath}", filePath);
                return 0;
            }

            _logger.LogInformation("Starting import of music releases from: {FilePath}", filePath);

            try
            {
                var releases = await _fileReader.ReadJsonFileAsync<List<MusicReleaseImportDto>>(filePath);

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
                    var batchCount = await _batchProcessor.ProcessBatchAsync(batch);
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

        public async Task<int> ImportMusicReleasesBatchAsync(int batchSize, int skipCount = 0)
        {
            var filePath = Path.Combine(_dataPath, "musicreleases.json");
            
            if (!_fileReader.FileExists(filePath))
            {
                _logger.LogWarning("MusicReleases JSON file not found at: {FilePath}", filePath);
                return 0;
            }

            try
            {
                var releases = await _fileReader.ReadJsonFileAsync<List<MusicReleaseImportDto>>(filePath);

                if (releases == null || releases.Count == 0)
                {
                    return 0;
                }

                var batch = releases.Skip(skipCount).Take(batchSize).ToList();
                return await _batchProcessor.ProcessBatchAsync(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing music release batch (skip: {SkipCount}, size: {BatchSize})", skipCount, batchSize);
                throw;
            }
        }

        public async Task<int> GetMusicReleaseCountAsync()
        {
            var filePath = Path.Combine(_dataPath, "musicreleases.json");
            
            if (!_fileReader.FileExists(filePath))
            {
                return 0;
            }

            try
            {
                return await _fileReader.GetJsonArrayCountAsync<MusicReleaseImportDto>(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting music release count from {FilePath}", filePath);
                return 0;
            }
        }

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

        public async Task<int> UpdateUpcValuesAsync()
        {
            var filePath = Path.Combine(_dataPath, "musicreleases.json");
            
            if (!_fileReader.FileExists(filePath))
            {
                _logger.LogWarning("MusicReleases JSON file not found at: {FilePath}", filePath);
                return 0;
            }

            _logger.LogInformation("Starting UPC update from: {FilePath}", filePath);

            try
            {
                var releases = await _fileReader.ReadJsonFileAsync<List<MusicReleaseImportDto>>(filePath);

                if (releases == null || releases.Count == 0)
                {
                    _logger.LogWarning("No music releases found in JSON file");
                    return 0;
                }

                var updatedCount = await _batchProcessor.UpdateUpcBatchAsync(releases);
                
                _logger.LogInformation("UPC Update complete: Updated={UpdatedCount} out of {TotalCount}", updatedCount, releases.Count);
                return updatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating UPC values from {FilePath}", filePath);
                throw;
            }
        }

        public async Task<(bool IsValid, List<string> Errors)> ValidateLookupDataAsync()
        {
            return await _batchProcessor.ValidateLookupDataAsync();
        }
    }
}
