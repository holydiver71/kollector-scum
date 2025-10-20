using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for resolving or creating lookup entities by ID or name
    /// </summary>
    public class EntityResolverService : IEntityResolverService
    {
        private readonly IRepository<Artist> _artistRepository;
        private readonly IRepository<Genre> _genreRepository;
        private readonly IRepository<Label> _labelRepository;
        private readonly IRepository<Country> _countryRepository;
        private readonly IRepository<Format> _formatRepository;
        private readonly IRepository<Packaging> _packagingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EntityResolverService> _logger;

        public EntityResolverService(
            IRepository<Artist> artistRepository,
            IRepository<Genre> genreRepository,
            IRepository<Label> labelRepository,
            IRepository<Country> countryRepository,
            IRepository<Format> formatRepository,
            IRepository<Packaging> packagingRepository,
            IUnitOfWork unitOfWork,
            ILogger<EntityResolverService> logger)
        {
            _artistRepository = artistRepository;
            _genreRepository = genreRepository;
            _labelRepository = labelRepository;
            _countryRepository = countryRepository;
            _formatRepository = formatRepository;
            _packagingRepository = packagingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<int>?> ResolveOrCreateArtistsAsync(
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
                        _logger.LogDebug("Found existing artist: {Name} (ID: {Id})", existing.Name, existing.Id);
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

        public async Task<List<int>?> ResolveOrCreateGenresAsync(
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
                        _logger.LogDebug("Found existing genre: {Name} (ID: {Id})", existing.Name, existing.Id);
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

        public async Task<int?> ResolveOrCreateLabelAsync(
            int? labelId, 
            string? labelName, 
            CreatedEntitiesDto createdEntities)
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
                    _logger.LogDebug("Found existing label: {Name} (ID: {Id})", existing.Name, existing.Id);
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

        public async Task<int?> ResolveOrCreateCountryAsync(
            int? countryId, 
            string? countryName, 
            CreatedEntitiesDto createdEntities)
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
                    _logger.LogDebug("Found existing country: {Name} (ID: {Id})", existing.Name, existing.Id);
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

        public async Task<int?> ResolveOrCreateFormatAsync(
            int? formatId, 
            string? formatName, 
            CreatedEntitiesDto createdEntities)
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
                    _logger.LogDebug("Found existing format: {Name} (ID: {Id})", existing.Name, existing.Id);
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

        public async Task<int?> ResolveOrCreatePackagingAsync(
            int? packagingId, 
            string? packagingName, 
            CreatedEntitiesDto createdEntities)
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
                    _logger.LogDebug("Found existing packaging: {Name} (ID: {Id})", existing.Name, existing.Id);
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
    }
}
