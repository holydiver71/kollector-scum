using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Orchestrates seeding of all lookup tables
    /// Slim coordinator that delegates to specialized seeders
    /// </summary>
    public class DataSeedingOrchestrator : IDataSeedingOrchestrator
    {
        private readonly ILookupSeeder<Country, CountryJsonDto> _countrySeeder;
        private readonly ILookupSeeder<Store, StoreJsonDto> _storeSeeder;
        private readonly ILookupSeeder<Format, FormatJsonDto> _formatSeeder;
        private readonly ILookupSeeder<Genre, GenreJsonDto> _genreSeeder;
        private readonly ILookupSeeder<Label, LabelJsonDto> _labelSeeder;
        private readonly ILookupSeeder<Artist, ArtistJsonDto> _artistSeeder;
        private readonly ILookupSeeder<Packaging, PackagingJsonDto> _packagingSeeder;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDiscogsService _discogsService;
        private readonly IMusicReleaseCommandService _commandService;
        private readonly ILogger<DataSeedingOrchestrator> _logger;
        private readonly string _imagesPath;

        public DataSeedingOrchestrator(
            ILookupSeeder<Country, CountryJsonDto> countrySeeder,
            ILookupSeeder<Store, StoreJsonDto> storeSeeder,
            ILookupSeeder<Format, FormatJsonDto> formatSeeder,
            ILookupSeeder<Genre, GenreJsonDto> genreSeeder,
            ILookupSeeder<Label, LabelJsonDto> labelSeeder,
            ILookupSeeder<Artist, ArtistJsonDto> artistSeeder,
            ILookupSeeder<Packaging, PackagingJsonDto> packagingSeeder,
            IUnitOfWork unitOfWork,
            IDiscogsService discogsService,
            IMusicReleaseCommandService commandService,
            IConfiguration configuration,
            ILogger<DataSeedingOrchestrator> logger)
        {
            _countrySeeder = countrySeeder ?? throw new ArgumentNullException(nameof(countrySeeder));
            _storeSeeder = storeSeeder ?? throw new ArgumentNullException(nameof(storeSeeder));
            _formatSeeder = formatSeeder ?? throw new ArgumentNullException(nameof(formatSeeder));
            _genreSeeder = genreSeeder ?? throw new ArgumentNullException(nameof(genreSeeder));
            _labelSeeder = labelSeeder ?? throw new ArgumentNullException(nameof(labelSeeder));
            _artistSeeder = artistSeeder ?? throw new ArgumentNullException(nameof(artistSeeder));
            _packagingSeeder = packagingSeeder ?? throw new ArgumentNullException(nameof(packagingSeeder));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _discogsService = discogsService ?? throw new ArgumentNullException(nameof(discogsService));
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imagesPath = configuration["ImagesPath"] ?? "/home/andy/music-images";
        }

        public async Task<int> SeedAllLookupDataAsync()
        {
            _logger.LogInformation("Starting lookup data seeding");

            try
            {
                var totalSeeded = 0;

                // Seed in order - some tables may have dependencies
                totalSeeded += await _countrySeeder.SeedAsync();
                totalSeeded += await _storeSeeder.SeedAsync();
                totalSeeded += await _formatSeeder.SeedAsync();
                totalSeeded += await _genreSeeder.SeedAsync();
                totalSeeded += await _labelSeeder.SeedAsync();
                totalSeeded += await _artistSeeder.SeedAsync();
                totalSeeded += await _packagingSeeder.SeedAsync();

                _logger.LogInformation("Lookup data seeding completed successfully. Total records seeded: {TotalSeeded}", totalSeeded);
                return totalSeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during lookup data seeding");
                throw;
            }
        }

        public async Task ClearDatabaseAsync()
        {
            _logger.LogInformation("Clearing database of all music releases");
            
            // Delete all music releases
            // Note: This assumes cascade delete is configured or we handle dependencies manually
            // For now, we'll just delete MusicReleases. Lookup tables will remain but that's fine/desired.
            var allReleases = await _unitOfWork.MusicReleases.GetAllAsync();
            _unitOfWork.MusicReleases.DeleteRange(allReleases);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Database cleared");
        }

        public async Task<int> SeedFromDiscogsAsync(int count)
        {
            _logger.LogInformation("Starting random seed from Discogs. Target: {Count} releases", count);
            
            int seededCount = 0;
            var random = new Random();
            
            // Ensure covers directory exists
            var coversPath = Path.Combine(_imagesPath, "covers");
            if (!Directory.Exists(coversPath))
            {
                Directory.CreateDirectory(coversPath);
            }

            while (seededCount < count)
            {
                try
                {
                    // 1. Search for random releases
                    // Strategy: Random year between 1980 and 1989, Heavy Metal
                    var year = random.Next(1980, 1990);
                    var genre = "Rock";
                    // var style = "Heavy Metal"; // Commented out to avoid potential 401 on strict search
                    
                    _logger.LogInformation("Searching Discogs for Year: {Year}, Genre: {Genre}, Query: Heavy Metal ...", year, genre);
                    
                    var commonWords = new[] { "love", "life", "time", "world", "night", "heart", "soul", "blue", "black", "white", "metal", "steel", "power", "blood", "fire" };
                    var randomWord = commonWords[random.Next(commonWords.Length)];
                    var query = $"Heavy Metal {randomWord}";
                    
                    // Search using query string for "Heavy Metal" instead of style parameter to potentially avoid auth issues
                    var searchResults = await _discogsService.SearchGenericAsync(query, "release", genre, null, null, year, null);
                    
                    if (searchResults == null || !searchResults.Any())
                    {
                        _logger.LogWarning("No results found for query (or rate limit hit). Waiting 10s before retrying...");
                        await Task.Delay(10000); // Backoff for rate limit or empty results
                        continue;
                    }
                    
                    // Pick a random result from the page
                    var randomResult = searchResults[random.Next(searchResults.Count)];
                    
                    // 2. Get full details
                    var details = await _discogsService.GetReleaseDetailsAsync(randomResult.Id);
                    
                    if (details == null)
                    {
                        _logger.LogWarning("Failed to get details for release {Id}. Waiting 10s...", randomResult.Id);
                        await Task.Delay(10000);
                        continue;
                    }
                    
                    // 3. Download Cover Image
                    string? coverImageFilename = null;
                    var imageUrl = details.Images?.FirstOrDefault(i => i.Type == "primary")?.Uri ?? details.Images?.FirstOrDefault()?.Uri;

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        try
                        {
                            using var httpClient = new HttpClient();
                            // Discogs requires User-Agent
                            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("KollectorScum/1.0");
                            
                            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                            var extension = Path.GetExtension(imageUrl);
                            if (string.IsNullOrEmpty(extension) || extension.Length > 5) extension = ".jpg"; // Fallback

                            // Sanitize filename
                            var safeTitle = string.Join("_", details.Title.Split(Path.GetInvalidFileNameChars()));
                            var filename = $"{safeTitle}_{details.Id}{extension}";
                            var filePath = Path.Combine(coversPath, filename);
                            
                            await File.WriteAllBytesAsync(filePath, imageBytes);
                            coverImageFilename = filename;
                            _logger.LogInformation("Downloaded cover image: {Filename}", filename);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to download cover image for {Title}", details.Title);
                            // Continue without image
                        }
                    }

                    // 4. Map to CreateMusicReleaseDto
                    DateTime releaseDate;
                    if (!string.IsNullOrEmpty(details.ReleasedDate) && DateTime.TryParse(details.ReleasedDate, out var parsedDate))
                    {
                        releaseDate = parsedDate.ToUniversalTime();
                    }
                    else
                    {
                        releaseDate = new DateTime(year, 1, 1).ToUniversalTime();
                    }

                    var createDto = new CreateMusicReleaseDto
                    {
                        Title = details.Title,
                        ReleaseYear = releaseDate,
                        OrigReleaseYear = new DateTime(year, 1, 1).ToUniversalTime(), // Approximation
                        ArtistNames = details.Artists?.Select(a => a.Name).ToList() ?? new List<string> { "Unknown" },
                        GenreNames = details.Genres ?? new List<string>(),
                        LabelName = details.Labels?.FirstOrDefault()?.Name ?? "Unknown",
                        CountryName = details.Country ?? "Unknown",
                        FormatName = details.Formats?.FirstOrDefault()?.Name ?? "Unknown",
                        LabelNumber = details.Labels?.FirstOrDefault()?.CatalogNumber,
                        Images = coverImageFilename != null ? new MusicReleaseImageDto { CoverFront = coverImageFilename, Thumbnail = coverImageFilename } : null
                    };
                    
                    // 5. Create Release
                    var result = await _commandService.CreateMusicReleaseAsync(createDto);
                    
                    if (result.IsSuccess)
                    {
                        seededCount++;
                        _logger.LogInformation("Seeded release {Count}/{Total}: {Title}", seededCount, count, createDto.Title);
                    }
                    else
                    {
                        _logger.LogError("Failed to create release: {Error}", result.ErrorMessage);
                    }
                    
                    // Rate limit
                    await Task.Delay(3000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error seeding random release");
                    await Task.Delay(3000);
                }
            }
            
            return seededCount;
        }
    }
}
