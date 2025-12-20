using System.Text.Json;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Maps Discogs API responses to application DTOs
    /// </summary>
    public class DiscogsResponseMapper : IDiscogsResponseMapper
    {
        private readonly ILogger<DiscogsResponseMapper> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public DiscogsResponseMapper(ILogger<DiscogsResponseMapper> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Map search results JSON to DTOs
        /// </summary>
        public List<DiscogsSearchResultDto> MapSearchResults(string? jsonResponse)
        {
            if (string.IsNullOrEmpty(jsonResponse))
            {
                _logger.LogWarning("Empty JSON response provided for search results mapping");
                return new List<DiscogsSearchResultDto>();
            }

            try
            {
                var searchResponse = JsonSerializer.Deserialize<DiscogsSearchResponse>(jsonResponse, _jsonOptions);

                if (searchResponse?.Results == null)
                {
                    _logger.LogWarning("No results found in Discogs search response");
                    return new List<DiscogsSearchResultDto>();
                }

                _logger.LogInformation("Mapping {Count} search results from Discogs", searchResponse.Results.Count);

                var results = searchResponse.Results.Select(r => new DiscogsSearchResultDto
                {
                    Id = r.Id?.ToString() ?? string.Empty,
                    Title = r.Title ?? string.Empty,
                    Artist = string.Join(", ", r.Artist ?? Array.Empty<string>()),
                    Year = r.Year,
                    Format = r.Format?.FirstOrDefault(),
                    Label = r.Label?.FirstOrDefault(),
                    CatalogNumber = r.Catno,
                    Country = r.Country,
                    ThumbUrl = r.Thumb,
                    CoverImageUrl = r.CoverImage,
                    ResourceUrl = r.ResourceUrl
                }).ToList();

                return results;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error when mapping search results");
                return new List<DiscogsSearchResultDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping search results");
                return new List<DiscogsSearchResultDto>();
            }
        }

        /// <summary>
        /// Map release details JSON to DTO
        /// </summary>
        public DiscogsReleaseDto? MapReleaseDetails(string? jsonResponse)
        {
            if (string.IsNullOrEmpty(jsonResponse))
            {
                _logger.LogWarning("Empty JSON response provided for release details mapping");
                return null;
            }

            try
            {
                var releaseResponse = JsonSerializer.Deserialize<DiscogsReleaseResponse>(jsonResponse, _jsonOptions);

                if (releaseResponse == null)
                {
                    _logger.LogWarning("Failed to deserialize release details");
                    return null;
                }

                _logger.LogInformation("Successfully mapped release details for ID: {ReleaseId}", releaseResponse.Id);

                var dto = new DiscogsReleaseDto
                {
                    Id = releaseResponse.Id?.ToString() ?? string.Empty,
                    Title = releaseResponse.Title ?? string.Empty,
                    Year = releaseResponse.Year,
                    Country = releaseResponse.Country,
                    ReleasedDate = releaseResponse.ReleasedFormatted,
                    ResourceUrl = releaseResponse.ResourceUrl,
                    Uri = releaseResponse.Uri,
                    Notes = releaseResponse.Notes,
                    Genres = releaseResponse.Genres ?? new List<string>(),
                    Styles = releaseResponse.Styles ?? new List<string>(),
                    Artists = MapArtists(releaseResponse.Artists),
                    Labels = MapLabels(releaseResponse.Labels),
                    Formats = MapFormats(releaseResponse.Formats),
                    Images = MapImages(releaseResponse.Images),
                    Tracklist = MapTracklist(releaseResponse.Tracklist),
                    Identifiers = MapIdentifiers(releaseResponse.Identifiers)
                };

                return dto;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error when mapping release details");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping release details");
                return null;
            }
        }

        private List<DiscogsArtistDto> MapArtists(List<DiscogsArtistResponse>? artists)
        {
            if (artists == null) return new List<DiscogsArtistDto>();

            return artists.Select(a => new DiscogsArtistDto
            {
                Name = a.Name ?? string.Empty,
                Id = a.Id?.ToString(),
                ResourceUrl = a.ResourceUrl
            }).ToList();
        }

        private List<DiscogsLabelDto> MapLabels(List<DiscogsLabelResponse>? labels)
        {
            if (labels == null) return new List<DiscogsLabelDto>();

            return labels.Select(l => new DiscogsLabelDto
            {
                Name = l.Name ?? string.Empty,
                CatalogNumber = l.Catno,
                Id = l.Id?.ToString(),
                ResourceUrl = l.ResourceUrl
            }).ToList();
        }

        private List<DiscogsFormatDto> MapFormats(List<DiscogsFormatResponse>? formats)
        {
            if (formats == null) return new List<DiscogsFormatDto>();

            return formats.Select(f => new DiscogsFormatDto
            {
                Name = f.Name ?? string.Empty,
                Qty = f.Qty,
                Descriptions = f.Descriptions ?? new List<string>()
            }).ToList();
        }

        private List<DiscogsImageDto> MapImages(List<DiscogsImageResponse>? images)
        {
            if (images == null) return new List<DiscogsImageDto>();

            return images.Select(i => new DiscogsImageDto
            {
                Type = i.Type ?? string.Empty,
                Uri = i.Uri ?? string.Empty,
                Uri150 = i.Uri150,
                ResourceUrl = i.ResourceUrl,
                Width = i.Width,
                Height = i.Height
            }).ToList();
        }

        private List<DiscogsTrackDto> MapTracklist(List<DiscogsTrackResponse>? tracklist)
        {
            if (tracklist == null) return new List<DiscogsTrackDto>();

            return tracklist.Select(t => new DiscogsTrackDto
            {
                Position = t.Position ?? string.Empty,
                Title = t.Title ?? string.Empty,
                Duration = t.Duration,
                Artists = MapArtists(t.Artists)
            }).ToList();
        }

        private List<DiscogsIdentifierDto> MapIdentifiers(List<DiscogsIdentifierResponse>? identifiers)
        {
            if (identifiers == null) return new List<DiscogsIdentifierDto>();

            return identifiers.Select(i => new DiscogsIdentifierDto
            {
                Type = i.Type ?? string.Empty,
                Value = i.Value ?? string.Empty
            }).ToList();
        }

        /// <summary>
        /// Map user collection JSON to DTO
        /// </summary>
        public DiscogsCollectionResponseDto? MapCollectionResponse(string? jsonResponse)
        {
            if (string.IsNullOrEmpty(jsonResponse))
            {
                _logger.LogWarning("Empty JSON response provided for collection mapping");
                return null;
            }

            try
            {
                var collectionResponse = JsonSerializer.Deserialize<DiscogsCollectionApiResponse>(jsonResponse, _jsonOptions);

                if (collectionResponse == null)
                {
                    _logger.LogWarning("Failed to deserialize collection response");
                    return null;
                }

                _logger.LogInformation("Successfully mapped collection with {Count} releases", 
                    collectionResponse.Releases?.Count ?? 0);

                var dto = new DiscogsCollectionResponseDto
                {
                    Pagination = collectionResponse.Pagination != null ? new DiscogsPaginationDto
                    {
                        Page = collectionResponse.Pagination.Page,
                        PerPage = collectionResponse.Pagination.PerPage,
                        Pages = collectionResponse.Pagination.Pages,
                        Items = collectionResponse.Pagination.Items
                    } : null,
                    Releases = MapCollectionReleases(collectionResponse.Releases)
                };

                return dto;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error when mapping collection");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping collection response");
                return null;
            }
        }

        private List<DiscogsCollectionReleaseDto> MapCollectionReleases(List<DiscogsCollectionReleaseResponse>? releases)
        {
            if (releases == null) return new List<DiscogsCollectionReleaseDto>();

            return releases.Select(r => new DiscogsCollectionReleaseDto
            {
                InstanceId = r.InstanceId?.ToString(),
                Rating = r.Rating,
                DateAdded = r.DateAdded,
                Notes = r.Notes?.Select(n => new DiscogsNoteDto
                {
                    FieldId = n.FieldId,
                    Value = n.Value
                }).ToList(),
                BasicInformation = r.BasicInformation != null ? new DiscogsBasicInfoDto
                {
                    Id = r.BasicInformation.Id,
                    Title = r.BasicInformation.Title ?? string.Empty,
                    Year = r.BasicInformation.Year,
                    Country = r.BasicInformation.Country,
                    CoverImage = r.BasicInformation.CoverImage,
                    Thumb = r.BasicInformation.Thumb,
                    ResourceUrl = r.BasicInformation.ResourceUrl,
                    Artists = MapArtists(r.BasicInformation.Artists),
                    Labels = MapLabels(r.BasicInformation.Labels),
                    Formats = MapFormats(r.BasicInformation.Formats),
                    Genres = r.BasicInformation.Genres ?? new List<string>(),
                    Styles = r.BasicInformation.Styles ?? new List<string>()
                } : null
            }).ToList();
        }

        #region Internal Response Models (for deserialization)

        private class DiscogsSearchResponse
        {
            public List<DiscogsSearchResult>? Results { get; set; }
        }

        private class DiscogsSearchResult
        {
            public long? Id { get; set; }
            public string? Title { get; set; }
            public string[]? Artist { get; set; }
            public string? Year { get; set; }
            public string[]? Format { get; set; }
            public string[]? Label { get; set; }
            public string? Catno { get; set; }
            public string? Country { get; set; }
            public string? Thumb { get; set; }
            public string? CoverImage { get; set; }
            public string? ResourceUrl { get; set; }
        }

        private class DiscogsReleaseResponse
        {
            public long? Id { get; set; }
            public string? Title { get; set; }
            public int? Year { get; set; }
            public string? Country { get; set; }
            public string? ReleasedFormatted { get; set; }
            public string? ResourceUrl { get; set; }
            public string? Uri { get; set; }
            public string? Notes { get; set; }
            public List<string>? Genres { get; set; }
            public List<string>? Styles { get; set; }
            public List<DiscogsArtistResponse>? Artists { get; set; }
            public List<DiscogsLabelResponse>? Labels { get; set; }
            public List<DiscogsFormatResponse>? Formats { get; set; }
            public List<DiscogsImageResponse>? Images { get; set; }
            public List<DiscogsTrackResponse>? Tracklist { get; set; }
            public List<DiscogsIdentifierResponse>? Identifiers { get; set; }
        }

        private class DiscogsArtistResponse
        {
            public string? Name { get; set; }
            public long? Id { get; set; }
            public string? ResourceUrl { get; set; }
        }

        private class DiscogsLabelResponse
        {
            public string? Name { get; set; }
            public string? Catno { get; set; }
            public long? Id { get; set; }
            public string? ResourceUrl { get; set; }
        }

        private class DiscogsFormatResponse
        {
            public string? Name { get; set; }
            public string? Qty { get; set; }
            public List<string>? Descriptions { get; set; }
        }

        private class DiscogsImageResponse
        {
            public string? Type { get; set; }
            public string? Uri { get; set; }
            public string? Uri150 { get; set; }
            public string? ResourceUrl { get; set; }
            public int? Width { get; set; }
            public int? Height { get; set; }
        }

        private class DiscogsTrackResponse
        {
            public string? Position { get; set; }
            public string? Title { get; set; }
            public string? Duration { get; set; }
            public List<DiscogsArtistResponse>? Artists { get; set; }
        }

        private class DiscogsIdentifierResponse
        {
            public string? Type { get; set; }
            public string? Value { get; set; }
        }

        private class DiscogsCollectionApiResponse
        {
            public DiscogsPaginationResponse? Pagination { get; set; }
            public List<DiscogsCollectionReleaseResponse>? Releases { get; set; }
        }

        private class DiscogsPaginationResponse
        {
            public int Page { get; set; }
            public int PerPage { get; set; }
            public int Pages { get; set; }
            public int Items { get; set; }
        }

        private class DiscogsCollectionReleaseResponse
        {
            public long? InstanceId { get; set; }
            public int? Rating { get; set; }
            public DiscogsBasicInfoResponse? BasicInformation { get; set; }
            public List<DiscogsNoteResponse>? Notes { get; set; }
            public string? DateAdded { get; set; }
        }

        private class DiscogsBasicInfoResponse
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public int? Year { get; set; }
            public List<DiscogsArtistResponse>? Artists { get; set; }
            public List<DiscogsLabelResponse>? Labels { get; set; }
            public List<DiscogsFormatResponse>? Formats { get; set; }
            public List<string>? Genres { get; set; }
            public List<string>? Styles { get; set; }
            public string? Country { get; set; }
            public string? CoverImage { get; set; }
            public string? Thumb { get; set; }
            public string? ResourceUrl { get; set; }
        }

        private class DiscogsNoteResponse
        {
            public int FieldId { get; set; }
            public string? Value { get; set; }
        }

        #endregion
    }
}
