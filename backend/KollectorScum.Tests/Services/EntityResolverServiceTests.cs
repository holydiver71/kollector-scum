using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using KollectorScum.Api.Services;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.DTOs;
using System.Linq.Expressions;

namespace KollectorScum.Tests.Services
{
    public class EntityResolverServiceTests
    {
        private readonly Mock<IRepository<Artist>> _mockArtistRepo;
        private readonly Mock<IRepository<Genre>> _mockGenreRepo;
        private readonly Mock<IRepository<Label>> _mockLabelRepo;
        private readonly Mock<IRepository<Country>> _mockCountryRepo;
        private readonly Mock<IRepository<Format>> _mockFormatRepo;
        private readonly Mock<IRepository<Packaging>> _mockPackagingRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<EntityResolverService>> _mockLogger;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly EntityResolverService _service;

        public EntityResolverServiceTests()
        {
            _mockArtistRepo = new Mock<IRepository<Artist>>();
            _mockGenreRepo = new Mock<IRepository<Genre>>();
            _mockLabelRepo = new Mock<IRepository<Label>>();
            _mockCountryRepo = new Mock<IRepository<Country>>();
            _mockFormatRepo = new Mock<IRepository<Format>>();
            _mockPackagingRepo = new Mock<IRepository<Packaging>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<EntityResolverService>>();
            _mockUserContext = new Mock<IUserContext>();
            var defaultUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");
            _mockUserContext.Setup(u => u.GetActingUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.GetUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.IsAdmin()).Returns(false);

            _service = new EntityResolverService(
                _mockArtistRepo.Object,
                _mockGenreRepo.Object,
                _mockLabelRepo.Object,
                _mockCountryRepo.Object,
                _mockFormatRepo.Object,
                _mockPackagingRepo.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockUserContext.Object
            );
        }

        #region ResolveOrCreateArtistsAsync Tests

        [Fact]
        public async Task ResolveOrCreateArtistsAsync_WithExistingIds_ReturnsExistingIds()
        {
            // Arrange
            var artistIds = new List<int> { 1, 2 };
            var createdEntities = new CreatedEntitiesDto();

            _mockArtistRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Artist { Id = 1, Name = "Artist 1" });
            _mockArtistRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Artist { Id = 2, Name = "Artist 2" });

            // Act
            var result = await _service.ResolveOrCreateArtistsAsync(artistIds, null, createdEntities);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.Null(createdEntities.Artists);
        }

        [Fact]
        public async Task ResolveOrCreateArtistsAsync_WithNewName_CreatesNewArtist()
        {
            // Arrange
            var artistNames = new List<string> { "New Artist" };
            var createdEntities = new CreatedEntitiesDto();

            _mockArtistRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Artist?)null);

            _mockArtistRepo.Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .Callback<Artist>(a => a.Id = 1)
                .ReturnsAsync((Artist a) => a);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ResolveOrCreateArtistsAsync(null, artistNames, createdEntities);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains(1, result);
            Assert.NotNull(createdEntities.Artists);
            Assert.Single(createdEntities.Artists);
            Assert.Equal("New Artist", createdEntities.Artists[0].Name);
            _mockArtistRepo.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Once);
        }

        [Fact]
        public async Task ResolveOrCreateArtistsAsync_WithExistingName_ReturnsExistingId()
        {
            // Arrange
            var artistNames = new List<string> { "Existing Artist" };
            var createdEntities = new CreatedEntitiesDto();
            var existingArtist = new Artist { Id = 5, Name = "Existing Artist" };

            _mockArtistRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(existingArtist);

            // Act
            var result = await _service.ResolveOrCreateArtistsAsync(null, artistNames, createdEntities);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains(5, result);
            Assert.Null(createdEntities.Artists);
            _mockArtistRepo.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Never);
        }

        [Fact]
        public async Task ResolveOrCreateArtistsAsync_WithMixedIdsAndNames_ProcessesBoth()
        {
            // Arrange
            var artistIds = new List<int> { 1 };
            var artistNames = new List<string> { "New Artist" };
            var createdEntities = new CreatedEntitiesDto();

            _mockArtistRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Artist { Id = 1, Name = "Existing Artist" });

            _mockArtistRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Artist?)null);

            _mockArtistRepo.Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .Callback<Artist>(a => a.Id = 2)
                .ReturnsAsync((Artist a) => a);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ResolveOrCreateArtistsAsync(artistIds, artistNames, createdEntities);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.NotNull(createdEntities.Artists);
            Assert.Single(createdEntities.Artists);
        }

        [Fact]
        public async Task ResolveOrCreateArtistsAsync_WithEmptyInputs_ReturnsNull()
        {
            // Arrange
            var createdEntities = new CreatedEntitiesDto();

            // Act
            var result = await _service.ResolveOrCreateArtistsAsync(null, null, createdEntities);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ResolveOrCreateArtistsAsync_WithWhitespaceName_TrimsAndCreates()
        {
            // Arrange
            var artistNames = new List<string> { "  Whitespace Artist  " };
            var createdEntities = new CreatedEntitiesDto();

            _mockArtistRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Artist?)null);

            _mockArtistRepo.Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .Callback<Artist>(a => 
                {
                    a.Id = 1;
                    Assert.Equal("Whitespace Artist", a.Name);
                })
                .ReturnsAsync((Artist a) => a);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ResolveOrCreateArtistsAsync(null, artistNames, createdEntities);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            _mockArtistRepo.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Once);
        }

        #endregion

        #region ResolveOrCreateGenresAsync Tests

        [Fact]
        public async Task ResolveOrCreateGenresAsync_WithExistingIds_ReturnsExistingIds()
        {
            // Arrange
            var genreIds = new List<int> { 1, 2 };
            var createdEntities = new CreatedEntitiesDto();

            _mockGenreRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Genre { Id = 1, Name = "Rock" });
            _mockGenreRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Genre { Id = 2, Name = "Pop" });

            // Act
            var result = await _service.ResolveOrCreateGenresAsync(genreIds, null, createdEntities);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.Null(createdEntities.Genres);
        }

        [Fact]
        public async Task ResolveOrCreateGenresAsync_WithNewName_CreatesNewGenre()
        {
            // Arrange
            var genreNames = new List<string> { "New Genre" };
            var createdEntities = new CreatedEntitiesDto();

            _mockGenreRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Genre, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Genre?)null);

            _mockGenreRepo.Setup(r => r.AddAsync(It.IsAny<Genre>()))
                .Callback<Genre>(g => g.Id = 1)
                .ReturnsAsync((Genre g) => g);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ResolveOrCreateGenresAsync(null, genreNames, createdEntities);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotNull(createdEntities.Genres);
            Assert.Single(createdEntities.Genres);
            Assert.Equal("New Genre", createdEntities.Genres[0].Name);
            _mockGenreRepo.Verify(r => r.AddAsync(It.IsAny<Genre>()), Times.Once);
        }

        #endregion

        #region ResolveOrCreateLabelAsync Tests

        [Fact]
        public async Task ResolveOrCreateLabelAsync_WithExistingId_ReturnsExistingId()
        {
            // Arrange
            var createdEntities = new CreatedEntitiesDto();

            _mockLabelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Label { Id = 1, Name = "Test Label" });

            // Act
            var result = await _service.ResolveOrCreateLabelAsync(1, null, createdEntities);

            // Assert
            Assert.Equal(1, result);
            Assert.Null(createdEntities.Labels);
        }

        [Fact]
        public async Task ResolveOrCreateLabelAsync_WithNewName_CreatesNewLabel()
        {
            // Arrange
            var createdEntities = new CreatedEntitiesDto();

            _mockLabelRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Label?)null);

            _mockLabelRepo.Setup(r => r.AddAsync(It.IsAny<Label>()))
                .Callback<Label>(l => l.Id = 1)
                .ReturnsAsync((Label l) => l);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ResolveOrCreateLabelAsync(null, "New Label", createdEntities);

            // Assert
            Assert.Equal(1, result);
            Assert.NotNull(createdEntities.Labels);
            Assert.Single(createdEntities.Labels);
            Assert.Equal("New Label", createdEntities.Labels[0].Name);
            _mockLabelRepo.Verify(r => r.AddAsync(It.IsAny<Label>()), Times.Once);
        }

        [Fact]
        public async Task ResolveOrCreateLabelAsync_WithNullInputs_ReturnsNull()
        {
            // Arrange
            var createdEntities = new CreatedEntitiesDto();

            // Act
            var result = await _service.ResolveOrCreateLabelAsync(null, null, createdEntities);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region ResolveOrCreateCountryAsync Tests

        [Fact]
        public async Task ResolveOrCreateCountryAsync_WithExistingId_ReturnsExistingId()
        {
            // Arrange
            var createdEntities = new CreatedEntitiesDto();

            _mockCountryRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Country { Id = 1, Name = "USA" });

            // Act
            var result = await _service.ResolveOrCreateCountryAsync(1, null, createdEntities);

            // Assert
            Assert.Equal(1, result);
            Assert.Null(createdEntities.Countries);
        }

        [Fact]
        public async Task ResolveOrCreateCountryAsync_WithNewName_CreatesNewCountry()
        {
            // Arrange
            var createdEntities = new CreatedEntitiesDto();

            _mockCountryRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Country, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Country?)null);

            _mockCountryRepo.Setup(r => r.AddAsync(It.IsAny<Country>()))
                .Callback<Country>(c => c.Id = 1)
                .ReturnsAsync((Country c) => c);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ResolveOrCreateCountryAsync(null, "Canada", createdEntities);

            // Assert
            Assert.Equal(1, result);
            Assert.NotNull(createdEntities.Countries);
            Assert.Single(createdEntities.Countries);
            Assert.Equal("Canada", createdEntities.Countries[0].Name);
            _mockCountryRepo.Verify(r => r.AddAsync(It.IsAny<Country>()), Times.Once);
        }

        #endregion

        #region ResolveOrCreateFormatAsync Tests

        [Fact]
        public async Task ResolveOrCreateFormatAsync_WithExistingId_ReturnsExistingId()
        {
            // Arrange
            var createdEntities = new CreatedEntitiesDto();

            _mockFormatRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Format { Id = 1, Name = "CD" });

            // Act
            var result = await _service.ResolveOrCreateFormatAsync(1, null, createdEntities);

            // Assert
            Assert.Equal(1, result);
            Assert.Null(createdEntities.Formats);
        }

        [Fact]
        public async Task ResolveOrCreateFormatAsync_WithNewName_CreatesNewFormat()
        {
            // Arrange
            var createdEntities = new CreatedEntitiesDto();

            _mockFormatRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Format, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Format?)null);

            _mockFormatRepo.Setup(r => r.AddAsync(It.IsAny<Format>()))
                .Callback<Format>(f => f.Id = 1)
                .ReturnsAsync((Format f) => f);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ResolveOrCreateFormatAsync(null, "Vinyl", createdEntities);

            // Assert
            Assert.Equal(1, result);
            Assert.NotNull(createdEntities.Formats);
            Assert.Single(createdEntities.Formats);
            Assert.Equal("Vinyl", createdEntities.Formats[0].Name);
            _mockFormatRepo.Verify(r => r.AddAsync(It.IsAny<Format>()), Times.Once);
        }

        #endregion

        #region ResolveOrCreatePackagingAsync Tests

        [Fact]
        public async Task ResolveOrCreatePackagingAsync_WithExistingId_ReturnsExistingId()
        {
            // Arrange
            var createdEntities = new CreatedEntitiesDto();

            _mockPackagingRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Packaging { Id = 1, Name = "Jewel Case" });

            // Act
            var result = await _service.ResolveOrCreatePackagingAsync(1, null, createdEntities);

            // Assert
            Assert.Equal(1, result);
            Assert.Null(createdEntities.Packagings);
        }

        [Fact]
        public async Task ResolveOrCreatePackagingAsync_WithNewName_CreatesNewPackaging()
        {
            // Arrange
            var createdEntities = new CreatedEntitiesDto();

            _mockPackagingRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Packaging, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Packaging?)null);

            _mockPackagingRepo.Setup(r => r.AddAsync(It.IsAny<Packaging>()))
                .Callback<Packaging>(p => p.Id = 1)
                .ReturnsAsync((Packaging p) => p);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ResolveOrCreatePackagingAsync(null, "Digipak", createdEntities);

            // Assert
            Assert.Equal(1, result);
            Assert.NotNull(createdEntities.Packagings);
            Assert.Single(createdEntities.Packagings);
            Assert.Equal("Digipak", createdEntities.Packagings[0].Name);
            _mockPackagingRepo.Verify(r => r.AddAsync(It.IsAny<Packaging>()), Times.Once);
        }

        [Fact]
        public async Task ResolveOrCreatePackagingAsync_WithNullInputs_ReturnsNull()
        {
            // Arrange
            var createdEntities = new CreatedEntitiesDto();

            // Act
            var result = await _service.ResolveOrCreatePackagingAsync(null, null, createdEntities);

            // Assert
            Assert.Null(result);
        }

        #endregion
    }
}
