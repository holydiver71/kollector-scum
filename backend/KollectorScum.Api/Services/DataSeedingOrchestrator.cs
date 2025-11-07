using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<DataSeedingOrchestrator> _logger;

        public DataSeedingOrchestrator(
            ILookupSeeder<Country, CountryJsonDto> countrySeeder,
            ILookupSeeder<Store, StoreJsonDto> storeSeeder,
            ILookupSeeder<Format, FormatJsonDto> formatSeeder,
            ILookupSeeder<Genre, GenreJsonDto> genreSeeder,
            ILookupSeeder<Label, LabelJsonDto> labelSeeder,
            ILookupSeeder<Artist, ArtistJsonDto> artistSeeder,
            ILookupSeeder<Packaging, PackagingJsonDto> packagingSeeder,
            ILogger<DataSeedingOrchestrator> logger)
        {
            _countrySeeder = countrySeeder ?? throw new ArgumentNullException(nameof(countrySeeder));
            _storeSeeder = storeSeeder ?? throw new ArgumentNullException(nameof(storeSeeder));
            _formatSeeder = formatSeeder ?? throw new ArgumentNullException(nameof(formatSeeder));
            _genreSeeder = genreSeeder ?? throw new ArgumentNullException(nameof(genreSeeder));
            _labelSeeder = labelSeeder ?? throw new ArgumentNullException(nameof(labelSeeder));
            _artistSeeder = artistSeeder ?? throw new ArgumentNullException(nameof(artistSeeder));
            _packagingSeeder = packagingSeeder ?? throw new ArgumentNullException(nameof(packagingSeeder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    }
}
