using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Models.ValueObjects;
using Microsoft.Extensions.Logging;
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

        public MusicReleaseMapperService(
            IRepository<Artist> artistRepository,
            IRepository<Genre> genreRepository,
            IRepository<Store> storeRepository,
            ILogger<MusicReleaseMapperService> logger)
        {
            _artistRepository = artistRepository;
            _genreRepository = genreRepository;
            _storeRepository = storeRepository;
            _logger = logger;
        }

        public MusicReleaseSummaryDto MapToSummaryDto(MusicRelease musicRelease)
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
                PurchaseInfo = await ResolvePurchaseInfoAsync(musicRelease.PurchaseInfo),
                Images = string.IsNullOrEmpty(musicRelease.Images) 
                    ? null 
                    : JsonSerializer.Deserialize<MusicReleaseImageDto>(musicRelease.Images),
                Links = string.IsNullOrEmpty(musicRelease.Links) 
                    ? null 
                    : JsonSerializer.Deserialize<List<MusicReleaseLinkDto>>(musicRelease.Links),
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
