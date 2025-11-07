using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for music release write operations (commands)
    /// Handles: Create, Update, Delete operations with validation
    /// </summary>
    public class MusicReleaseCommandService : IMusicReleaseCommandService
    {
        private readonly IRepository<MusicRelease> _musicReleaseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEntityResolverService _entityResolver;
        private readonly IMusicReleaseMapperService _mapper;
        private readonly IMusicReleaseValidator _validator;
        private readonly ILogger<MusicReleaseCommandService> _logger;

        public MusicReleaseCommandService(
            IRepository<MusicRelease> musicReleaseRepository,
            IUnitOfWork unitOfWork,
            IEntityResolverService entityResolver,
            IMusicReleaseMapperService mapper,
            IMusicReleaseValidator validator,
            ILogger<MusicReleaseCommandService> logger)
        {
            _musicReleaseRepository = musicReleaseRepository ?? throw new ArgumentNullException(nameof(musicReleaseRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _entityResolver = entityResolver ?? throw new ArgumentNullException(nameof(entityResolver));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<CreateMusicReleaseResponseDto>> CreateMusicReleaseAsync(CreateMusicReleaseDto createDto)
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

                // Validate and check for duplicates
                var validationResult = await _validator.ValidateCreateAsync(createDto);
                if (!validationResult.IsValid)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    
                    // Determine error type based on validation result
                    var errorType = validationResult.Duplicates?.Any() == true 
                        ? ErrorType.DuplicateError 
                        : ErrorType.ValidationError;
                    
                    return Result<CreateMusicReleaseResponseDto>.Failure(validationResult.ErrorMessage ?? "Validation failed", errorType);
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
                var response = new CreateMusicReleaseResponseDto
                {
                    Release = createdDto,
                    Created = HasCreatedEntities(createdEntities) ? createdEntities : null
                };
                
                return Result<CreateMusicReleaseResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating music release: {Title}", createDto.Title);
                return Result<CreateMusicReleaseResponseDto>.Failure($"An error occurred while creating the music release: {ex.Message}", ErrorType.DatabaseError);
            }
        }

        public async Task<Result<MusicReleaseDto>> UpdateMusicReleaseAsync(int id, UpdateMusicReleaseDto updateDto)
        {
            _logger.LogInformation("Updating music release: {Id}", id);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Check if exists first
                var existingMusicRelease = await _musicReleaseRepository.GetByIdAsync(id);
                if (existingMusicRelease == null)
                {
                    _logger.LogWarning("Music release not found: {Id}", id);
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<MusicReleaseDto>.NotFound("Music release", id);
                }

                // Validate update
                var validationResult = await _validator.ValidateUpdateAsync(id, updateDto);
                if (!validationResult.IsValid)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<MusicReleaseDto>.ValidationError(validationResult.ErrorMessage ?? "Validation failed");
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

                var updatedDto = await _mapper.MapToFullDtoAsync(existingMusicRelease);
                return Result<MusicReleaseDto>.Success(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating music release: {Id}", id);
                await _unitOfWork.RollbackTransactionAsync();
                return Result<MusicReleaseDto>.Failure($"An error occurred while updating the music release: {ex.Message}", ErrorType.DatabaseError);
            }
        }

        public async Task<Result<bool>> DeleteMusicReleaseAsync(int id)
        {
            _logger.LogInformation("Deleting music release: {Id}", id);

            try
            {
                var musicRelease = await _musicReleaseRepository.GetByIdAsync(id);
                if (musicRelease == null)
                {
                    _logger.LogWarning("Music release not found: {Id}", id);
                    return Result<bool>.NotFound("Music release", id);
                }

                _musicReleaseRepository.Delete(musicRelease);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Music release deleted successfully: {Id}", id);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting music release: {Id}", id);
                return Result<bool>.Failure($"An error occurred while deleting the music release: {ex.Message}", ErrorType.DatabaseError);
            }
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
