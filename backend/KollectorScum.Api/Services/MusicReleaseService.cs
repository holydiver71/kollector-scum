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
    /// Service for music release business logic - orchestrates CRUD operations
    /// </summary>
    public class MusicReleaseService : IMusicReleaseService
    {
        private readonly IRepository<MusicRelease> _musicReleaseRepository;
        private readonly IRepository<Artist> _artistRepository;
        private readonly IRepository<Label> _labelRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEntityResolverService _entityResolver;
        private readonly IMusicReleaseMapperService _mapper;
        private readonly ICollectionStatisticsService _statisticsService;
        private readonly ILogger<MusicReleaseService> _logger;

        public MusicReleaseService(
            IRepository<MusicRelease> musicReleaseRepository,
            IRepository<Artist> artistRepository,
            IRepository<Label> labelRepository,
            IUnitOfWork unitOfWork,
            IEntityResolverService entityResolver,
            IMusicReleaseMapperService mapper,
            ICollectionStatisticsService statisticsService,
            ILogger<MusicReleaseService> logger)
        {
            _musicReleaseRepository = musicReleaseRepository;
            _artistRepository = artistRepository;
            _labelRepository = labelRepository;
            _unitOfWork = unitOfWork;
            _entityResolver = entityResolver;
            _mapper = mapper;
            _statisticsService = statisticsService;
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

            var summaryDtos = await Task.Run(() => pagedResult.Items.Select(mr => _mapper.MapToSummaryDto(mr)).ToList());

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

            return await _mapper.MapToFullDtoAsync(musicRelease);
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
            return await _statisticsService.GetCollectionStatisticsAsync();
        }

        public async Task<CreateMusicReleaseResponseDto> CreateMusicReleaseAsync(CreateMusicReleaseDto createDto)
        {
            _logger.LogInformation("Creating music release: {Title}", createDto.Title);

            var createdEntities = new CreatedEntitiesDto();
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Resolve or create related entities using the resolver service
                var resolvedArtistIds = await _entityResolver.ResolveOrCreateArtistsAsync(
                    createDto.ArtistIds, createDto.ArtistNames, createdEntities);
                var resolvedGenreIds = await _entityResolver.ResolveOrCreateGenresAsync(
                    createDto.GenreIds, createDto.GenreNames, createdEntities);
                var resolvedLabelId = await _entityResolver.ResolveOrCreateLabelAsync(
                    createDto.LabelId, createDto.LabelName, createdEntities);
                var resolvedCountryId = await _entityResolver.ResolveOrCreateCountryAsync(
                    createDto.CountryId, createDto.CountryName, createdEntities);
                var resolvedFormatId = await _entityResolver.ResolveOrCreateFormatAsync(
                    createDto.FormatId, createDto.FormatName, createdEntities);
                var resolvedPackagingId = await _entityResolver.ResolveOrCreatePackagingAsync(
                    createDto.PackagingId, createDto.PackagingName, createdEntities);

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

                var createdDto = await _mapper.MapToFullDtoAsync(musicRelease);
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

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var existingMusicRelease = await _musicReleaseRepository.GetByIdAsync(id);
                if (existingMusicRelease == null)
                {
                    _logger.LogWarning("Music release not found: {Id}", id);
                    await _unitOfWork.RollbackTransactionAsync();
                    return null;
                }

                // Handle purchase info with potential store creation
                if (updateDto.PurchaseInfo != null)
                {
                    // If StoreName is provided but StoreId is not, try to resolve or create the store
                    if (!string.IsNullOrWhiteSpace(updateDto.PurchaseInfo.StoreName) && !updateDto.PurchaseInfo.StoreId.HasValue)
                    {
                        var storeName = updateDto.PurchaseInfo.StoreName.Trim();
                        _logger.LogInformation("Resolving or creating store: {StoreName}", storeName);

                        // Check if store exists (case-insensitive)
                        var existingStores = await _unitOfWork.Stores.GetAsync(
                            filter: s => s.Name.ToLower() == storeName.ToLower());
                        var existingStore = existingStores.FirstOrDefault();

                        if (existingStore != null)
                        {
                            // Use existing store
                            updateDto.PurchaseInfo.StoreId = existingStore.Id;
                            _logger.LogInformation("Found existing store: {StoreName} (ID: {StoreId})", existingStore.Name, existingStore.Id);
                        }
                        else
                        {
                            // Create new store
                            var newStore = new Store { Name = storeName };
                            await _unitOfWork.Stores.AddAsync(newStore);
                            await _unitOfWork.SaveChangesAsync(); // Save to get the ID
                            updateDto.PurchaseInfo.StoreId = newStore.Id;
                            _logger.LogInformation("Created new store: {StoreName} (ID: {StoreId})", newStore.Name, newStore.Id);
                        }
                    }
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

                await _unitOfWork.CommitTransactionAsync();

                return await _mapper.MapToFullDtoAsync(existingMusicRelease);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating music release: {Id}", id);
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
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

        /// <summary>
        /// Check for potential duplicate releases based on title, catalog number, or artist
        /// </summary>
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

        /// <summary>
        /// Checks if any entities were created during the create operation
        /// </summary>
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
    }
}
