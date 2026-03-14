using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Models.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for calculating collection statistics
    /// </summary>
    public class CollectionStatisticsService : ICollectionStatisticsService
    {
        private readonly IRepository<MusicRelease> _musicReleaseRepository;
        private readonly IRepository<Format> _formatRepository;
        private readonly IRepository<Country> _countryRepository;
        private readonly IRepository<Genre> _genreRepository;
        private readonly IMusicReleaseMapperService _mapperService;
        private readonly ILogger<CollectionStatisticsService> _logger;
        private readonly IUserContext _userContext;

        public CollectionStatisticsService(
            IRepository<MusicRelease> musicReleaseRepository,
            IRepository<Format> formatRepository,
            IRepository<Country> countryRepository,
            IRepository<Genre> genreRepository,
            IMusicReleaseMapperService mapperService,
            ILogger<CollectionStatisticsService> logger,
            IUserContext userContext)
        {
            _musicReleaseRepository = musicReleaseRepository;
            _formatRepository = formatRepository;
            _countryRepository = countryRepository;
            _genreRepository = genreRepository;
            _mapperService = mapperService;
            _logger = logger;
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        public async Task<CollectionStatisticsDto> GetCollectionStatisticsAsync()
        {
            _logger.LogInformation("Getting collection statistics");

            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
            {
                return new CollectionStatisticsDto();
            }

            var statistics = new CollectionStatisticsDto();
            var userReleasesQuery = _musicReleaseRepository
                .Query()
                .AsNoTracking()
                .Where(r => r.UserId == userId.Value);

            statistics.TotalReleases = await userReleasesQuery.CountAsync();
            
            var artistJsonList = await userReleasesQuery
                .Where(r => !string.IsNullOrEmpty(r.Artists))
                .Select(r => r.Artists!)
                .ToListAsync();

            var genreJsonList = await userReleasesQuery
                .Where(r => !string.IsNullOrEmpty(r.Genres))
                .Select(r => r.Genres!)
                .ToListAsync();

            var purchaseReleases = await userReleasesQuery
                .Where(r => !string.IsNullOrEmpty(r.PurchaseInfo))
                .ToListAsync();

            var recentReleases = await userReleasesQuery
                .OrderByDescending(r => r.DateAdded)
                .Take(10)
                .ToListAsync();

            // Count unique artists
            statistics.TotalArtists = CountUniqueIds(artistJsonList);

            // Count unique genres
            statistics.TotalGenres = CountUniqueIds(genreJsonList);

            // Count unique labels
            statistics.TotalLabels = await userReleasesQuery
                .Where(r => r.LabelId.HasValue)
                .Select(r => r.LabelId!.Value)
                .Distinct()
                .CountAsync();

            // Releases by year
            statistics.ReleasesByYear = await userReleasesQuery
                .Where(r => r.ReleaseYear.HasValue)
                .GroupBy(r => r.ReleaseYear!.Value.Year)
                .Select(g => new YearStatisticDto
                {
                    Year = g.Key,
                    Count = g.Count()
                })
                .OrderBy(y => y.Year)
                .ToListAsync();

            // Releases by format
            statistics.ReleasesByFormat = await CalculateReleasesByFormatAsync(userReleasesQuery, statistics.TotalReleases);

            // Releases by country
            statistics.ReleasesByCountry = await CalculateReleasesByCountryAsync(userReleasesQuery, statistics.TotalReleases);

            // Releases by genre
            statistics.ReleasesByGenre = await CalculateReleasesByGenreAsync(genreJsonList, statistics.TotalReleases);

            // Calculate collection value
            CalculateCollectionValue(purchaseReleases, statistics);

            // Recently added releases
            statistics.RecentlyAdded = recentReleases
                .Select(r => _mapperService.MapToSummaryDto(r))
                .ToList();

            return statistics;
        }

        private int CountUniqueIds(List<string> serializedIds)
        {
            var uniqueIds = new HashSet<int>();
            foreach (var serializedValue in serializedIds)
            {
                try
                {
                    var ids = JsonSerializer.Deserialize<List<int>>(serializedValue);
                    if (ids != null)
                    {
                        foreach (var id in ids)
                        {
                            uniqueIds.Add(id);
                        }
                    }
                }
                catch { }
            }
            return uniqueIds.Count;
        }

        private async Task<List<FormatStatisticDto>> CalculateReleasesByFormatAsync(
            IQueryable<MusicRelease> userReleasesQuery,
            int totalReleases)
        {
            var releasesByFormat = await userReleasesQuery
                .Where(r => r.FormatId.HasValue)
                .GroupBy(r => r.FormatId!.Value)
                .Select(g => new
                {
                    FormatId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var formats = await _formatRepository.GetAllAsync();
            var formatDict = formats.ToDictionary(f => f.Id, f => f.Name);

            return releasesByFormat
                .Select(f => new FormatStatisticDto
                {
                    FormatId = f.FormatId,
                    FormatName = formatDict.ContainsKey(f.FormatId) ? formatDict[f.FormatId] : "Unknown",
                    Count = f.Count,
                    Percentage = totalReleases > 0 ? Math.Round((decimal)f.Count / totalReleases * 100, 2) : 0
                })
                .OrderByDescending(f => f.Count)
                .ToList();
        }

        private async Task<List<CountryStatisticDto>> CalculateReleasesByCountryAsync(
            IQueryable<MusicRelease> userReleasesQuery,
            int totalReleases)
        {
            var releasesByCountry = await userReleasesQuery
                .Where(r => r.CountryId.HasValue)
                .GroupBy(r => r.CountryId!.Value)
                .Select(g => new
                {
                    CountryId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var countries = await _countryRepository.GetAllAsync();
            var countryDict = countries.ToDictionary(c => c.Id, c => c.Name);

            return releasesByCountry
                .Select(c => new CountryStatisticDto
                {
                    CountryId = c.CountryId,
                    CountryName = countryDict.ContainsKey(c.CountryId) ? countryDict[c.CountryId] : "Unknown",
                    Count = c.Count,
                    Percentage = totalReleases > 0 ? Math.Round((decimal)c.Count / totalReleases * 100, 2) : 0
                })
                .OrderByDescending(c => c.Count)
                .Take(10)
                .ToList();
        }

        private async Task<List<GenreStatisticDto>> CalculateReleasesByGenreAsync(
            List<string> genreJsonList,
            int totalReleases)
        {
            var genreCountMap = new Dictionary<int, int>();
            foreach (var serializedGenres in genreJsonList)
            {
                try
                {
                    var genreIds = JsonSerializer.Deserialize<List<int>>(serializedGenres);
                    if (genreIds != null)
                    {
                        foreach (var genreId in genreIds)
                        {
                            if (!genreCountMap.ContainsKey(genreId))
                                genreCountMap[genreId] = 0;
                            genreCountMap[genreId]++;
                        }
                    }
                }
                catch { }
            }

            var genres = await _genreRepository.GetAllAsync();
            var genreDict = genres.ToDictionary(g => g.Id, g => g.Name);

            return genreCountMap
                .Select(kvp => new GenreStatisticDto
                {
                    GenreId = kvp.Key,
                    GenreName = genreDict.ContainsKey(kvp.Key) ? genreDict[kvp.Key] : "Unknown",
                    Count = kvp.Value,
                    Percentage = totalReleases > 0 ? Math.Round((decimal)kvp.Value / totalReleases * 100, 2) : 0
                })
                .OrderByDescending(g => g.Count)
                .Take(15)
                .ToList();
        }

        private void CalculateCollectionValue(List<MusicRelease> releases, CollectionStatisticsDto statistics)
        {
            var releasesWithPurchaseInfo = new List<(MusicRelease release, decimal price)>();
            foreach (var release in releases)
            {
                try
                {
                    var purchaseInfo = JsonSerializer.Deserialize<PurchaseInfo>(release.PurchaseInfo!);
                    if (purchaseInfo?.Price != null && purchaseInfo.Price > 0)
                    {
                        releasesWithPurchaseInfo.Add((release, purchaseInfo.Price.Value));
                    }
                }
                catch { }
            }

            if (releasesWithPurchaseInfo.Any())
            {
                statistics.TotalValue = releasesWithPurchaseInfo.Sum(r => r.price);
                statistics.AveragePrice = Math.Round(releasesWithPurchaseInfo.Average(r => r.price), 2);

                var mostExpensive = releasesWithPurchaseInfo.OrderByDescending(r => r.price).First();
                statistics.MostExpensiveRelease = _mapperService.MapToSummaryDto(mostExpensive.release);
            }
        }
    }
}
