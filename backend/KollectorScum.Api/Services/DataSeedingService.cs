using System.Text.Json;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for seeding database with JSON data
    /// </summary>
    public class DataSeedingService : IDataSeedingService
    {
        private readonly KollectorScumDbContext _context;
        private readonly ILogger<DataSeedingService> _logger;
        private readonly string _dataPath;

        public DataSeedingService(KollectorScumDbContext context, ILogger<DataSeedingService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            
            // Get the data path from configuration or use default
            _dataPath = configuration["DataPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "data");
            
            // Ensure the data path is absolute
            if (!Path.IsPathRooted(_dataPath))
            {
                _dataPath = Path.GetFullPath(_dataPath);
            }
        }

        /// <summary>
        /// Seeds all lookup table data from JSON files
        /// </summary>
        public async Task SeedLookupDataAsync()
        {
            _logger.LogInformation("Starting lookup data seeding from path: {DataPath}", _dataPath);

            try
            {
                await SeedCountriesAsync();
                await SeedStoresAsync();
                await SeedFormatsAsync();
                await SeedGenresAsync();
                await SeedLabelsAsync();
                await SeedArtistsAsync();
                await SeedPackagingsAsync();

                _logger.LogInformation("Lookup data seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during lookup data seeding");
                throw;
            }
        }

        /// <summary>
        /// Seeds country data from JSON file
        /// </summary>
        public async Task SeedCountriesAsync()
        {
            var filePath = Path.Combine(_dataPath, "countrys.json");
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Countries JSON file not found at: {FilePath}", filePath);
                return;
            }

            // Check if data already exists
            if (await _context.Countries.AnyAsync())
            {
                _logger.LogInformation("Countries data already exists, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding countries from: {FilePath}", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var container = JsonSerializer.Deserialize<CountriesJsonContainer>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (container?.Countrys != null)
            {
                var countries = container.Countrys.Select(dto => new Country
                {
                    Id = dto.Id,
                    Name = dto.Name
                }).ToList();

                await _context.Countries.AddRangeAsync(countries);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} countries", countries.Count);
            }
        }

        /// <summary>
        /// Seeds store data from JSON file
        /// </summary>
        public async Task SeedStoresAsync()
        {
            var filePath = Path.Combine(_dataPath, "stores.json");
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Stores JSON file not found at: {FilePath}", filePath);
                return;
            }

            // Check if data already exists
            if (await _context.Stores.AnyAsync())
            {
                _logger.LogInformation("Stores data already exists, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding stores from: {FilePath}", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var container = JsonSerializer.Deserialize<StoresJsonContainer>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (container?.Stores != null)
            {
                var stores = container.Stores.Select(dto => new Store
                {
                    Id = dto.Id,
                    Name = dto.Name
                }).ToList();

                await _context.Stores.AddRangeAsync(stores);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} stores", stores.Count);
            }
        }

        /// <summary>
        /// Seeds format data from JSON file
        /// </summary>
        public async Task SeedFormatsAsync()
        {
            var filePath = Path.Combine(_dataPath, "formats.json");
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Formats JSON file not found at: {FilePath}", filePath);
                return;
            }

            // Check if data already exists
            if (await _context.Formats.AnyAsync())
            {
                _logger.LogInformation("Formats data already exists, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding formats from: {FilePath}", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var container = JsonSerializer.Deserialize<FormatsJsonContainer>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (container?.Formats != null)
            {
                var formats = container.Formats.Select(dto => new Format
                {
                    Id = dto.Id,
                    Name = dto.Name
                }).ToList();

                await _context.Formats.AddRangeAsync(formats);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} formats", formats.Count);
            }
        }

        /// <summary>
        /// Seeds genre data from JSON file
        /// </summary>
        public async Task SeedGenresAsync()
        {
            var filePath = Path.Combine(_dataPath, "genres.json");
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Genres JSON file not found at: {FilePath}", filePath);
                return;
            }

            // Check if data already exists
            if (await _context.Genres.AnyAsync())
            {
                _logger.LogInformation("Genres data already exists, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding genres from: {FilePath}", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var container = JsonSerializer.Deserialize<GenresJsonContainer>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (container?.Genres != null)
            {
                var genres = container.Genres.Select(dto => new Genre
                {
                    Id = dto.Id,
                    Name = dto.Name
                }).ToList();

                await _context.Genres.AddRangeAsync(genres);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} genres", genres.Count);
            }
        }

        /// <summary>
        /// Seeds label data from JSON file
        /// </summary>
        public async Task SeedLabelsAsync()
        {
            var filePath = Path.Combine(_dataPath, "labels.json");
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Labels JSON file not found at: {FilePath}", filePath);
                return;
            }

            // Check if data already exists
            if (await _context.Labels.AnyAsync())
            {
                _logger.LogInformation("Labels data already exists, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding labels from: {FilePath}", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var container = JsonSerializer.Deserialize<LabelsJsonContainer>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (container?.Labels != null)
            {
                var labels = container.Labels.Select(dto => new Label
                {
                    Id = dto.Id,
                    Name = dto.Name
                }).ToList();

                await _context.Labels.AddRangeAsync(labels);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} labels", labels.Count);
            }
        }

        /// <summary>
        /// Seeds artist data from JSON file
        /// </summary>
        public async Task SeedArtistsAsync()
        {
            var filePath = Path.Combine(_dataPath, "artists.json");
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Artists JSON file not found at: {FilePath}", filePath);
                return;
            }

            // Check if data already exists
            if (await _context.Artists.AnyAsync())
            {
                _logger.LogInformation("Artists data already exists, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding artists from: {FilePath}", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var container = JsonSerializer.Deserialize<ArtistsJsonContainer>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (container?.Artists != null)
            {
                var artists = container.Artists.Select(dto => new Artist
                {
                    Id = dto.Id,
                    Name = dto.Name
                }).ToList();

                await _context.Artists.AddRangeAsync(artists);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} artists", artists.Count);
            }
        }

        /// <summary>
        /// Seeds packaging data from JSON file
        /// </summary>
        public async Task SeedPackagingsAsync()
        {
            var filePath = Path.Combine(_dataPath, "packagings.json");
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Packagings JSON file not found at: {FilePath}", filePath);
                return;
            }

            // Check if data already exists
            if (await _context.Packagings.AnyAsync())
            {
                _logger.LogInformation("Packagings data already exists, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding packagings from: {FilePath}", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var container = JsonSerializer.Deserialize<PackagingsJsonContainer>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (container?.Packagings != null)
            {
                var packagings = container.Packagings.Select(dto => new Packaging
                {
                    Id = dto.Id,
                    Name = dto.Name
                }).ToList();

                await _context.Packagings.AddRangeAsync(packagings);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} packagings", packagings.Count);
            }
        }
    }
}
