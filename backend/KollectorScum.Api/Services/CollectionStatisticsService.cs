using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Models.ValueObjects;
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
            var allReleases = await _musicReleaseRepository.GetAsync(r => r.UserId == userId.Value);
            var releasesList = allReleases.ToList();

            statistics.TotalReleases = releasesList.Count;
            
            // Count unique artists
            statistics.TotalArtists = CountUniqueArtists(releasesList);

            // Count unique genres
            statistics.TotalGenres = CountUniqueGenres(releasesList);

            // Count unique labels
            statistics.TotalLabels = releasesList
                .Where(r => r.LabelId.HasValue)
                .Select(r => r.LabelId)
                .Distinct()
                .Count();

            // Releases by year
            statistics.ReleasesByYear = CalculateReleasesByYear(releasesList);

            // Releases by format
            statistics.ReleasesByFormat = await CalculateReleasesByFormatAsync(releasesList, statistics.TotalReleases);

            // Releases by country
            statistics.ReleasesByCountry = await CalculateReleasesByCountryAsync(releasesList, statistics.TotalReleases);

            // Releases by genre
            statistics.ReleasesByGenre = await CalculateReleasesByGenreAsync(releasesList, statistics.TotalReleases);

            // Calculate collection value
            CalculateCollectionValue(releasesList, statistics);

            // Recently added releases
            statistics.RecentlyAdded = releasesList
                .OrderByDescending(r => r.DateAdded)
                .Take(10)
                .Select(r => _mapperService.MapToSummaryDto(r))
                .ToList();

            return statistics;
        }

        private int CountUniqueArtists(List<MusicRelease> releases)
        {
            var uniqueArtistIds = new HashSet<int>();
            foreach (var release in releases.Where(r => !string.IsNullOrEmpty(r.Artists)))
            {
                try
                {
                    var artistIds = JsonSerializer.Deserialize<List<int>>(release.Artists!);
                    if (artistIds != null)
                    {
                        foreach (var id in artistIds)
                        {
                            uniqueArtistIds.Add(id);
                        }
                    }
                }
                catch { }
            }
            return uniqueArtistIds.Count;
        }

        private int CountUniqueGenres(List<MusicRelease> releases)
        {
            var uniqueGenreIds = new HashSet<int>();
            foreach (var release in releases.Where(r => !string.IsNullOrEmpty(r.Genres)))
            {
                try
                {
                    var genreIds = JsonSerializer.Deserialize<List<int>>(release.Genres!);
                    if (genreIds != null)
                    {
                        foreach (var id in genreIds)
                        {
                            uniqueGenreIds.Add(id);
                        }
                    }
                }
                catch { }
            }
            return uniqueGenreIds.Count;
        }

        private List<YearStatisticDto> CalculateReleasesByYear(List<MusicRelease> releases)
        {
            return releases
                .Where(r => r.ReleaseYear.HasValue)
                .GroupBy(r => r.ReleaseYear!.Value.Year)
                .Select(g => new YearStatisticDto
                {
                    Year = g.Key,
                    Count = g.Count()
                })
                .OrderBy(y => y.Year)
                .ToList();
        }

        private async Task<List<FormatStatisticDto>> CalculateReleasesByFormatAsync(
            List<MusicRelease> releases, 
            int totalReleases)
        {
            var releasesByFormat = releases
                .Where(r => r.FormatId.HasValue)
                .GroupBy(r => r.FormatId!.Value)
                .Select(g => new
                {
                    FormatId = g.Key,
                    Count = g.Count()
                })
                .ToList();

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
            List<MusicRelease> releases, 
            int totalReleases)
        {
            var releasesByCountry = releases
                .Where(r => r.CountryId.HasValue)
                .GroupBy(r => r.CountryId!.Value)
                .Select(g => new
                {
                    CountryId = g.Key,
                    Count = g.Count()
                })
                .ToList();

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
            List<MusicRelease> releases, 
            int totalReleases)
        {
            var genreCountMap = new Dictionary<int, int>();
            foreach (var release in releases.Where(r => !string.IsNullOrEmpty(r.Genres)))
            {
                try
                {
                    var genreIds = JsonSerializer.Deserialize<List<int>>(release.Genres!);
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
            foreach (var release in releases.Where(r => !string.IsNullOrEmpty(r.PurchaseInfo)))
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
