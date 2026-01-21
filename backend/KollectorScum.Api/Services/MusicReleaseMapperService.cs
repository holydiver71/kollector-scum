using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Models.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for mapping between MusicRelease entities and DTOs
    /// </summary>
    public class MusicReleaseMapperService : IMusicReleaseMapperService
    {
        private readonly IRepository<Artist> _artistRepository;
        private readonly IRepository<Genre> _genreRepository;
        private readonly IRepository<Store> _storeRepository;
        private readonly ILogger<MusicReleaseMapperService> _logger;
        private readonly IStorageService _storageService;
        private readonly IConfiguration _configuration;

        public MusicReleaseMapperService(
            IRepository<Artist> artistRepository,
            IRepository<Genre> genreRepository,
            IRepository<Store> storeRepository,
            ILogger<MusicReleaseMapperService> logger,
            IStorageService? storageService,
            IConfiguration? configuration)
        {
            _artistRepository = artistRepository;
            _genreRepository = genreRepository;
            _storeRepository = storeRepository;
            _logger = logger;
            // Tests and some callers may not provide storage/config during unit construction.
            // Provide safe fallbacks in that case so the mapper remains usable in tests.
            _storageService = storageService ?? new NoopStorageService();
            _configuration = configuration ?? new ConfigurationBuilder().AddInMemoryCollection().Build();
        }

        // Backwards-compatible constructor for tests/older callers that don't provide storage/config
        public MusicReleaseMapperService(
            IRepository<Artist> artistRepository,
            IRepository<Genre> genreRepository,
            IRepository<Store> storeRepository,
            ILogger<MusicReleaseMapperService> logger)
            : this(artistRepository, genreRepository, storeRepository, logger, null, null)
        {
        }

        // Lightweight fallback used when DI doesn't provide a storage implementation (tests/local)
        private class NoopStorageService : IStorageService
        {
            public Task<string> UploadFileAsync(string bucketName, string userId, string fileName, Stream fileStream, string contentType)
            {
                var safe = Path.GetFileName(fileName);
                    return Task.FromResult(safe);
            }

            public Task DeleteFileAsync(string bucketName, string userId, string fileName)
            {
                return Task.CompletedTask;
            }

            public string GetPublicUrl(string bucketName, string userId, string fileName)
            {
                var safe = Path.GetFileName(fileName);
                    return safe;
            }
        }

        public MusicReleaseSummaryDto MapToSummaryDto(MusicRelease musicRelease)
        {
            List<int>? artistIds = null;
            List<int>? genreIds = null;
            MusicReleaseImageDto? images = null;

            try
            {
                if (!string.IsNullOrEmpty(musicRelease.Artists))
                {
                    artistIds = JsonSerializer.Deserialize<List<int>>(musicRelease.Artists);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Artists JSON for release {Id}", musicRelease.Id);
                artistIds = null;
            }

            try
            {
                if (!string.IsNullOrEmpty(musicRelease.Genres))
                {
                    genreIds = JsonSerializer.Deserialize<List<int>>(musicRelease.Genres);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Genres JSON for release {Id}", musicRelease.Id);
                genreIds = null;
            }

            try
            {
                if (!string.IsNullOrEmpty(musicRelease.Images))
                {
                    images = JsonSerializer.Deserialize<MusicReleaseImageDto>(musicRelease.Images);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Images JSON for release {Id}", musicRelease.Id);
                images = null;
            }

            // Resolve image URLs (convert local/relative filenames to public URLs when storage is configured)
            images = ResolveImageUrls(images, musicRelease.UserId);

            return new MusicReleaseSummaryDto
            {
                Id = musicRelease.Id,
                Title = musicRelease.Title,
                ReleaseYear = musicRelease.ReleaseYear,
                OrigReleaseYear = musicRelease.OrigReleaseYear,
                ArtistNames = artistIds?.Select(id => GetArtistName(id)).ToList(),
                GenreNames = genreIds?.Select(id => GetGenreName(id)).ToList(),
                LabelName = musicRelease.Label?.Name,
                FormatName = musicRelease.Format?.Name,
                CountryName = musicRelease.Country?.Name,
                CoverImageUrl = images?.CoverFront ?? images?.Thumbnail,
                DateAdded = musicRelease.DateAdded
            };
        }

        public async Task<MusicReleaseDto> MapToFullDtoAsync(MusicRelease musicRelease)
        {
            List<int>? artistIds = null;
            List<int>? genreIds = null;
            try
            {
                if (!string.IsNullOrEmpty(musicRelease.Artists))
                    artistIds = JsonSerializer.Deserialize<List<int>>(musicRelease.Artists);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Artists JSON for release {Id}", musicRelease.Id);
                artistIds = null;
            }
            try
            {
                if (!string.IsNullOrEmpty(musicRelease.Genres))
                    genreIds = JsonSerializer.Deserialize<List<int>>(musicRelease.Genres);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Genres JSON for release {Id}", musicRelease.Id);
                genreIds = null;
            }

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

            var images = await SafeDeserializeImageAsync(musicRelease.Images);
            images = ResolveImageUrls(images, musicRelease.UserId);

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
                PurchaseInfo = await ResolvePurchaseInfoAsync(musicRelease.PurchaseInfo),
                Images = images,
                Links = await SafeDeserializeLinksAsync(musicRelease.Links),
                Media = await ResolveMediaArtistsAsync(musicRelease.Media),
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

        private async Task<List<MusicReleaseMediaDto>?> ResolveMediaArtistsAsync(string? mediaJson)
        {
            if (string.IsNullOrEmpty(mediaJson))
                return null;

            List<MusicReleaseMediaDto>? mediaList = null;
            try
            {
                mediaList = JsonSerializer.Deserialize<List<MusicReleaseMediaDto>>(mediaJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Media JSON for release media: {MediaJson}", mediaJson);
                return null;
            }
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

        private Task<MusicReleaseImageDto?> SafeDeserializeImageAsync(string? imagesJson)
        {
            if (string.IsNullOrEmpty(imagesJson)) return Task.FromResult<MusicReleaseImageDto?>(null);
            try
            {
                var dto = JsonSerializer.Deserialize<MusicReleaseImageDto>(imagesJson);
                return Task.FromResult(dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Images JSON for release: {Json}", imagesJson);
                return Task.FromResult<MusicReleaseImageDto?>(null);
            }
        }

        private Task<List<MusicReleaseLinkDto>?> SafeDeserializeLinksAsync(string? linksJson)
        {
            if (string.IsNullOrEmpty(linksJson)) return Task.FromResult<List<MusicReleaseLinkDto>?>(null);
            try
            {
                var list = JsonSerializer.Deserialize<List<MusicReleaseLinkDto>>(linksJson);
                return Task.FromResult(list);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Links JSON for release: {Json}", linksJson);
                return Task.FromResult<List<MusicReleaseLinkDto>?>(null);
            }
        }

        private MusicReleaseImageDto? ResolveImageUrls(MusicReleaseImageDto? images, Guid userId)
        {
            if (images == null) return null;
            images.CoverFront = ResolveImageUrl(images.CoverFront, userId);
            images.CoverBack = ResolveImageUrl(images.CoverBack, userId);
            images.Thumbnail = ResolveImageUrl(images.Thumbnail, userId);
            return images;
        }

        private string? ResolveImageUrl(string? imageValue, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(imageValue)) return null;
            var trimmed = imageValue.Trim();
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return trimmed;

            // If value is a path like /cover-art/{userId}/{filename} or just a filename, use the last segment as filename
            var fileName = trimmed.Contains('/') ? trimmed.Split('/').Last() : trimmed;

            // Only resolve to a storage public URL when R2/storage is configured; otherwise return the raw value.
            var r2Endpoint = _configuration["R2:Endpoint"] ?? _configuration["R2__Endpoint"];
            var r2AccountId = _configuration["R2:AccountId"] ?? _configuration["R2__AccountId"];
            var r2PublicBaseUrl = _configuration["R2:PublicBaseUrl"] ?? _configuration["R2__PublicBaseUrl"];

            if (string.IsNullOrWhiteSpace(r2Endpoint) && string.IsNullOrWhiteSpace(r2AccountId) && string.IsNullOrWhiteSpace(r2PublicBaseUrl))
            {
                return trimmed;
            }

            var bucket = _configuration["R2:BucketName"] ?? _configuration["R2__BucketName"] ?? "cover-art";
            try
            {
                return _storageService.GetPublicUrl(bucket, userId.ToString(), fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve image URL for {ImageValue}", imageValue);
                return trimmed;
            }
        }

        private async Task<MusicReleasePurchaseInfoDto?> ResolvePurchaseInfoAsync(string? purchaseInfoJson)
        {
            if (string.IsNullOrEmpty(purchaseInfoJson))
                return null;

            try
            {
                var jsonOptions = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };

                // Determine which format by checking if "Date" or "PurchaseDate" exists in the JSON
                bool hasDateField = purchaseInfoJson.Contains("\"Date\"", StringComparison.OrdinalIgnoreCase);
                bool hasPurchaseDateField = purchaseInfoJson.Contains("\"PurchaseDate\"", StringComparison.OrdinalIgnoreCase);

                if (hasPurchaseDateField)
                {
                    // Try DTO format (StoreId, PurchaseDate) - legacy format
                    var purchaseInfoDto = JsonSerializer.Deserialize<MusicReleasePurchaseInfoDto>(purchaseInfoJson, jsonOptions);
                    if (purchaseInfoDto != null)
                    {
                        string? storeName = null;
                        if (purchaseInfoDto.StoreId.HasValue)
                        {
                            var store = await _storeRepository.GetByIdAsync(purchaseInfoDto.StoreId.Value);
                            storeName = store?.Name;
                        }

                        return new MusicReleasePurchaseInfoDto
                        {
                            StoreId = purchaseInfoDto.StoreId,
                            StoreName = storeName,
                            Price = purchaseInfoDto.Price,
                            Currency = purchaseInfoDto.Currency ?? "GBP",
                            PurchaseDate = purchaseInfoDto.PurchaseDate,
                            Notes = purchaseInfoDto.Notes
                        };
                    }
                }
                
                if (hasDateField)
                {
                    // Try value object format (StoreID, Date) - correct format
                    var purchaseInfo = JsonSerializer.Deserialize<PurchaseInfo>(purchaseInfoJson, jsonOptions);
                    if (purchaseInfo != null)
                    {
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
                }

                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse purchase info JSON: {Json}", purchaseInfoJson);
                return null;
            }
        }
    }
}
