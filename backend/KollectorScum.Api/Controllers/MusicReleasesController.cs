using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Models.ValueObjects;
using KollectorScum.Api.DTOs;
using System.Linq.Expressions;
using System.Text.Json;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing music releases
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MusicReleasesController : ControllerBase
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
        private readonly ILogger<MusicReleasesController> _logger;

        public MusicReleasesController(
            IRepository<MusicRelease> musicReleaseRepository,
            IRepository<Artist> artistRepository,
            IRepository<Genre> genreRepository,
            IRepository<Label> labelRepository,
            IRepository<Country> countryRepository,
            IRepository<Format> formatRepository,
            IRepository<Packaging> packagingRepository,
            IRepository<Store> storeRepository,
            IUnitOfWork unitOfWork,
            ILogger<MusicReleasesController> logger)
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

        /// <summary>
        /// Gets a paginated list of music releases
        /// </summary>
        /// <param name="search">Search term to filter by title or artist name</param>
        /// <param name="artistId">Filter by artist ID</param>
        /// <param name="genreId">Filter by genre ID</param>
        /// <param name="labelId">Filter by label ID</param>
        /// <param name="countryId">Filter by country ID</param>
        /// <param name="formatId">Filter by format ID</param>
        /// <param name="live">Filter by live recordings</param>
        /// <param name="yearFrom">Filter by minimum release year (inclusive)</param>
        /// <param name="yearTo">Filter by maximum release year (inclusive)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <returns>Paginated list of music release summaries</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<MusicReleaseSummaryDto>), 200)]
        public async Task<ActionResult<PagedResult<MusicReleaseSummaryDto>>> GetMusicReleases(
            [FromQuery] string? search,
            [FromQuery] int? artistId,
            [FromQuery] int? genreId,
            [FromQuery] int? labelId,
            [FromQuery] int? countryId,
            [FromQuery] int? formatId,
            [FromQuery] bool? live,
            [FromQuery] int? yearFrom,
            [FromQuery] int? yearTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                _logger.LogInformation("Getting music releases - Page: {Page}, PageSize: {PageSize}, Search: {Search}, ArtistId: {ArtistId}, GenreId: {GenreId}, YearFrom: {YearFrom}, YearTo: {YearTo}",
                    page, pageSize, search, artistId, genreId, yearFrom, yearTo);

                // Build filter expression
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

                var result = new PagedResult<MusicReleaseSummaryDto>
                {
                    Items = summaryDtos,
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount,
                    TotalPages = pagedResult.TotalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting music releases");
                return StatusCode(500, "An error occurred while retrieving music releases");
            }
        }

        /// <summary>
        /// Gets a specific music release by ID
        /// </summary>
        /// <param name="id">Music release ID</param>
        /// <returns>Music release details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MusicReleaseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<MusicReleaseDto>> GetMusicRelease(int id)
        {
            try
            {
                _logger.LogInformation("Getting music release by ID: {Id}", id);

                var musicRelease = await _musicReleaseRepository.GetByIdAsync(id, "Label,Country,Format,Packaging");

                if (musicRelease == null)
                {
                    _logger.LogWarning("Music release not found: {Id}", id);
                    return NotFound($"Music release with ID {id} not found");
                }

                var dto = await MapToFullDto(musicRelease);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting music release by ID: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the music release");
            }
        }

        /// <summary>
        /// Gets search suggestions based on a partial search term
        /// </summary>
        /// <param name="query">Partial search term</param>
        /// <param name="limit">Maximum number of suggestions to return (default: 10)</param>
        /// <returns>List of search suggestions</returns>
        [HttpGet("suggestions")]
        [ProducesResponseType(typeof(List<SearchSuggestionDto>), 200)]
        public async Task<ActionResult<List<SearchSuggestionDto>>> GetSearchSuggestions(
            [FromQuery] string query,
            [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Ok(new List<SearchSuggestionDto>());
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

                // Return top suggestions, prioritizing exact matches
                return Ok(suggestions
                    .OrderBy(s => !s.Name.ToLower().StartsWith(queryLower))
                    .ThenBy(s => s.Name)
                    .Take(limit)
                    .ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
                return StatusCode(500, "An error occurred while retrieving search suggestions");
            }
        }

        /// <summary>
        /// Gets comprehensive collection statistics
        /// </summary>
        /// <returns>Collection statistics including counts, distributions, and value metrics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(CollectionStatisticsDto), 200)]
        public async Task<ActionResult<CollectionStatisticsDto>> GetCollectionStatistics()
        {
            try
            {
                _logger.LogInformation("Getting collection statistics");

                var statistics = new CollectionStatisticsDto();

                // Get all releases for processing
                var allReleases = await _musicReleaseRepository.GetAllAsync();
                var releasesList = allReleases.ToList();

                // Basic counts
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
                    catch { /* Skip invalid JSON */ }
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
                    catch { /* Skip invalid JSON */ }
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
                    .Take(10) // Top 10 countries
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
                    catch { /* Skip invalid JSON */ }
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
                    .Take(15) // Top 15 genres
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
                    catch { /* Skip invalid JSON */ }
                }

                if (releasesWithPurchaseInfo.Any())
                {
                    statistics.TotalValue = releasesWithPurchaseInfo.Sum(r => r.price);
                    statistics.AveragePrice = Math.Round(releasesWithPurchaseInfo.Average(r => r.price), 2);

                    // Most expensive release
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

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting collection statistics");
                return StatusCode(500, "An error occurred while retrieving collection statistics");
            }
        }

        /// <summary>
        /// Creates a new music release
        /// Supports auto-creation of new lookup entities (artists, labels, genres, etc.)
        /// </summary>
        /// <param name="createDto">Music release data</param>
        /// <returns>Created music release with details about auto-created entities</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CreateMusicReleaseResponseDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<CreateMusicReleaseResponseDto>> CreateMusicRelease([FromBody] CreateMusicReleaseDto createDto)
        {
            try
            {
                _logger.LogInformation("Creating music release: {Title}", createDto.Title);

                // Track what entities we create
                var createdEntities = new CreatedEntitiesDto();

                // Resolve or create all lookup entities within a transaction
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // Resolve artists (IDs + names)
                    var resolvedArtistIds = await ResolveOrCreateArtists(
                        createDto.ArtistIds, 
                        createDto.ArtistNames, 
                        createdEntities);

                    // Resolve genres (IDs + names)
                    var resolvedGenreIds = await ResolveOrCreateGenres(
                        createDto.GenreIds, 
                        createDto.GenreNames, 
                        createdEntities);

                    // Resolve label (ID or name)
                    var resolvedLabelId = await ResolveOrCreateLabel(
                        createDto.LabelId, 
                        createDto.LabelName, 
                        createdEntities);

                    // Resolve country (ID or name)
                    var resolvedCountryId = await ResolveOrCreateCountry(
                        createDto.CountryId, 
                        createDto.CountryName, 
                        createdEntities);

                    // Resolve format (ID or name)
                    var resolvedFormatId = await ResolveOrCreateFormat(
                        createDto.FormatId, 
                        createDto.FormatName, 
                        createdEntities);

                    // Resolve packaging (ID or name)
                    var resolvedPackagingId = await ResolveOrCreatePackaging(
                        createDto.PackagingId, 
                        createDto.PackagingName, 
                        createdEntities);

                    // Check for duplicates
                    var duplicates = await CheckForDuplicates(createDto.Title, createDto.LabelNumber, resolvedArtistIds);
                    if (duplicates.Any())
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        _logger.LogWarning("Potential duplicate detected for: {Title}", createDto.Title);
                        return BadRequest(new
                        {
                            message = "Potential duplicate release found",
                            duplicates = duplicates.Select(d => new
                            {
                                id = d.Id,
                                title = d.Title,
                                labelNumber = d.LabelNumber,
                                year = d.ReleaseYear
                            })
                        });
                    }

                    // Create the music release with resolved IDs
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

                    _logger.LogInformation("Successfully created music release {Title} with ID {Id}", 
                        musicRelease.Title, musicRelease.Id);

                    // Log what was created
                    if (createdEntities.Artists?.Any() == true)
                        _logger.LogInformation("Created {Count} new artists", createdEntities.Artists.Count);
                    if (createdEntities.Labels?.Any() == true)
                        _logger.LogInformation("Created {Count} new labels", createdEntities.Labels.Count);
                    if (createdEntities.Genres?.Any() == true)
                        _logger.LogInformation("Created {Count} new genres", createdEntities.Genres.Count);
                    if (createdEntities.Countries?.Any() == true)
                        _logger.LogInformation("Created {Count} new countries", createdEntities.Countries.Count);
                    if (createdEntities.Formats?.Any() == true)
                        _logger.LogInformation("Created {Count} new formats", createdEntities.Formats.Count);
                    if (createdEntities.Packagings?.Any() == true)
                        _logger.LogInformation("Created {Count} new packagings", createdEntities.Packagings.Count);

                    var createdDto = await MapToFullDto(musicRelease);
                    
                    var response = new CreateMusicReleaseResponseDto
                    {
                        Release = createdDto,
                        Created = HasCreatedEntities(createdEntities) ? createdEntities : null
                    };

                    return CreatedAtAction(nameof(GetMusicRelease), new { id = musicRelease.Id }, response);
                }
                catch (Exception)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating music release: {Title}", createDto.Title);
                return StatusCode(500, "An error occurred while creating the music release");
            }
        }

        /// <summary>
        /// Updates an existing music release
        /// </summary>
        /// <param name="id">Music release ID</param>
        /// <param name="updateDto">Updated music release data</param>
        /// <returns>Updated music release</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(MusicReleaseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<MusicReleaseDto>> UpdateMusicRelease(int id, [FromBody] UpdateMusicReleaseDto updateDto)
        {
            try
            {
                _logger.LogInformation("Updating music release: {Id}", id);

                var existingMusicRelease = await _musicReleaseRepository.GetByIdAsync(id);
                if (existingMusicRelease == null)
                {
                    return NotFound($"Music release with ID {id} not found");
                }

                // Validate relationships exist
                var validationResult = await ValidateRelationships(updateDto.LabelId, updateDto.CountryId, 
                    updateDto.FormatId, updateDto.PackagingId, updateDto.ArtistIds, updateDto.GenreIds);
                
                if (validationResult != null)
                    return validationResult;

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

                var updatedDto = await MapToFullDto(existingMusicRelease);
                return Ok(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating music release: {Id}", id);
                return StatusCode(500, "An error occurred while updating the music release");
            }
        }

        /// <summary>
        /// Deletes a music release
        /// </summary>
        /// <param name="id">Music release ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteMusicRelease(int id)
        {
            try
            {
                _logger.LogInformation("Deleting music release: {Id}", id);

                var musicRelease = await _musicReleaseRepository.GetByIdAsync(id);
                if (musicRelease == null)
                {
                    return NotFound($"Music release with ID {id} not found");
                }

                _musicReleaseRepository.Delete(musicRelease);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Music release deleted successfully: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting music release: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the music release");
            }
        }

        // Private helper methods
        
        /// <summary>
        /// Check for potential duplicate releases based on title, catalog number, or artist
        /// </summary>
        /// <param name="title">Release title</param>
        /// <param name="labelNumber">Catalog/label number</param>
        /// <param name="artistIds">Artist IDs</param>
        /// <returns>List of potential duplicates</returns>
        private async Task<List<MusicRelease>> CheckForDuplicates(string title, string? labelNumber, List<int>? artistIds)
        {
            var duplicates = new List<MusicRelease>();

            // Check by exact catalog number (strongest indicator of duplicate)
            if (!string.IsNullOrWhiteSpace(labelNumber))
            {
                var normalizedCatalog = labelNumber.Trim().ToLower();
                var catalogMatches = await _musicReleaseRepository.GetAsync(
                    filter: r => r.LabelNumber != null && r.LabelNumber.ToLower() == normalizedCatalog);
                duplicates.AddRange(catalogMatches);
            }

            // If no catalog number match, check by title + artist combination
            if (!duplicates.Any() && artistIds != null && artistIds.Any())
            {
                // Normalize title for comparison (lowercase, trim)
                var normalizedTitle = title.Trim().ToLower();
                
                var allReleases = await _musicReleaseRepository.GetAllAsync();
                var titleArtistMatches = allReleases.Where(r =>
                {
                    // Check title similarity
                    if (r.Title.Trim().ToLower() != normalizedTitle)
                        return false;

                    // Check if at least one artist matches
                    if (string.IsNullOrEmpty(r.Artists))
                        return false;

                    var releaseArtistIds = JsonSerializer.Deserialize<List<int>>(r.Artists);
                    return releaseArtistIds != null && releaseArtistIds.Intersect(artistIds).Any();
                });

                duplicates.AddRange(titleArtistMatches);
            }

            return duplicates.Distinct().ToList();
        }

        private async Task<ActionResult?> ValidateRelationships(
            int? labelId, int? countryId, int? formatId, int? packagingId,
            List<int>? artistIds, List<int>? genreIds)
        {
            if (labelId.HasValue && !await _labelRepository.ExistsAsync(labelId.Value))
                return BadRequest($"Label with ID {labelId} does not exist");

            if (countryId.HasValue && !await _countryRepository.ExistsAsync(countryId.Value))
                return BadRequest($"Country with ID {countryId} does not exist");

            if (formatId.HasValue && !await _formatRepository.ExistsAsync(formatId.Value))
                return BadRequest($"Format with ID {formatId} does not exist");

            if (packagingId.HasValue && !await _packagingRepository.ExistsAsync(packagingId.Value))
                return BadRequest($"Packaging with ID {packagingId} does not exist");

            if (artistIds != null)
            {
                foreach (var artistId in artistIds)
                {
                    if (!await _artistRepository.ExistsAsync(artistId))
                        return BadRequest($"Artist with ID {artistId} does not exist");
                }
            }

            if (genreIds != null)
            {
                foreach (var genreId in genreIds)
                {
                    if (!await _genreRepository.ExistsAsync(genreId))
                        return BadRequest($"Genre with ID {genreId} does not exist");
                }
            }

            return null;
        }

        // ===== LOOKUP ENTITY RESOLUTION/CREATION METHODS =====

        /// <summary>
        /// Resolves artist IDs or creates new artists from names
        /// </summary>
        private async Task<List<int>?> ResolveOrCreateArtists(
            List<int>? artistIds, 
            List<string>? artistNames, 
            CreatedEntitiesDto createdEntities)
        {
            var resolvedIds = new List<int>();

            // Add existing IDs
            if (artistIds != null)
            {
                resolvedIds.AddRange(artistIds);
            }

            // Create or find artists by name
            if (artistNames != null && artistNames.Any())
            {
                foreach (var name in artistNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    var trimmedName = name.Trim();
                    
                    // Check if artist already exists (case-insensitive)
                    var existing = await _artistRepository.GetFirstOrDefaultAsync(
                        a => a.Name.ToLower() == trimmedName.ToLower());

                    if (existing != null)
                    {
                        resolvedIds.Add(existing.Id);
                        _logger.LogDebug("Found existing artist: {Name} (ID: {Id})", existing.Name, existing.Id);
                    }
                    else
                    {
                        // Create new artist
                        var newArtist = new Artist { Name = trimmedName };
                        await _artistRepository.AddAsync(newArtist);
                        await _unitOfWork.SaveChangesAsync(); // Save to get ID
                        
                        resolvedIds.Add(newArtist.Id);
                        
                        // Track created entity
                        createdEntities.Artists ??= new List<ArtistDto>();
                        createdEntities.Artists.Add(new ArtistDto { Id = newArtist.Id, Name = newArtist.Name });
                        
                        _logger.LogInformation("Created new artist: {Name} (ID: {Id})", newArtist.Name, newArtist.Id);
                    }
                }
            }

            return resolvedIds.Any() ? resolvedIds : null;
        }

        /// <summary>
        /// Resolves genre IDs or creates new genres from names
        /// </summary>
        private async Task<List<int>?> ResolveOrCreateGenres(
            List<int>? genreIds, 
            List<string>? genreNames, 
            CreatedEntitiesDto createdEntities)
        {
            var resolvedIds = new List<int>();

            // Add existing IDs
            if (genreIds != null)
            {
                resolvedIds.AddRange(genreIds);
            }

            // Create or find genres by name
            if (genreNames != null && genreNames.Any())
            {
                foreach (var name in genreNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    var trimmedName = name.Trim();
                    
                    // Check if genre already exists (case-insensitive)
                    var existing = await _genreRepository.GetFirstOrDefaultAsync(
                        g => g.Name.ToLower() == trimmedName.ToLower());

                    if (existing != null)
                    {
                        resolvedIds.Add(existing.Id);
                        _logger.LogDebug("Found existing genre: {Name} (ID: {Id})", existing.Name, existing.Id);
                    }
                    else
                    {
                        // Create new genre
                        var newGenre = new Genre { Name = trimmedName };
                        await _genreRepository.AddAsync(newGenre);
                        await _unitOfWork.SaveChangesAsync(); // Save to get ID
                        
                        resolvedIds.Add(newGenre.Id);
                        
                        // Track created entity
                        createdEntities.Genres ??= new List<GenreDto>();
                        createdEntities.Genres.Add(new GenreDto { Id = newGenre.Id, Name = newGenre.Name });
                        
                        _logger.LogInformation("Created new genre: {Name} (ID: {Id})", newGenre.Name, newGenre.Id);
                    }
                }
            }

            return resolvedIds.Any() ? resolvedIds : null;
        }

        /// <summary>
        /// Resolves label ID or creates new label from name
        /// </summary>
        private async Task<int?> ResolveOrCreateLabel(
            int? labelId, 
            string? labelName, 
            CreatedEntitiesDto createdEntities)
        {
            // If ID provided, use it
            if (labelId.HasValue)
                return labelId;

            // If name provided, find or create
            if (!string.IsNullOrWhiteSpace(labelName))
            {
                var trimmedName = labelName.Trim();
                
                // Check if label already exists (case-insensitive)
                var existing = await _labelRepository.GetFirstOrDefaultAsync(
                    l => l.Name.ToLower() == trimmedName.ToLower());

                if (existing != null)
                {
                    _logger.LogDebug("Found existing label: {Name} (ID: {Id})", existing.Name, existing.Id);
                    return existing.Id;
                }
                else
                {
                    // Create new label
                    var newLabel = new Label { Name = trimmedName };
                    await _labelRepository.AddAsync(newLabel);
                    await _unitOfWork.SaveChangesAsync(); // Save to get ID
                    
                    // Track created entity
                    createdEntities.Labels ??= new List<LabelDto>();
                    createdEntities.Labels.Add(new LabelDto { Id = newLabel.Id, Name = newLabel.Name });
                    
                    _logger.LogInformation("Created new label: {Name} (ID: {Id})", newLabel.Name, newLabel.Id);
                    return newLabel.Id;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves country ID or creates new country from name
        /// </summary>
        private async Task<int?> ResolveOrCreateCountry(
            int? countryId, 
            string? countryName, 
            CreatedEntitiesDto createdEntities)
        {
            // If ID provided, use it
            if (countryId.HasValue)
                return countryId;

            // If name provided, find or create
            if (!string.IsNullOrWhiteSpace(countryName))
            {
                var trimmedName = countryName.Trim();
                
                // Check if country already exists (case-insensitive)
                var existing = await _countryRepository.GetFirstOrDefaultAsync(
                    c => c.Name.ToLower() == trimmedName.ToLower());

                if (existing != null)
                {
                    _logger.LogDebug("Found existing country: {Name} (ID: {Id})", existing.Name, existing.Id);
                    return existing.Id;
                }
                else
                {
                    // Create new country
                    var newCountry = new Country { Name = trimmedName };
                    await _countryRepository.AddAsync(newCountry);
                    await _unitOfWork.SaveChangesAsync(); // Save to get ID
                    
                    // Track created entity
                    createdEntities.Countries ??= new List<CountryDto>();
                    createdEntities.Countries.Add(new CountryDto { Id = newCountry.Id, Name = newCountry.Name });
                    
                    _logger.LogInformation("Created new country: {Name} (ID: {Id})", newCountry.Name, newCountry.Id);
                    return newCountry.Id;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves format ID or creates new format from name
        /// </summary>
        private async Task<int?> ResolveOrCreateFormat(
            int? formatId, 
            string? formatName, 
            CreatedEntitiesDto createdEntities)
        {
            // If ID provided, use it
            if (formatId.HasValue)
                return formatId;

            // If name provided, find or create
            if (!string.IsNullOrWhiteSpace(formatName))
            {
                var trimmedName = formatName.Trim();
                
                // Check if format already exists (case-insensitive)
                var existing = await _formatRepository.GetFirstOrDefaultAsync(
                    f => f.Name.ToLower() == trimmedName.ToLower());

                if (existing != null)
                {
                    _logger.LogDebug("Found existing format: {Name} (ID: {Id})", existing.Name, existing.Id);
                    return existing.Id;
                }
                else
                {
                    // Create new format
                    var newFormat = new Format { Name = trimmedName };
                    await _formatRepository.AddAsync(newFormat);
                    await _unitOfWork.SaveChangesAsync(); // Save to get ID
                    
                    // Track created entity
                    createdEntities.Formats ??= new List<FormatDto>();
                    createdEntities.Formats.Add(new FormatDto { Id = newFormat.Id, Name = newFormat.Name });
                    
                    _logger.LogInformation("Created new format: {Name} (ID: {Id})", newFormat.Name, newFormat.Id);
                    return newFormat.Id;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves packaging ID or creates new packaging from name
        /// </summary>
        private async Task<int?> ResolveOrCreatePackaging(
            int? packagingId, 
            string? packagingName, 
            CreatedEntitiesDto createdEntities)
        {
            // If ID provided, use it
            if (packagingId.HasValue)
                return packagingId;

            // If name provided, find or create
            if (!string.IsNullOrWhiteSpace(packagingName))
            {
                var trimmedName = packagingName.Trim();
                
                // Check if packaging already exists (case-insensitive)
                var existing = await _packagingRepository.GetFirstOrDefaultAsync(
                    p => p.Name.ToLower() == trimmedName.ToLower());

                if (existing != null)
                {
                    _logger.LogDebug("Found existing packaging: {Name} (ID: {Id})", existing.Name, existing.Id);
                    return existing.Id;
                }
                else
                {
                    // Create new packaging
                    var newPackaging = new Packaging { Name = trimmedName };
                    await _packagingRepository.AddAsync(newPackaging);
                    await _unitOfWork.SaveChangesAsync(); // Save to get ID
                    
                    // Track created entity
                    createdEntities.Packagings ??= new List<PackagingDto>();
                    createdEntities.Packagings.Add(new PackagingDto { Id = newPackaging.Id, Name = newPackaging.Name });
                    
                    _logger.LogInformation("Created new packaging: {Name} (ID: {Id})", newPackaging.Name, newPackaging.Id);
                    return newPackaging.Id;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if any entities were created
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
            // This is synchronous for performance in list mapping - could be optimized with caching
            var artist = _artistRepository.GetByIdAsync(id).Result;
            return artist?.Name ?? $"Artist {id}";
        }

        private string GetGenreName(int id)
        {
            // This is synchronous for performance in list mapping - could be optimized with caching
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
                        // Resolve artist IDs to names
                        if (track.Artists != null && track.Artists.Count > 0)
                        {
                            var resolvedArtists = new List<string>();
                            foreach (var artistIdStr in track.Artists)
                            {
                                // Try to parse as ID, if it's numeric
                                if (int.TryParse(artistIdStr, out int artistId))
                                {
                                    var artist = await _artistRepository.GetByIdAsync(artistId);
                                    resolvedArtists.Add(artist?.Name ?? artistIdStr);
                                }
                                else
                                {
                                    // Already a name, keep as is
                                    resolvedArtists.Add(artistIdStr);
                                }
                            }
                            track.Artists = resolvedArtists;
                        }

                        // Resolve genre IDs to names
                        if (track.Genres != null && track.Genres.Count > 0)
                        {
                            var resolvedGenres = new List<string>();
                            foreach (var genreIdStr in track.Genres)
                            {
                                // Try to parse as ID, if it's numeric
                                if (int.TryParse(genreIdStr, out int genreId))
                                {
                                    var genre = await _genreRepository.GetByIdAsync(genreId);
                                    resolvedGenres.Add(genre?.Name ?? genreIdStr);
                                }
                                else
                                {
                                    // Already a name, keep as is
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

        /// <summary>
        /// Resolves purchase information from JSON string to DTO with store name resolution
        /// </summary>
        /// <param name="purchaseInfoJson">JSON string containing purchase information</param>
        /// <returns>Resolved purchase info DTO</returns>
        private async Task<MusicReleasePurchaseInfoDto?> ResolvePurchaseInfo(string? purchaseInfoJson)
        {
            if (string.IsNullOrEmpty(purchaseInfoJson))
                return null;

            try
            {
                // Deserialize the original JSON structure
                var purchaseInfo = JsonSerializer.Deserialize<Models.ValueObjects.PurchaseInfo>(purchaseInfoJson);
                if (purchaseInfo == null)
                    return null;

                // Resolve store name if StoreID is provided
                string? storeName = null;
                if (purchaseInfo.StoreID.HasValue)
                {
                    var store = await _storeRepository.GetByIdAsync(purchaseInfo.StoreID.Value);
                    storeName = store?.Name;
                }

                // Map to DTO
                return new MusicReleasePurchaseInfoDto
                {
                    StoreId = purchaseInfo.StoreID,
                    StoreName = storeName,
                    Price = purchaseInfo.Price,
                    Currency = "GBP", // Default currency - assuming GBP for UK-based collection
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
