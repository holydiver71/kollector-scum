using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Generic service for reading and deserializing JSON files
    /// Pure infrastructure concern - no business logic
    /// </summary>
    public class JsonFileReader : IJsonFileReader
    {
        private readonly ILogger<JsonFileReader> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonFileReader(ILogger<JsonFileReader> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<T?> ReadJsonFileAsync<T>(string filePath) where T : class
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("JSON file not found at: {FilePath}", filePath);
                return null;
            }

            try
            {
                _logger.LogDebug("Reading JSON file: {FilePath}", filePath);
                var jsonContent = await File.ReadAllTextAsync(filePath);
                
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogWarning("JSON file is empty: {FilePath}", filePath);
                    return null;
                }

                var result = JsonSerializer.Deserialize<T>(jsonContent, _jsonOptions);
                
                if (result == null)
                {
                    _logger.LogWarning("Failed to deserialize JSON from: {FilePath}", filePath);
                }
                
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error for file: {FilePath}", filePath);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading JSON file: {FilePath}", filePath);
                throw;
            }
        }

        public bool FileExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            return File.Exists(filePath);
        }

        public async Task<int> GetJsonArrayCountAsync<T>(string filePath) where T : class
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return 0;
            }

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("JSON file not found at: {FilePath}", filePath);
                return 0;
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var items = JsonSerializer.Deserialize<List<T>>(jsonContent, _jsonOptions);
                return items?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting count from JSON file: {FilePath}", filePath);
                return 0;
            }
        }
    }
}
