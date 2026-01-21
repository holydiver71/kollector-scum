using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;
        private readonly IUserContext _userContext;
        private readonly IStorageService _storageService;

        public MusicReleaseCommandService(
            IRepository<MusicRelease> musicReleaseRepository,
            IUnitOfWork unitOfWork,
            IEntityResolverService entityResolver,
            IMusicReleaseMapperService mapper,
            IMusicReleaseValidator validator,
            ILogger<MusicReleaseCommandService> logger,
            IConfiguration configuration,
            IUserContext userContext,
            IStorageService storageService)
        {
            _musicReleaseRepository = musicReleaseRepository ?? throw new ArgumentNullException(nameof(musicReleaseRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _entityResolver = entityResolver ?? throw new ArgumentNullException(nameof(entityResolver));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
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
                var userId = _userContext.GetActingUserId();
                if (!userId.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<CreateMusicReleaseResponseDto>.Failure("User must be authenticated to create a release", ErrorType.ValidationError);
                }

                DateTime? NormalizeToUtc(DateTime? dt)
                {
                    if (!dt.HasValue) return null;
                    var d = dt.Value;
                    if (d.Kind == DateTimeKind.Utc) return d;
                    if (d.Kind == DateTimeKind.Local) return d.ToUniversalTime();
                    // Unspecified: assume input is UTC-equivalent timestamp; mark as UTC
                    return DateTime.SpecifyKind(d, DateTimeKind.Utc);
                }

                var musicRelease = new MusicRelease
                {
                    UserId = userId.Value,
                    Title = createDto.Title,
                    ReleaseYear = NormalizeToUtc(createDto.ReleaseYear),
                    OrigReleaseYear = NormalizeToUtc(createDto.OrigReleaseYear),
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
                    Images = createDto.Images != null ? JsonSerializer.Serialize(createDto.Images) : null,
                    Links = createDto.Links != null ? JsonSerializer.Serialize(createDto.Links) : null,
                    Media = createDto.Media != null ? JsonSerializer.Serialize(createDto.Media) : null,
                    DateAdded = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                // Map DTO to value object for PurchaseInfo
                if (createDto.PurchaseInfo != null)
                {
                    var purchaseInfoValueObject = new KollectorScum.Api.Models.ValueObjects.PurchaseInfo
                    {
                        StoreID = createDto.PurchaseInfo.StoreId,
                        Price = createDto.PurchaseInfo.Price,
                        Date = NormalizeToUtc(createDto.PurchaseInfo.PurchaseDate),
                        Notes = createDto.PurchaseInfo.Notes
                    };
                    musicRelease.PurchaseInfo = JsonSerializer.Serialize(purchaseInfoValueObject);
                }

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

                // Check ownership
                var userId = _userContext.GetActingUserId();
                if (userId.HasValue && existingMusicRelease.UserId != userId.Value)
                {
                    _logger.LogWarning("Access denied for music release {Id}. User {UserId} does not own this release.", id, userId);
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<MusicReleaseDto>.Failure("Access denied", ErrorType.AuthorizationError);
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
                            filter: s => s.UserId == (userId ?? Guid.Empty) && s.Name.ToLower() == storeName.ToLower());
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
                            var newStore = new Store { Name = storeName, UserId = (userId ?? Guid.Empty) };
                            await _unitOfWork.Stores.AddAsync(newStore);
                            await _unitOfWork.SaveChangesAsync(); // Save to get the ID
                            updateDto.PurchaseInfo.StoreId = newStore.Id;
                            _logger.LogInformation("Created new store: {StoreName} (ID: {StoreId})", newStore.Name, newStore.Id);
                        }
                    }
                }

                DateTime? NormalizeToUtc(DateTime? dt)
                {
                    if (!dt.HasValue) return null;
                    var d = dt.Value;
                    if (d.Kind == DateTimeKind.Utc) return d;
                    if (d.Kind == DateTimeKind.Local) return d.ToUniversalTime();
                    return DateTime.SpecifyKind(d, DateTimeKind.Utc);
                }

                // Update properties
                existingMusicRelease.Title = updateDto.Title;
                existingMusicRelease.ReleaseYear = NormalizeToUtc(updateDto.ReleaseYear);
                existingMusicRelease.OrigReleaseYear = NormalizeToUtc(updateDto.OrigReleaseYear);
                existingMusicRelease.Artists = updateDto.ArtistIds != null ? JsonSerializer.Serialize(updateDto.ArtistIds) : null;
                existingMusicRelease.Genres = updateDto.GenreIds != null ? JsonSerializer.Serialize(updateDto.GenreIds) : null;
                existingMusicRelease.Live = updateDto.Live;
                existingMusicRelease.LabelId = updateDto.LabelId;
                existingMusicRelease.CountryId = updateDto.CountryId;
                existingMusicRelease.LabelNumber = updateDto.LabelNumber;
                existingMusicRelease.LengthInSeconds = updateDto.LengthInSeconds;
                existingMusicRelease.FormatId = updateDto.FormatId;
                existingMusicRelease.PackagingId = updateDto.PackagingId;
                
                // Map DTO to value object before serialization
                if (updateDto.PurchaseInfo != null)
                {
                    _logger.LogInformation("PurchaseInfo before mapping: StoreId={StoreId}, Price={Price}, PurchaseDate={PurchaseDate}", 
                        updateDto.PurchaseInfo.StoreId, updateDto.PurchaseInfo.Price, updateDto.PurchaseInfo.PurchaseDate);
                    
                    var purchaseInfoValueObject = new KollectorScum.Api.Models.ValueObjects.PurchaseInfo
                    {
                        StoreID = updateDto.PurchaseInfo.StoreId,
                        Price = updateDto.PurchaseInfo.Price,
                        Date = NormalizeToUtc(updateDto.PurchaseInfo.PurchaseDate),
                        Notes = updateDto.PurchaseInfo.Notes
                    };
                    var serialized = JsonSerializer.Serialize(purchaseInfoValueObject);
                    _logger.LogInformation("PurchaseInfo serialized: {Json}", serialized);
                    existingMusicRelease.PurchaseInfo = serialized;
                }
                else
                {
                    _logger.LogInformation("PurchaseInfo is null, clearing field");
                    existingMusicRelease.PurchaseInfo = null;
                }
                
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

                // Check ownership
                var userId = _userContext.GetActingUserId();
                if (userId.HasValue && musicRelease.UserId != userId.Value)
                {
                    _logger.LogWarning("Access denied for music release {Id}. User {UserId} does not own this release.", id, userId);
                    return Result<bool>.Failure("Access denied", ErrorType.AuthorizationError);
                }

                // Delete associated image files before deleting the record
                await DeleteImageFilesAsync(musicRelease);

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
        /// Deletes image files associated with a music release
        /// </summary>
        private async Task DeleteImageFilesAsync(MusicRelease musicRelease)
        {
            _logger.LogInformation("DeleteImageFilesAsync called for release ID: {Id}", musicRelease.Id);
            
            if (string.IsNullOrWhiteSpace(musicRelease.Images))
            {
                _logger.LogInformation("No images to delete for release ID: {Id}", musicRelease.Id);
                return;
            }

            try
            {
                var imagesPath = _configuration["ImagesPath"] ?? "/home/andy/music-images";
                var coversPath = Path.Combine(imagesPath, "covers");
                var thumbnailsPath = Path.Combine(imagesPath, "thumbnails");
                var bucketName = _configuration["R2:BucketName"] ?? _configuration["R2__BucketName"] ?? "cover-art-staging";

                // Parse the Images JSON
                var imageData = JsonSerializer.Deserialize<MusicReleaseImageDto>(musicRelease.Images);
                if (imageData == null)
                {
                    _logger.LogWarning("Failed to deserialize images JSON for release ID: {Id}", musicRelease.Id);
                    return;
                }

                // Delete front cover
                if (!string.IsNullOrWhiteSpace(imageData.CoverFront))
                {
                    await DeleteImageAsync(imageData.CoverFront, coversPath, bucketName, musicRelease.UserId, "front cover");
                }

                // Delete back cover
                if (!string.IsNullOrWhiteSpace(imageData.CoverBack))
                {
                    await DeleteImageAsync(imageData.CoverBack, coversPath, bucketName, musicRelease.UserId, "back cover");
                }

                // Delete thumbnail (stored in separate thumbnails folder)
                if (!string.IsNullOrWhiteSpace(imageData.Thumbnail))
                {
                    await DeleteImageAsync(imageData.Thumbnail, thumbnailsPath, bucketName, musicRelease.UserId, "thumbnail");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing or deleting image files for release: {Id}", musicRelease.Id);
                // Don't fail the entire delete operation if image deletion fails
            }
        }

        /// <summary>
        /// Deletes an image from R2 storage or local filesystem
        /// </summary>
        private async Task DeleteImageAsync(string imageUrl, string localFolderPath, string bucketName, Guid userId, string imageType)
        {
            try
            {
                // Check if it's an R2/HTTPS URL
                if (imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract filename from URL
                    var filename = ExtractFilenameFromUrl(imageUrl);
                    
                    // Try to delete from R2
                    try
                    {
                        await _storageService.DeleteFileAsync(bucketName, userId.ToString(), filename);
                        _logger.LogInformation("Deleted {ImageType} from R2: {Bucket}/{UserId}/{Filename}", imageType, bucketName, userId, filename);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete {ImageType} from R2: {Filename}", imageType, filename);
                    }
                }
                else
                {
                    // Local filesystem deletion (fallback for old releases)
                    var filename = ExtractFilenameFromUrl(imageUrl);
                    var fullPath = Path.Combine(localFolderPath, filename);
                    DeleteImageFile(fullPath, imageType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting {ImageType}: {Url}", imageType, imageUrl);
            }
        }

        /// <summary>
        /// Extracts the filename from a URL (e.g., "http://localhost:5072/api/images/covers/file.jpg" -> "file.jpg")
        /// </summary>
        private string ExtractFilenameFromUrl(string urlOrFilename)
        {
            // If it's already just a filename (no protocol), return as-is
            if (!urlOrFilename.StartsWith("http://") && !urlOrFilename.StartsWith("https://"))
            {
                return urlOrFilename;
            }

            // Extract filename from URL
            try
            {
                var uri = new Uri(urlOrFilename);
                return Path.GetFileName(uri.LocalPath);
            }
            catch
            {
                // If URL parsing fails, try to get the part after the last slash
                var lastSlashIndex = urlOrFilename.LastIndexOf('/');
                return lastSlashIndex >= 0 ? urlOrFilename.Substring(lastSlashIndex + 1) : urlOrFilename;
            }
        }

        /// <summary>
        /// Helper method to delete a single image file
        /// </summary>
        private void DeleteImageFile(string fullPath, string imageType)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted {ImageType} file: {FilePath}", imageType, fullPath);
                }
                else
                {
                    _logger.LogDebug("{ImageType} file not found (skipping): {FilePath}", imageType, fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete {ImageType} file: {FilePath}", imageType, fullPath);
                // Continue even if one file fails to delete
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
