using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Models.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text.Json;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for music release business logic
    /// </summary>
    public class MusicReleaseService : IMusicReleaseService
    {
        private readonly IRepository<MusicRelease> _musicReleaseRepository;
        private readonly IRepository<Artist> _artistRepository;
        private readonly IRepository<Genre> _genreRepository;
        private readonly IRepository<Label> _labelRepository;
        private readonly IRepository<Country> _countryRepository;
        private readonly IRepository<Format> _formatRepository;
        private readonly IRepository<Packaging> _packagingRepository;
        private readonly IRepository<Store> _storeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MusicReleaseService> _logger;

        public MusicReleaseService(
            IRepository<MusicRelease> musicReleaseRepository,
            IRepository<Artist> artistRepository,
            IRepository<Genre> genreRepository,
            IRepository<Label> labelRepository,
            IRepository<Country> countryRepository,
            IRepository<Format> formatRepository,
            IRepository<Packaging> packagingRepository,
            IRepository<Store> storeRepository,
            IUnitOfWork unitOfWork,
            ILogger<MusicReleaseService> logger)
        {
            _musicReleaseRepository = musicReleaseRepository;
            _artistRepository = artistRepository;
            _genreRepository = genreRepository;
            _labelRepository = labelRepository;
            _countryRepository = countryRepository;
            _formatRepository = formatRepository;
            _packagingRepository = packagingRepository;
            _storeRepository = storeRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PagedResult<MusicReleaseSummaryDto>> GetMusicReleasesAsync(
            string? search, int? artistId, int? genreId, int? labelId, 
            int? countryId, int? formatId, bool? live, int? yearFrom, 
            int? yearTo, int page, int pageSize)
        {
            _logger.LogInformation("Getting music releases - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            Expression<Func<MusicRelease, bool>>? filter = null;

            if (!string.IsNullOrEmpty(search) || artistId.HasValue || genreId.HasValue || 
                labelId.HasValue || countryId.HasValue || formatId.HasValue || live.HasValue ||
                yearFrom.HasValue || yearTo.HasValue)
            {
                filter = mr => 
                    (string.IsNullOrEmpty(search) || mr.Title.ToLower().Contains(search.ToLower())) &&
                    (!artistId.HasValue || (mr.Artists != null && mr.Artists.Contains(artistId.Value.ToString()))) &&
                    (!genreId.HasValue || (mr.Genres != null && mr.Genres.Contains(genreId.Value.ToString()))) &&
                    (!labelId.HasValue || mr.LabelId == labelId.Value) &&
                    (!countryId.HasValue || mr.CountryId == countryId.Value) &&
                    (!formatId.HasValue || mr.FormatId == formatId.Value) &&
                    (!live.HasValue || mr.Live == live.Value) &&
                    (!yearFrom.HasValue || (mr.ReleaseYear.HasValue && mr.ReleaseYear.Value.Year >= yearFrom.Value)) &&
                    (!yearTo.HasValue || (mr.ReleaseYear.HasValue && mr.ReleaseYear.Value.Year <= yearTo.Value));
            }

            var pagedResult = await _musicReleaseRepository.GetPagedAsync(
                page,
                pageSize,
                filter,
                mr => mr.OrderBy(x => x.Title),
                "Label,Country,Format"
            );

            var summaryDtos = await Task.Run(() => pagedResult.Items.Select(MapToSummaryDto).ToList());

            return new PagedResult<MusicReleaseSummaryDto>
            {
                Items = summaryDtos,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount,
                TotalPages = pagedResult.TotalPages
            };
        }

        public async Task<MusicReleaseDto?> GetMusicReleaseAsync(int id)
        {
            _logger.LogInformation("Getting music release by ID: {Id}", id);

            var musicRelease = await _musicReleaseRepository.GetByIdAsync(id, "Label,Country,Format,Packaging");

            if (musicRelease == null)
            {
                _logger.LogWarning("Music release not found: {Id}", id);
                return null;
            }

            return await MapToFullDto(musicRelease);
        }

        public async Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return new List<SearchSuggestionDto>();
            }

            _logger.LogInformation("Getting search suggestions for query: {Query}", query);

            var queryLower = query.ToLower();
            var suggestions = new List<SearchSuggestionDto>();

            // Get release title suggestions
            var releases = await _musicReleaseRepository.GetAsync(
                mr => mr.Title.ToLower().Contains(queryLower),
                mr => mr.OrderBy(x => x.Title)
            );

            suggestions.AddRange(releases.Take(limit).Select(r => new SearchSuggestionDto
            {
                Type = "release",
                Id = r.Id,
                Name = r.Title,
                Subtitle = r.ReleaseYear?.Year.ToString()
            }));

            // Get artist suggestions
            var artists = await _artistRepository.GetAsync(
                a => a.Name.ToLower().Contains(queryLower),
                a => a.OrderBy(x => x.Name)
            );

            suggestions.AddRange(artists.Take(limit).Select(a => new SearchSuggestionDto
            {
                Type = "artist",
                Id = a.Id,
                Name = a.Name
            }));

            // Get label suggestions
            var labels = await _labelRepository.GetAsync(
                l => l.Name.ToLower().Contains(queryLower),
                l => l.OrderBy(x => x.Name)
            );

            suggestions.AddRange(labels.Take(limit).Select(l => new SearchSuggestionDto
            {
                Type = "label",
                Id = l.Id,
                Name = l.Name
            }));

            return suggestions
                .OrderBy(s => !s.Name.ToLower().StartsWith(queryLower))
                .ThenBy(s => s.Name)
                .Take(limit)
                .ToList();
        }

        public async Task<CollectionStatisticsDto> GetCollectionStatisticsAsync()
        {
            _logger.LogInformation("Getting collection statistics");

            var statistics = new CollectionStatisticsDto();
            var allReleases = await _musicReleaseRepository.GetAllAsync();
            var releasesList = allReleases.ToList();

            statistics.TotalReleases = releasesList.Count;
            
            // Count unique artists
            var uniqueArtistIds = new HashSet<int>();
            foreach (var release in releasesList.Where(r => !string.IsNullOrEmpty(r.Artists)))
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
            statistics.TotalArtists = uniqueArtistIds.Count;

            // Count unique genres
            var uniqueGenreIds = new HashSet<int>();
            foreach (var release in releasesList.Where(r => !string.IsNullOrEmpty(r.Genres)))
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
            statistics.TotalGenres = uniqueGenreIds.Count;

            // Count unique labels
            statistics.TotalLabels = releasesList.Where(r => r.LabelId.HasValue).Select(r => r.LabelId).Distinct().Count();

            // Releases by year
            var releasesByYear = releasesList
                .Where(r => r.ReleaseYear.HasValue)
                .GroupBy(r => r.ReleaseYear!.Value.Year)
                .Select(g => new YearStatisticDto
                {
                    Year = g.Key,
                    Count = g.Count()
                })
                .OrderBy(y => y.Year)
                .ToList();
            statistics.ReleasesByYear = releasesByYear;

            // Releases by format
            var releasesByFormat = releasesList
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

            statistics.ReleasesByFormat = releasesByFormat
                .Select(f => new FormatStatisticDto
                {
                    FormatId = f.FormatId,
                    FormatName = formatDict.ContainsKey(f.FormatId) ? formatDict[f.FormatId] : "Unknown",
                    Count = f.Count,
                    Percentage = statistics.TotalReleases > 0 ? Math.Round((decimal)f.Count / statistics.TotalReleases * 100, 2) : 0
                })
                .OrderByDescending(f => f.Count)
                .ToList();

            // Releases by country
            var releasesByCountry = releasesList
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

            statistics.ReleasesByCountry = releasesByCountry
                .Select(c => new CountryStatisticDto
                {
                    CountryId = c.CountryId,
                    CountryName = countryDict.ContainsKey(c.CountryId) ? countryDict[c.CountryId] : "Unknown",
                    Count = c.Count,
                    Percentage = statistics.TotalReleases > 0 ? Math.Round((decimal)c.Count / statistics.TotalReleases * 100, 2) : 0
                })
                .OrderByDescending(c => c.Count)
                .Take(10)
                .ToList();

            // Releases by genre
            var genreCountMap = new Dictionary<int, int>();
            foreach (var release in releasesList.Where(r => !string.IsNullOrEmpty(r.Genres)))
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

            statistics.ReleasesByGenre = genreCountMap
                .Select(kvp => new GenreStatisticDto
                {
                    GenreId = kvp.Key,
                    GenreName = genreDict.ContainsKey(kvp.Key) ? genreDict[kvp.Key] : "Unknown",
                    Count = kvp.Value,
                    Percentage = statistics.TotalReleases > 0 ? Math.Round((decimal)kvp.Value / statistics.TotalReleases * 100, 2) : 0
                })
                .OrderByDescending(g => g.Count)
                .Take(15)
                .ToList();

            // Calculate collection value
            var releasesWithPurchaseInfo = new List<(MusicRelease release, decimal price)>();
            foreach (var release in releasesList.Where(r => !string.IsNullOrEmpty(r.PurchaseInfo)))
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
                statistics.MostExpensiveRelease = MapToSummaryDto(mostExpensive.release);
            }

            // Recently added releases
            var recentReleases = releasesList
                .OrderByDescending(r => r.DateAdded)
                .Take(10)
                .Select(MapToSummaryDto)
                .ToList();
            statistics.RecentlyAdded = recentReleases;

            return statistics;
        }

        public async Task<CreateMusicReleaseResponseDto> CreateMusicReleaseAsync(CreateMusicReleaseDto createDto)
        {
            _logger.LogInformation("Creating music release: {Title}", createDto.Title);

            var createdEntities = new CreatedEntitiesDto();
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Resolve or create related entities
                var resolvedArtistIds = await ResolveOrCreateArtists(createDto.ArtistIds, createDto.ArtistNames, createdEntities);
                var resolvedGenreIds = await ResolveOrCreateGenres(createDto.GenreIds, createDto.GenreNames, createdEntities);
                var resolvedLabelId = await ResolveOrCreateLabel(createDto.LabelId, createDto.LabelName, createdEntities);
                var resolvedCountryId = await ResolveOrCreateCountry(createDto.CountryId, createDto.CountryName, createdEntities);
                var resolvedFormatId = await ResolveOrCreateFormat(createDto.FormatId, createDto.FormatName, createdEntities);
                var resolvedPackagingId = await ResolveOrCreatePackaging(createDto.PackagingId, createDto.PackagingName, createdEntities);

                // Check for duplicates
                var duplicates = await CheckForDuplicates(createDto.Title, createDto.LabelNumber, resolvedArtistIds);
                if (duplicates.Any())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new InvalidOperationException($"Potential duplicate release found. Similar release(s) exist: {string.Join(", ", duplicates.Select(d => $"'{d.Title}' (ID: {d.Id})"))}");
                }

                // Create the music release
                var musicRelease = new MusicRelease
                {
                    Title = createDto.Title,
                    ReleaseYear = createDto.ReleaseYear,
                    OrigReleaseYear = createDto.OrigReleaseYear,
                    Artists = resolvedArtistIds != null ? JsonSerializer.Serialize(resolvedArtistIds) : null,
                    Genres = resolvedGenreIds != null ? JsonSerializer.Serialize(resolvedGenreIds) : null,
                    Live = createDto.Live,
                    LabelId = resolvedLabelId,
                    CountryId = resolvedCountryId,
                    LabelNumber = createDto.LabelNumber,
                    Upc = createDto.Upc,
                    LengthInSeconds = createDto.LengthInSeconds,
                    FormatId = resolvedFormatId,
                    PackagingId = resolvedPackagingId,
                    PurchaseInfo = createDto.PurchaseInfo != null ? JsonSerializer.Serialize(createDto.PurchaseInfo) : null,
                    Images = createDto.Images != null ? JsonSerializer.Serialize(createDto.Images) : null,
                    Links = createDto.Links != null ? JsonSerializer.Serialize(createDto.Links) : null,
                    Media = createDto.Media != null ? JsonSerializer.Serialize(createDto.Media) : null,
                    DateAdded = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                await _musicReleaseRepository.AddAsync(musicRelease);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var createdDto = await MapToFullDto(musicRelease);
                return new CreateMusicReleaseResponseDto
                {
                    Release = createdDto,
                    Created = HasCreatedEntities(createdEntities) ? createdEntities : null
                };
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<MusicReleaseDto?> UpdateMusicReleaseAsync(int id, UpdateMusicReleaseDto updateDto)
        {
            _logger.LogInformation("Updating music release: {Id}", id);

            var existingMusicRelease = await _musicReleaseRepository.GetByIdAsync(id);
            if (existingMusicRelease == null)
            {
                _logger.LogWarning("Music release not found: {Id}", id);
                return null;
            }

            // Update properties
            existingMusicRelease.Title = updateDto.Title;
            existingMusicRelease.ReleaseYear = updateDto.ReleaseYear;
            existingMusicRelease.OrigReleaseYear = updateDto.OrigReleaseYear;
            existingMusicRelease.Artists = updateDto.ArtistIds != null ? JsonSerializer.Serialize(updateDto.ArtistIds) : null;
            existingMusicRelease.Genres = updateDto.GenreIds != null ? JsonSerializer.Serialize(updateDto.GenreIds) : null;
            existingMusicRelease.Live = updateDto.Live;
            existingMusicRelease.LabelId = updateDto.LabelId;
            existingMusicRelease.CountryId = updateDto.CountryId;
            existingMusicRelease.LabelNumber = updateDto.LabelNumber;
            existingMusicRelease.LengthInSeconds = updateDto.LengthInSeconds;
            existingMusicRelease.FormatId = updateDto.FormatId;
            existingMusicRelease.PackagingId = updateDto.PackagingId;
            existingMusicRelease.PurchaseInfo = updateDto.PurchaseInfo != null ? JsonSerializer.Serialize(updateDto.PurchaseInfo) : null;
            existingMusicRelease.Images = updateDto.Images != null ? JsonSerializer.Serialize(updateDto.Images) : null;
            existingMusicRelease.Links = updateDto.Links != null ? JsonSerializer.Serialize(updateDto.Links) : null;
            existingMusicRelease.Media = updateDto.Media != null ? JsonSerializer.Serialize(updateDto.Media) : null;
            existingMusicRelease.LastModified = DateTime.UtcNow;

            _musicReleaseRepository.Update(existingMusicRelease);
            await _unitOfWork.SaveChangesAsync();

            return await MapToFullDto(existingMusicRelease);
        }

        public async Task<bool> DeleteMusicReleaseAsync(int id)
        {
            _logger.LogInformation("Deleting music release: {Id}", id);

            var musicRelease = await _musicReleaseRepository.GetByIdAsync(id);
            if (musicRelease == null)
            {
                _logger.LogWarning("Music release not found: {Id}", id);
                return false;
            }

            _musicReleaseRepository.Delete(musicRelease);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Music release deleted successfully: {Id}", id);
            return true;
        }

        // Private helper methods

        private async Task<List<MusicRelease>> CheckForDuplicates(string title, string? labelNumber, List<int>? artistIds)
        {
            var duplicates = new List<MusicRelease>();

            if (!string.IsNullOrWhiteSpace(labelNumber))
            {
                var normalizedCatalog = labelNumber.Trim().ToLower();
                var catalogMatches = await _musicReleaseRepository.GetAsync(
                    filter: r => r.LabelNumber != null && r.LabelNumber.ToLower() == normalizedCatalog);
                duplicates.AddRange(catalogMatches);
            }

            if (!duplicates.Any() && artistIds != null && artistIds.Any())
            {
                var normalizedTitle = title.Trim().ToLower();
                var allReleases = await _musicReleaseRepository.GetAllAsync();
                var titleArtistMatches = allReleases.Where(r =>
                {
                    if (r.Title.Trim().ToLower() != normalizedTitle)
                        return false;

                    if (string.IsNullOrEmpty(r.Artists))
                        return false;

                    try
                    {
                        var releaseArtistIds = JsonSerializer.Deserialize<List<int>>(r.Artists);
                        return releaseArtistIds != null && releaseArtistIds.Intersect(artistIds).Any();
                    }
                    catch
                    {
                        return false;
                    }
                });

                duplicates.AddRange(titleArtistMatches);
            }

            return duplicates.Distinct().ToList();
        }

        private async Task<List<int>?> ResolveOrCreateArtists(
            List<int>? artistIds, 
            List<string>? artistNames, 
            CreatedEntitiesDto createdEntities)
        {
            var resolvedIds = new List<int>();

            if (artistIds != null)
            {
                resolvedIds.AddRange(artistIds);
            }

            if (artistNames != null && artistNames.Any())
            {
                foreach (var name in artistNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    var trimmedName = name.Trim();
                    var existing = await _artistRepository.GetFirstOrDefaultAsync(
                        a => a.Name.ToLower() == trimmedName.ToLower());

                    if (existing != null)
                    {
                        resolvedIds.Add(existing.Id);
                    }
                    else
                    {
                        var newArtist = new Artist { Name = trimmedName };
                        await _artistRepository.AddAsync(newArtist);
                        await _unitOfWork.SaveChangesAsync();
                        
                        createdEntities.Artists ??= new List<ArtistDto>();
                        createdEntities.Artists.Add(new ArtistDto { Id = newArtist.Id, Name = newArtist.Name });
                        
                        resolvedIds.Add(newArtist.Id);
                        _logger.LogInformation("Created new artist: {Name} (ID: {Id})", newArtist.Name, newArtist.Id);
                    }
                }
            }

            return resolvedIds.Any() ? resolvedIds : null;
        }

        private async Task<List<int>?> ResolveOrCreateGenres(
            List<int>? genreIds, 
            List<string>? genreNames, 
            CreatedEntitiesDto createdEntities)
        {
            var resolvedIds = new List<int>();

            if (genreIds != null)
            {
                resolvedIds.AddRange(genreIds);
            }

            if (genreNames != null && genreNames.Any())
            {
                foreach (var name in genreNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    var trimmedName = name.Trim();
                    var existing = await _genreRepository.GetFirstOrDefaultAsync(
                        g => g.Name.ToLower() == trimmedName.ToLower());

                    if (existing != null)
                    {
                        resolvedIds.Add(existing.Id);
                    }
                    else
                    {
                        var newGenre = new Genre { Name = trimmedName };
                        await _genreRepository.AddAsync(newGenre);
                        await _unitOfWork.SaveChangesAsync();
                        
                        createdEntities.Genres ??= new List<GenreDto>();
                        createdEntities.Genres.Add(new GenreDto { Id = newGenre.Id, Name = newGenre.Name });
                        
                        resolvedIds.Add(newGenre.Id);
                        _logger.LogInformation("Created new genre: {Name} (ID: {Id})", newGenre.Name, newGenre.Id);
                    }
                }
            }

            return resolvedIds.Any() ? resolvedIds : null;
        }

        private async Task<int?> ResolveOrCreateLabel(int? labelId, string? labelName, CreatedEntitiesDto createdEntities)
        {
            if (labelId.HasValue)
                return labelId;

            if (!string.IsNullOrWhiteSpace(labelName))
            {
                var trimmedName = labelName.Trim();
                var existing = await _labelRepository.GetFirstOrDefaultAsync(
                    l => l.Name.ToLower() == trimmedName.ToLower());

                if (existing != null)
                {
                    return existing.Id;
                }
                else
                {
                    var newLabel = new Label { Name = trimmedName };
                    await _labelRepository.AddAsync(newLabel);
                    await _unitOfWork.SaveChangesAsync();
                    
                    createdEntities.Labels ??= new List<LabelDto>();
                    createdEntities.Labels.Add(new LabelDto { Id = newLabel.Id, Name = newLabel.Name });
                    
                    _logger.LogInformation("Created new label: {Name} (ID: {Id})", newLabel.Name, newLabel.Id);
                    return newLabel.Id;
                }
            }

            return null;
        }

        private async Task<int?> ResolveOrCreateCountry(int? countryId, string? countryName, CreatedEntitiesDto createdEntities)
        {
            if (countryId.HasValue)
                return countryId;

            if (!string.IsNullOrWhiteSpace(countryName))
            {
                var trimmedName = countryName.Trim();
                var existing = await _countryRepository.GetFirstOrDefaultAsync(
                    c => c.Name.ToLower() == trimmedName.ToLower());

                if (existing != null)
                {
                    return existing.Id;
                }
                else
                {
                    var newCountry = new Country { Name = trimmedName };
                    await _countryRepository.AddAsync(newCountry);
                    await _unitOfWork.SaveChangesAsync();
                    
                    createdEntities.Countries ??= new List<CountryDto>();
                    createdEntities.Countries.Add(new CountryDto { Id = newCountry.Id, Name = newCountry.Name });
                    
                    _logger.LogInformation("Created new country: {Name} (ID: {Id})", newCountry.Name, newCountry.Id);
                    return newCountry.Id;
                }
            }

            return null;
        }

        private async Task<int?> ResolveOrCreateFormat(int? formatId, string? formatName, CreatedEntitiesDto createdEntities)
        {
            if (formatId.HasValue)
                return formatId;

            if (!string.IsNullOrWhiteSpace(formatName))
            {
                var trimmedName = formatName.Trim();
                var existing = await _formatRepository.GetFirstOrDefaultAsync(
                    f => f.Name.ToLower() == trimmedName.ToLower());

                if (existing != null)
                {
                    return existing.Id;
                }
                else
                {
                    var newFormat = new Format { Name = trimmedName };
                    await _formatRepository.AddAsync(newFormat);
                    await _unitOfWork.SaveChangesAsync();
                    
                    createdEntities.Formats ??= new List<FormatDto>();
                    createdEntities.Formats.Add(new FormatDto { Id = newFormat.Id, Name = newFormat.Name });
                    
                    _logger.LogInformation("Created new format: {Name} (ID: {Id})", newFormat.Name, newFormat.Id);
                    return newFormat.Id;
                }
            }

            return null;
        }

        private async Task<int?> ResolveOrCreatePackaging(int? packagingId, string? packagingName, CreatedEntitiesDto createdEntities)
        {
            if (packagingId.HasValue)
                return packagingId;

            if (!string.IsNullOrWhiteSpace(packagingName))
            {
                var trimmedName = packagingName.Trim();
                var existing = await _packagingRepository.GetFirstOrDefaultAsync(
                    p => p.Name.ToLower() == trimmedName.ToLower());

                if (existing != null)
                {
                    return existing.Id;
                }
                else
                {
                    var newPackaging = new Packaging { Name = trimmedName };
                    await _packagingRepository.AddAsync(newPackaging);
                    await _unitOfWork.SaveChangesAsync();
                    
                    createdEntities.Packagings ??= new List<PackagingDto>();
                    createdEntities.Packagings.Add(new PackagingDto { Id = newPackaging.Id, Name = newPackaging.Name });
                    
                    _logger.LogInformation("Created new packaging: {Name} (ID: {Id})", newPackaging.Name, newPackaging.Id);
                    return newPackaging.Id;
                }
            }

            return null;
        }

        private bool HasCreatedEntities(CreatedEntitiesDto createdEntities)
        {
            return (createdEntities.Artists?.Any() == true) ||
                   (createdEntities.Labels?.Any() == true) ||
                   (createdEntities.Genres?.Any() == true) ||
                   (createdEntities.Countries?.Any() == true) ||
                   (createdEntities.Formats?.Any() == true) ||
                   (createdEntities.Packagings?.Any() == true) ||
                   (createdEntities.Stores?.Any() == true);
        }

        private MusicReleaseSummaryDto MapToSummaryDto(MusicRelease musicRelease)
        {
            var artistIds = string.IsNullOrEmpty(musicRelease.Artists) 
                ? null 
                : JsonSerializer.Deserialize<List<int>>(musicRelease.Artists);
            
            var genreIds = string.IsNullOrEmpty(musicRelease.Genres) 
                ? null 
                : JsonSerializer.Deserialize<List<int>>(musicRelease.Genres);

            var images = string.IsNullOrEmpty(musicRelease.Images) 
                ? null 
                : JsonSerializer.Deserialize<MusicReleaseImageDto>(musicRelease.Images);

            return new MusicReleaseSummaryDto
            {
                Id = musicRelease.Id,
                Title = musicRelease.Title,
                ReleaseYear = musicRelease.ReleaseYear,
                ArtistNames = artistIds?.Select(id => GetArtistName(id)).ToList(),
                GenreNames = genreIds?.Select(id => GetGenreName(id)).ToList(),
                LabelName = musicRelease.Label?.Name,
                FormatName = musicRelease.Format?.Name,
                CountryName = musicRelease.Country?.Name,
                CoverImageUrl = images?.CoverFront ?? images?.Thumbnail,
                DateAdded = musicRelease.DateAdded
            };
        }

        private async Task<MusicReleaseDto> MapToFullDto(MusicRelease musicRelease)
        {
            var artistIds = string.IsNullOrEmpty(musicRelease.Artists) 
                ? null 
                : JsonSerializer.Deserialize<List<int>>(musicRelease.Artists);
            
            var genreIds = string.IsNullOrEmpty(musicRelease.Genres) 
                ? null 
                : JsonSerializer.Deserialize<List<int>>(musicRelease.Genres);

            List<ArtistDto>? artists = null;
            if (artistIds != null)
            {
                artists = new List<ArtistDto>();
                foreach (var id in artistIds)
                {
                    var artist = await _artistRepository.GetByIdAsync(id);
                    if (artist != null)
                        artists.Add(new ArtistDto { Id = artist.Id, Name = artist.Name });
                }
            }

            List<GenreDto>? genres = null;
            if (genreIds != null)
            {
                genres = new List<GenreDto>();
                foreach (var id in genreIds)
                {
                    var genre = await _genreRepository.GetByIdAsync(id);
                    if (genre != null)
                        genres.Add(new GenreDto { Id = genre.Id, Name = genre.Name });
                }
            }

            return new MusicReleaseDto
            {
                Id = musicRelease.Id,
                Title = musicRelease.Title,
                ReleaseYear = musicRelease.ReleaseYear,
                OrigReleaseYear = musicRelease.OrigReleaseYear,
                Artists = artists,
                Genres = genres,
                Live = musicRelease.Live,
                Label = musicRelease.Label != null ? new LabelDto { Id = musicRelease.Label.Id, Name = musicRelease.Label.Name } : null,
                Country = musicRelease.Country != null ? new CountryDto { Id = musicRelease.Country.Id, Name = musicRelease.Country.Name } : null,
                LabelNumber = musicRelease.LabelNumber,
                LengthInSeconds = musicRelease.LengthInSeconds,
                Format = musicRelease.Format != null ? new FormatDto { Id = musicRelease.Format.Id, Name = musicRelease.Format.Name } : null,
                Packaging = musicRelease.Packaging != null ? new PackagingDto { Id = musicRelease.Packaging.Id, Name = musicRelease.Packaging.Name } : null,
                Upc = musicRelease.Upc,
                PurchaseInfo = await ResolvePurchaseInfo(musicRelease.PurchaseInfo),
                Images = string.IsNullOrEmpty(musicRelease.Images) 
                    ? null 
                    : JsonSerializer.Deserialize<MusicReleaseImageDto>(musicRelease.Images),
                Links = string.IsNullOrEmpty(musicRelease.Links) 
                    ? null 
                    : JsonSerializer.Deserialize<List<MusicReleaseLinkDto>>(musicRelease.Links),
                Media = await ResolveMediaArtists(musicRelease.Media),
                DateAdded = musicRelease.DateAdded,
                LastModified = musicRelease.LastModified
            };
        }

        private string GetArtistName(int id)
        {
            var artist = _artistRepository.GetByIdAsync(id).Result;
            return artist?.Name ?? $"Artist {id}";
        }

        private string GetGenreName(int id)
        {
            var genre = _genreRepository.GetByIdAsync(id).Result;
            return genre?.Name ?? $"Genre {id}";
        }

        private async Task<List<MusicReleaseMediaDto>?> ResolveMediaArtists(string? mediaJson)
        {
            if (string.IsNullOrEmpty(mediaJson))
                return null;

            var mediaList = JsonSerializer.Deserialize<List<MusicReleaseMediaDto>>(mediaJson);
            if (mediaList == null) return null;

            foreach (var media in mediaList)
            {
                if (media.Tracks != null)
                {
                    foreach (var track in media.Tracks)
                    {
                        if (track.Artists != null && track.Artists.Count > 0)
                        {
                            var resolvedArtists = new List<string>();
                            foreach (var artistIdStr in track.Artists)
                            {
                                if (int.TryParse(artistIdStr, out int artistId))
                                {
                                    var artist = await _artistRepository.GetByIdAsync(artistId);
                                    resolvedArtists.Add(artist?.Name ?? artistIdStr);
                                }
                                else
                                {
                                    resolvedArtists.Add(artistIdStr);
                                }
                            }
                            track.Artists = resolvedArtists;
                        }

                        if (track.Genres != null && track.Genres.Count > 0)
                        {
                            var resolvedGenres = new List<string>();
                            foreach (var genreIdStr in track.Genres)
                            {
                                if (int.TryParse(genreIdStr, out int genreId))
                                {
                                    var genre = await _genreRepository.GetByIdAsync(genreId);
                                    resolvedGenres.Add(genre?.Name ?? genreIdStr);
                                }
                                else
                                {
                                    resolvedGenres.Add(genreIdStr);
                                }
                            }
                            track.Genres = resolvedGenres;
                        }
                    }
                }
            }

            return mediaList;
        }

        private async Task<MusicReleasePurchaseInfoDto?> ResolvePurchaseInfo(string? purchaseInfoJson)
        {
            if (string.IsNullOrEmpty(purchaseInfoJson))
                return null;

            try
            {
                var purchaseInfo = JsonSerializer.Deserialize<PurchaseInfo>(purchaseInfoJson);
                if (purchaseInfo == null)
                    return null;

                string? storeName = null;
                if (purchaseInfo.StoreID.HasValue)
                {
                    var store = await _storeRepository.GetByIdAsync(purchaseInfo.StoreID.Value);
                    storeName = store?.Name;
                }

                return new MusicReleasePurchaseInfoDto
                {
                    StoreId = purchaseInfo.StoreID,
                    StoreName = storeName,
                    Price = purchaseInfo.Price,
                    Currency = "GBP",
                    PurchaseDate = purchaseInfo.Date,
                    Notes = purchaseInfo.Notes
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse purchase info JSON: {Json}", purchaseInfoJson);
                return null;
            }
        }
    }
}
