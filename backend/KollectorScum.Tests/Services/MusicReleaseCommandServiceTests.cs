using System.Linq.Expressions;
using System.Text.Json;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for MusicReleaseCommandService.
    /// Tests for Create, Update, and Delete operations including image file handling.
    /// </summary>
    public class MusicReleaseCommandServiceTests : IDisposable
    {
        private readonly Guid defaultUserId;
        private readonly Mock<IRepository<MusicRelease>> _mockMusicReleaseRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRepository<Store>> _mockStoreRepo;
        private readonly Mock<IEntityResolverService> _mockEntityResolver;
        private readonly Mock<IMusicReleaseMapperService> _mockMapper;
        private readonly Mock<IMusicReleaseValidator> _mockValidator;
        private readonly Mock<ILogger<MusicReleaseCommandService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly Mock<IStorageService> _mockStorageService;
        private readonly MusicReleaseCommandService _service;
        private readonly string _testImagesPath;

        /// <summary>
        /// Initialises shared mocks and a real <see cref="MusicReleaseCommandService"/> instance.
        /// </summary>
        public MusicReleaseCommandServiceTests()
        {
            _mockMusicReleaseRepo = new Mock<IRepository<MusicRelease>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockStoreRepo = new Mock<IRepository<Store>>();
            _mockEntityResolver = new Mock<IEntityResolverService>();
            _mockMapper = new Mock<IMusicReleaseMapperService>();
            _mockValidator = new Mock<IMusicReleaseValidator>();
            _mockLogger = new Mock<ILogger<MusicReleaseCommandService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockUserContext = new Mock<IUserContext>();
            _mockStorageService = new Mock<IStorageService>();
            defaultUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");
            _mockUserContext.Setup(u => u.GetActingUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.GetUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.IsAdmin()).Returns(false);

            // Wire the Stores repository through the UnitOfWork
            _mockUnitOfWork.Setup(u => u.Stores).Returns(_mockStoreRepo.Object);

            // Setup test directory for images
            _testImagesPath = Path.Combine(Path.GetTempPath(), "kollector-test-images", Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(_testImagesPath, "covers"));
            Directory.CreateDirectory(Path.Combine(_testImagesPath, "thumbnails"));

            // Setup configuration to use test path
            _mockConfiguration.Setup(c => c["ImagesPath"]).Returns(_testImagesPath);

            _service = new MusicReleaseCommandService(
                _mockMusicReleaseRepo.Object,
                _mockUnitOfWork.Object,
                _mockEntityResolver.Object,
                _mockMapper.Object,
                _mockValidator.Object,
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockUserContext.Object,
                _mockStorageService.Object
            );
        }

        /// <summary>Cleans up the temporary image directory after each test.</summary>
        public void Dispose()
        {
            // Cleanup test directory
            if (Directory.Exists(_testImagesPath))
            {
                Directory.Delete(_testImagesPath, true);
            }
        }

        // ── Shared helper ──────────────────────────────────────────────────────

        /// <summary>
        /// Sets up all entity resolver mocks to return default empty/null values so
        /// Create tests only need to override the specific resolver they care about.
        /// </summary>
        private void SetupDefaultEntityResolvers()
        {
            _mockEntityResolver.Setup(e => e.ResolveOrCreateArtistsAsync(
                    It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int>());

            _mockEntityResolver.Setup(e => e.ResolveOrCreateGenresAsync(
                    It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int>());

            _mockEntityResolver.Setup(e => e.ResolveOrCreateLabelAsync(
                    It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync((int?)null);

            _mockEntityResolver.Setup(e => e.ResolveOrCreateCountryAsync(
                    It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync((int?)null);

            _mockEntityResolver.Setup(e => e.ResolveOrCreateFormatAsync(
                    It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync((int?)null);

            _mockEntityResolver.Setup(e => e.ResolveOrCreatePackagingAsync(
                    It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync((int?)null);
        }

        /// <summary>
        /// Sets up the validator to report a passing create validation (no duplicates).
        /// </summary>
        private void SetupValidCreateValidation()
        {
            _mockValidator
                .Setup(v => v.ValidateCreateAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ReturnsAsync((true, (string?)null, (List<MusicReleaseSummaryDto>?)null));
        }

        /// <summary>
        /// Sets up the validator to report a passing update validation.
        /// </summary>
        private void SetupValidUpdateValidation()
        {
            _mockValidator
                .Setup(v => v.ValidateUpdateAsync(It.IsAny<int>(), It.IsAny<UpdateMusicReleaseDto>()))
                .ReturnsAsync((true, (string?)null));
        }

        #region CreateMusicReleaseAsync Tests

        /// <summary>
        /// Mirrors legacy test: CreateMusicReleaseAsync_WithValidData_CreatesRelease.
        /// Creates a release with all IDs pre-resolved; verifies AddAsync and Commit are called.
        /// </summary>
        [Fact]
        public async Task CreateMusicReleaseAsync_WithValidData_CreatesRelease()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "New Album",
                ReleaseYear = new DateTime(2023, 1, 1),
                ArtistIds = new List<int> { 1 },
                GenreIds = new List<int> { 1 },
                LabelId = 1,
                CountryId = 1,
                FormatId = 1
            };

            SetupDefaultEntityResolvers();

            _mockEntityResolver.Setup(e => e.ResolveOrCreateArtistsAsync(
                    It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int> { 1 });

            _mockEntityResolver.Setup(e => e.ResolveOrCreateGenresAsync(
                    It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int> { 1 });

            _mockEntityResolver.Setup(e => e.ResolveOrCreateLabelAsync(
                    It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(1);

            _mockEntityResolver.Setup(e => e.ResolveOrCreateCountryAsync(
                    It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(1);

            _mockEntityResolver.Setup(e => e.ResolveOrCreateFormatAsync(
                    It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(1);

            SetupValidCreateValidation();

            _mockMusicReleaseRepo.Setup(r => r.AddAsync(It.IsAny<MusicRelease>()))
                .Callback<MusicRelease>(mr => mr.Id = 1)
                .ReturnsAsync((MusicRelease mr) => mr);

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, Title = "New Album" });

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateMusicReleaseAsync(createDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.Value!.Release);
            Assert.Equal("New Album", result.Value.Release.Title);
            _mockMusicReleaseRepo.Verify(r => r.AddAsync(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        /// <summary>
        /// Mirrors legacy test: CreateMusicReleaseAsync_WithNewArtistName_CreatesArtistAndRelease.
        /// Verifies the entity resolver is called when ArtistNames (not IDs) are supplied.
        /// </summary>
        [Fact]
        public async Task CreateMusicReleaseAsync_WithNewArtistName_CreatesArtistAndRelease()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "New Album",
                ReleaseYear = new DateTime(2023, 1, 1),
                ArtistNames = new List<string> { "New Artist" },
                GenreIds = new List<int> { 1 },
                LabelId = 1
            };

            SetupDefaultEntityResolvers();

            _mockEntityResolver.Setup(e => e.ResolveOrCreateArtistsAsync(
                    It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .Callback<List<int>?, List<string>?, CreatedEntitiesDto>((ids, names, created) =>
                {
                    created.Artists = new List<ArtistDto> { new ArtistDto { Id = 1, Name = "New Artist" } };
                })
                .ReturnsAsync(new List<int> { 1 });

            _mockEntityResolver.Setup(e => e.ResolveOrCreateGenresAsync(
                    It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int> { 1 });

            _mockEntityResolver.Setup(e => e.ResolveOrCreateLabelAsync(
                    It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(1);

            SetupValidCreateValidation();

            _mockMusicReleaseRepo.Setup(r => r.AddAsync(It.IsAny<MusicRelease>()))
                .Callback<MusicRelease>(mr => mr.Id = 1)
                .ReturnsAsync((MusicRelease mr) => mr);

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, Title = "New Album" });

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateMusicReleaseAsync(createDto);

            // Assert
            Assert.True(result.IsSuccess);
            _mockEntityResolver.Verify(e => e.ResolveOrCreateArtistsAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()), Times.Once);
        }

        /// <summary>
        /// Mirrors legacy test: CreateMusicReleaseAsync_WithDuplicateCatalogNumber_ThrowsArgumentException.
        /// When the validator returns a duplicate, the command service returns a DuplicateError result
        /// and rolls back the transaction.
        /// </summary>
        [Fact]
        public async Task CreateMusicReleaseAsync_WithDuplicateCatalogNumber_ReturnsDuplicateError()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Duplicate Album",
                ReleaseYear = new DateTime(2023, 1, 1),
                LabelNumber = "CATALOG001",
                ArtistIds = new List<int> { 1 }
            };

            SetupDefaultEntityResolvers();

            _mockEntityResolver.Setup(e => e.ResolveOrCreateArtistsAsync(
                    It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int> { 1 });

            // Validator reports a duplicate
            var duplicate = new MusicReleaseSummaryDto { Id = 1, Title = "Existing Album" };
            _mockValidator
                .Setup(v => v.ValidateCreateAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ReturnsAsync((false, "Duplicate release found", new List<MusicReleaseSummaryDto> { duplicate }));

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateMusicReleaseAsync(createDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.DuplicateError, result.ErrorType);
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.AtLeastOnce);
            _mockMusicReleaseRepo.Verify(r => r.AddAsync(It.IsAny<MusicRelease>()), Times.Never);
        }

        /// <summary>
        /// Mirrors legacy test: CreateMusicReleaseAsync_WithNewGenreName_CreatesGenreAndRelease.
        /// Verifies the entity resolver is called to create a new genre by name.
        /// </summary>
        [Fact]
        public async Task CreateMusicReleaseAsync_WithNewGenreName_CreatesGenreAndRelease()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Genre Album",
                GenreNames = new List<string> { "New Genre" }
            };

            SetupDefaultEntityResolvers();

            _mockEntityResolver.Setup(e => e.ResolveOrCreateGenresAsync(
                    It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .Callback<List<int>?, List<string>?, CreatedEntitiesDto>((ids, names, created) =>
                {
                    created.Genres = new List<GenreDto> { new GenreDto { Id = 5, Name = "New Genre" } };
                })
                .ReturnsAsync(new List<int> { 5 });

            SetupValidCreateValidation();

            _mockMusicReleaseRepo.Setup(r => r.AddAsync(It.IsAny<MusicRelease>()))
                .Callback<MusicRelease>(mr => mr.Id = 10)
                .ReturnsAsync((MusicRelease mr) => mr);

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 10, Title = "Genre Album" });

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateMusicReleaseAsync(createDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value!.Created?.Genres);
            Assert.Single(result.Value.Created!.Genres!);
            Assert.Equal("New Genre", result.Value.Created.Genres[0].Name);
            _mockEntityResolver.Verify(e => e.ResolveOrCreateGenresAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()), Times.Once);
        }

        #endregion

        #region UpdateMusicReleaseAsync Tests

        /// <summary>
        /// Mirrors legacy test: UpdateMusicReleaseAsync_WithValidData_UpdatesRelease.
        /// Verifies Update and SaveChanges are called, and the returned Result is successful.
        /// </summary>
        [Fact]
        public async Task UpdateMusicReleaseAsync_WithValidData_UpdatesRelease()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Updated Album",
                ReleaseYear = new DateTime(2023, 1, 1),
                ArtistIds = new List<int> { 1 },
                GenreIds = new List<int> { 1 }
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Old Album",
                UserId = defaultUserId,
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);

            SetupValidUpdateValidation();

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, Title = "Updated Album" });

            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("Updated Album", result.Value!.Title);
            _mockMusicReleaseRepo.Verify(r => r.Update(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        /// <summary>
        /// Mirrors legacy test: UpdateMusicReleaseAsync_WithInvalidId_ReturnsNull.
        /// When the release does not exist the command service returns a NotFound result.
        /// </summary>
        [Fact]
        public async Task UpdateMusicReleaseAsync_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto { Title = "Updated Album" };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((MusicRelease?)null);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateMusicReleaseAsync(999, updateDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        /// <summary>
        /// Mirrors legacy test: UpdateMusicReleaseAsync_WithNewStoreName_CreatesNewStore.
        /// When a StoreName is provided without a StoreId the service creates a new store.
        /// </summary>
        [Fact]
        public async Task UpdateMusicReleaseAsync_WithNewStoreName_CreatesNewStore()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album with Purchase Info",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreName = "New Record Store",
                    Price = 25.99m,
                    PurchaseDate = new DateTime(2023, 10, 15)
                }
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album",
                UserId = defaultUserId,
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            Store? createdStore = null;

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);

            _mockStoreRepo.Setup(s => s.GetAsync(
                    It.IsAny<Expression<Func<Store, bool>>>(),
                    It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new List<Store>());   // No existing stores

            _mockStoreRepo.Setup(s => s.AddAsync(It.IsAny<Store>()))
                .Callback<Store>(s =>
                {
                    s.Id = 100;
                    createdStore = s;
                })
                .ReturnsAsync((Store s) => s);

            SetupValidUpdateValidation();

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto
                {
                    Id = 1,
                    Title = "Album with Purchase Info",
                    PurchaseInfo = new MusicReleasePurchaseInfoDto { StoreId = 100, StoreName = "New Record Store", Price = 25.99m }
                });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value!.PurchaseInfo);
            Assert.Equal(100, result.Value.PurchaseInfo!.StoreId);
            Assert.NotNull(createdStore);
            Assert.Equal("New Record Store", createdStore!.Name);
            _mockStoreRepo.Verify(s => s.AddAsync(It.IsAny<Store>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeast(2));
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        /// <summary>
        /// Mirrors legacy test: UpdateMusicReleaseAsync_WithExistingStoreName_ReusesExistingStore.
        /// When a StoreName matches an existing store no new store is created.
        /// </summary>
        [Fact]
        public async Task UpdateMusicReleaseAsync_WithExistingStoreName_ReusesExistingStore()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album with Purchase Info",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreName = "Existing Record Store",
                    Price = 19.99m
                }
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album",
                UserId = defaultUserId,
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            var existingStore = new Store { Id = 50, Name = "Existing Record Store" };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);

            _mockStoreRepo.Setup(s => s.GetAsync(
                    It.IsAny<Expression<Func<Store, bool>>>(),
                    It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new List<Store> { existingStore });

            SetupValidUpdateValidation();

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto
                {
                    Id = 1,
                    Title = "Album with Purchase Info",
                    PurchaseInfo = new MusicReleasePurchaseInfoDto { StoreId = 50, StoreName = "Existing Record Store", Price = 19.99m }
                });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(50, result.Value!.PurchaseInfo!.StoreId);
            _mockStoreRepo.Verify(s => s.AddAsync(It.IsAny<Store>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        /// <summary>
        /// Mirrors legacy test: UpdateMusicReleaseAsync_WithStoreNameCaseInsensitive_ReusesExistingStore.
        /// The store lookup is case-insensitive; a match with different casing still reuses the store.
        /// </summary>
        [Fact]
        public async Task UpdateMusicReleaseAsync_WithStoreNameCaseInsensitive_ReusesExistingStore()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreName = "EXISTING RECORD STORE",   // Different case
                    Price = 15.99m
                }
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Album", UserId = defaultUserId };
            var existingStore = new Store { Id = 50, Name = "Existing Record Store" };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);

            _mockStoreRepo.Setup(s => s.GetAsync(
                    It.IsAny<Expression<Func<Store, bool>>>(),
                    It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new List<Store> { existingStore });

            SetupValidUpdateValidation();

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, PurchaseInfo = new MusicReleasePurchaseInfoDto { StoreId = 50 } });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(50, result.Value!.PurchaseInfo!.StoreId);
            _mockStoreRepo.Verify(s => s.AddAsync(It.IsAny<Store>()), Times.Never);
        }

        /// <summary>
        /// Mirrors legacy test: UpdateMusicReleaseAsync_WithStoreId_UsesProvidedStoreId.
        /// When StoreId is already set the service skips the store-name lookup entirely.
        /// </summary>
        [Fact]
        public async Task UpdateMusicReleaseAsync_WithStoreId_UsesProvidedStoreId()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreId = 75,
                    StoreName = "Should Be Ignored",
                    Price = 20.00m
                }
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Album", UserId = defaultUserId };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);

            SetupValidUpdateValidation();

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, PurchaseInfo = new MusicReleasePurchaseInfoDto { StoreId = 75 } });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(75, result.Value!.PurchaseInfo!.StoreId);
            _mockStoreRepo.Verify(s => s.GetAsync(
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()), Times.Never);
            _mockStoreRepo.Verify(s => s.AddAsync(It.IsAny<Store>()), Times.Never);
        }

        /// <summary>
        /// Mirrors legacy test: UpdateMusicReleaseAsync_WithWhitespaceStoreName_TrimsWhitespace.
        /// Leading/trailing whitespace in the StoreName is trimmed before the store is created.
        /// </summary>
        [Fact]
        public async Task UpdateMusicReleaseAsync_WithWhitespaceStoreName_TrimsWhitespace()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album",
                PurchaseInfo = new MusicReleasePurchaseInfoDto { StoreName = "  Trimmed Store  ", Price = 12.99m }
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Album", UserId = defaultUserId };
            Store? createdStore = null;

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);

            _mockStoreRepo.Setup(s => s.GetAsync(
                    It.IsAny<Expression<Func<Store, bool>>>(),
                    It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new List<Store>());

            _mockStoreRepo.Setup(s => s.AddAsync(It.IsAny<Store>()))
                .Callback<Store>(s => { s.Id = 101; createdStore = s; })
                .ReturnsAsync((Store s) => s);

            SetupValidUpdateValidation();

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, PurchaseInfo = new MusicReleasePurchaseInfoDto { StoreId = 101 } });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(createdStore);
            Assert.Equal("Trimmed Store", createdStore!.Name);
        }

        /// <summary>
        /// Mirrors legacy test: UpdateMusicReleaseAsync_WithoutPurchaseInfo_UpdatesSuccessfully.
        /// When PurchaseInfo is null the store-lookup code is never reached.
        /// </summary>
        [Fact]
        public async Task UpdateMusicReleaseAsync_WithoutPurchaseInfo_UpdatesSuccessfully()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album Without Purchase Info",
                ReleaseYear = new DateTime(2023, 1, 1)
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Old Title", UserId = defaultUserId };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);

            SetupValidUpdateValidation();

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, Title = "Album Without Purchase Info" });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.Value!.PurchaseInfo);
            _mockStoreRepo.Verify(s => s.GetAsync(
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Mirrors legacy test: UpdateMusicReleaseAsync_OnException_RollsBackTransaction.
        /// When the store lookup throws the service catches the exception, rolls back,
        /// and returns a DatabaseError result.
        /// </summary>
        [Fact]
        public async Task UpdateMusicReleaseAsync_OnException_RollsBackTransaction()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album",
                PurchaseInfo = new MusicReleasePurchaseInfoDto { StoreName = "Test Store" }
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Album", UserId = defaultUserId };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _mockStoreRepo.Setup(s => s.GetAsync(
                    It.IsAny<Expression<Func<Store, bool>>>(),
                    It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            SetupValidUpdateValidation();

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.DatabaseError, result.ErrorType);
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        /// <summary>
        /// Mirrors legacy test: UpdateMusicReleaseAsync_WithEmptyStoreName_IgnoresStoreCreation.
        /// A whitespace-only StoreName (after trimming it is empty) skips store creation.
        /// </summary>
        [Fact]
        public async Task UpdateMusicReleaseAsync_WithEmptyStoreName_IgnoresStoreCreation()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreName = "   ",   // Whitespace only
                    Price = 10.00m
                }
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Album", UserId = defaultUserId };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);

            SetupValidUpdateValidation();

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, PurchaseInfo = new MusicReleasePurchaseInfoDto { Price = 10.00m } });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.True(result.IsSuccess);
            _mockStoreRepo.Verify(s => s.GetAsync(
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()), Times.Never);
            _mockStoreRepo.Verify(s => s.AddAsync(It.IsAny<Store>()), Times.Never);
        }

        #endregion

        #region DeleteMusicReleaseAsync Tests

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithValidId_DeletesReleaseAndImages()
        {
            // Arrange
            var frontCoverFile = "test-front.jpg";
            var backCoverFile = "test-back.jpg";
            var thumbnailFile = "test-thumb.jpg";

            var imageDto = new MusicReleaseImageDto
            {
                CoverFront = frontCoverFile,
                CoverBack = backCoverFile,
                Thumbnail = thumbnailFile
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album to Delete",
                Images = JsonSerializer.Serialize(imageDto),
                UserId = defaultUserId
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);

            // Verify database operations
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);

            // Verify image files were deleted via the storage service
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), defaultUserId.ToString(), frontCoverFile), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), defaultUserId.ToString(), backCoverFile), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), defaultUserId.ToString(), thumbnailFile), Times.Once);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithNoImages_DeletesReleaseSuccessfully()
        {
            // Arrange
            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album without Images",
                Images = null,
                UserId = defaultUserId
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithPartialImages_DeletesExistingImages()
        {
            // Arrange
            var frontCoverFile = "test-front-only.jpg";

            var imageDto = new MusicReleaseImageDto
            {
                CoverFront = frontCoverFile,
                CoverBack = null,  // No back cover
                Thumbnail = null   // No thumbnail
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album with Partial Images",
                Images = JsonSerializer.Serialize(imageDto),
                UserId = defaultUserId
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);

            // Verify only the front cover was deleted
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), defaultUserId.ToString(), frontCoverFile), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithMissingImageFiles_DeletesReleaseSuccessfully()
        {
            // Arrange - Release has image metadata but files don't exist
            var imageDto = new MusicReleaseImageDto
            {
                CoverFront = "nonexistent-front.jpg",
                CoverBack = "nonexistent-back.jpg",
                Thumbnail = "nonexistent-thumb.jpg"
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album with Missing Image Files",
                Images = JsonSerializer.Serialize(imageDto),
                UserId = defaultUserId
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert - Should succeed even though files don't exist
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithInvalidImageJson_DeletesReleaseSuccessfully()
        {
            // Arrange - Release has invalid JSON in Images field
            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album with Invalid Image JSON",
                Images = "invalid json {]",
                UserId = defaultUserId
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert - Should succeed even with invalid JSON
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithNonexistentId_ReturnsNotFound()
        {
            // Arrange
            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((MusicRelease?)null);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(999);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WhenDatabaseThrowsException_ReturnsFailure()
        {
            // Arrange
            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album to Delete"
                ,
                UserId = defaultUserId
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.DatabaseError, result.ErrorType);
            Assert.Contains("Database error", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithLockedImageFile_StillDeletesRelease()
        {
            // Arrange – storage service throws when deleting the image,
            // but the release should still be deleted successfully.
            var frontCoverFile = "locked-file.jpg";

            _mockStorageService
                .Setup(s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>(), frontCoverFile))
                .ThrowsAsync(new IOException("File is locked"));

            var imageDto = new MusicReleaseImageDto
            {
                CoverFront = frontCoverFile
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album with Locked Image",
                Images = JsonSerializer.Serialize(imageDto),
                UserId = defaultUserId
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert – Should succeed even if image deletion fails
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithAllThreeImageTypes_DeletesAllFiles()
        {
            // Arrange
            var frontCoverFile = "album-front.jpg";
            var backCoverFile = "album-back.jpg";
            var thumbnailFile = "album-thumb.jpg";

            var imageDto = new MusicReleaseImageDto
            {
                CoverFront = frontCoverFile,
                CoverBack = backCoverFile,
                Thumbnail = thumbnailFile
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Complete Album",
                Images = JsonSerializer.Serialize(imageDto),
                UserId = defaultUserId
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify all three image types were deleted via the storage service
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), defaultUserId.ToString(), frontCoverFile), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), defaultUserId.ToString(), backCoverFile), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), defaultUserId.ToString(), thumbnailFile), Times.Once);
        }

        #endregion
    }
}
