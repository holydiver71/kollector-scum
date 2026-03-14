using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for resolving or creating lookup entities by ID or name.
    /// Uses generic helpers to eliminate duplication across entity types.
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
        private readonly IUserContext _userContext;

        public EntityResolverService(
            IRepository<Artist> artistRepository,
            IRepository<Genre> genreRepository,
            IRepository<Label> labelRepository,
            IRepository<Country> countryRepository,
            IRepository<Format> formatRepository,
            IRepository<Packaging> packagingRepository,
            IUnitOfWork unitOfWork,
            ILogger<EntityResolverService> logger,
            IUserContext userContext)
        {
            _artistRepository = artistRepository;
            _genreRepository = genreRepository;
            _labelRepository = labelRepository;
            _countryRepository = countryRepository;
            _formatRepository = formatRepository;
            _packagingRepository = packagingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        /// <inheritdoc />
        public async Task<List<int>?> ResolveOrCreateArtistsAsync(
            List<int>? artistIds,
            List<string>? artistNames,
            CreatedEntitiesDto createdEntities)
        {
            var userId = RequireUserId();
            var resolvedIds = new List<int>(artistIds ?? (IEnumerable<int>)Array.Empty<int>());

            if (artistNames != null)
            {
                foreach (var name in artistNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    var id = await ResolveOrCreateEntityAsync(
                        name.Trim(), userId, _artistRepository,
                        n => new Artist { Name = n, UserId = userId },
                        entity =>
                        {
                            createdEntities.Artists ??= new List<ArtistDto>();
                            createdEntities.Artists.Add(new ArtistDto { Id = entity.Id, Name = entity.Name });
                        },
                        "artist");

                    resolvedIds.Add(id);
                }
            }

            return resolvedIds.Count > 0 ? resolvedIds : null;
        }

        /// <inheritdoc />
        public async Task<List<int>?> ResolveOrCreateGenresAsync(
            List<int>? genreIds,
            List<string>? genreNames,
            CreatedEntitiesDto createdEntities)
        {
            var userId = RequireUserId();
            var resolvedIds = new List<int>(genreIds ?? (IEnumerable<int>)Array.Empty<int>());

            if (genreNames != null)
            {
                foreach (var name in genreNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    var id = await ResolveOrCreateEntityAsync(
                        name.Trim(), userId, _genreRepository,
                        n => new Genre { Name = n, UserId = userId },
                        entity =>
                        {
                            createdEntities.Genres ??= new List<GenreDto>();
                            createdEntities.Genres.Add(new GenreDto { Id = entity.Id, Name = entity.Name });
                        },
                        "genre");

                    resolvedIds.Add(id);
                }
            }

            return resolvedIds.Count > 0 ? resolvedIds : null;
        }

        /// <inheritdoc />
        public async Task<int?> ResolveOrCreateLabelAsync(
            int? labelId,
            string? labelName,
            CreatedEntitiesDto createdEntities)
        {
            if (labelId.HasValue) return labelId;
            var userId = RequireUserId();

            return await ResolveOrCreateSingleEntityAsync(
                labelName, userId, _labelRepository,
                n => new Label { Name = n, UserId = userId },
                entity =>
                {
                    createdEntities.Labels ??= new List<LabelDto>();
                    createdEntities.Labels.Add(new LabelDto { Id = entity.Id, Name = entity.Name });
                },
                "label");
        }

        /// <inheritdoc />
        public async Task<int?> ResolveOrCreateCountryAsync(
            int? countryId,
            string? countryName,
            CreatedEntitiesDto createdEntities)
        {
            if (countryId.HasValue) return countryId;
            var userId = RequireUserId();

            return await ResolveOrCreateSingleEntityAsync(
                countryName, userId, _countryRepository,
                n => new Country { Name = n, UserId = userId },
                entity =>
                {
                    createdEntities.Countries ??= new List<CountryDto>();
                    createdEntities.Countries.Add(new CountryDto { Id = entity.Id, Name = entity.Name });
                },
                "country");
        }

        /// <inheritdoc />
        public async Task<int?> ResolveOrCreateFormatAsync(
            int? formatId,
            string? formatName,
            CreatedEntitiesDto createdEntities)
        {
            if (formatId.HasValue) return formatId;
            var userId = RequireUserId();

            return await ResolveOrCreateSingleEntityAsync(
                formatName, userId, _formatRepository,
                n => new Format { Name = n, UserId = userId },
                entity =>
                {
                    createdEntities.Formats ??= new List<FormatDto>();
                    createdEntities.Formats.Add(new FormatDto { Id = entity.Id, Name = entity.Name });
                },
                "format");
        }

        /// <inheritdoc />
        public async Task<int?> ResolveOrCreatePackagingAsync(
            int? packagingId,
            string? packagingName,
            CreatedEntitiesDto createdEntities)
        {
            if (packagingId.HasValue) return packagingId;
            var userId = RequireUserId();

            return await ResolveOrCreateSingleEntityAsync(
                packagingName, userId, _packagingRepository,
                n => new Packaging { Name = n, UserId = userId },
                entity =>
                {
                    createdEntities.Packagings ??= new List<PackagingDto>();
                    createdEntities.Packagings.Add(new PackagingDto { Id = entity.Id, Name = entity.Name });
                },
                "packaging");
        }

        #region Private Generic Helpers

        /// <summary>
        /// Resolves or creates a single named entity.  Returns null if <paramref name="name"/> is blank.
        /// </summary>
        private async Task<int?> ResolveOrCreateSingleEntityAsync<TEntity>(
            string? name,
            Guid userId,
            IRepository<TEntity> repository,
            Func<string, TEntity> createEntity,
            Action<TEntity> afterCreate,
            string entityTypeName)
            where TEntity : class, INamedUserOwnedEntity
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return await ResolveOrCreateEntityAsync(name.Trim(), userId, repository, createEntity, afterCreate, entityTypeName);
        }

        /// <summary>
        /// Looks up an entity by <paramref name="trimmedName"/> and user; creates it when missing.
        /// Always returns the entity's integer ID.
        /// </summary>
        private async Task<int> ResolveOrCreateEntityAsync<TEntity>(
            string trimmedName,
            Guid userId,
            IRepository<TEntity> repository,
            Func<string, TEntity> createEntity,
            Action<TEntity> afterCreate,
            string entityTypeName)
            where TEntity : class, INamedUserOwnedEntity
        {
            var existing = await repository.GetFirstOrDefaultAsync(
                e => e.UserId == userId && e.Name.ToLower() == trimmedName.ToLower());

            if (existing != null)
            {
                _logger.LogDebug("Found existing {EntityType}: {Name} (ID: {Id})", entityTypeName, existing.Name, existing.Id);
                return existing.Id;
            }

            var newEntity = createEntity(trimmedName);
            await repository.AddAsync(newEntity);
            await _unitOfWork.SaveChangesAsync();

            afterCreate(newEntity);
            _logger.LogInformation("Created new {EntityType}: {Name} (ID: {Id})", entityTypeName, newEntity.Name, newEntity.Id);
            return newEntity.Id;
        }

        /// <summary>
        /// Returns the acting user ID or throws <see cref="UnauthorizedAccessException"/> when not authenticated.
        /// </summary>
        private Guid RequireUserId()
        {
            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("User must be authenticated to resolve entities");
            }
            return userId.Value;
        }

        #endregion
    }
}
