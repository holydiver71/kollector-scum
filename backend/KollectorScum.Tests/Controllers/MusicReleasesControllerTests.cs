using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Unit tests for MusicReleasesController focusing on Section 2.3 auto-creation functionality
    /// Target: 80%+ code coverage for auto-creation features
    /// </summary>
    public class MusicReleasesControllerTests
    {
        private readonly Mock<IRepository<MusicRelease>> _mockMusicReleaseRepository;
        private readonly Mock<IRepository<Artist>> _mockArtistRepository;
        private readonly Mock<IRepository<Genre>> _mockGenreRepository;
        private readonly Mock<IRepository<Label>> _mockLabelRepository;
        private readonly Mock<IRepository<Country>> _mockCountryRepository;
        private readonly Mock<IRepository<Format>> _mockFormatRepository;
        private readonly Mock<IRepository<Packaging>> _mockPackagingRepository;
        private readonly Mock<IRepository<Store>> _mockStoreRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<MusicReleasesController>> _mockLogger;
        private readonly MusicReleasesController _controller;

        public MusicReleasesControllerTests()
        {
            _mockMusicReleaseRepository = new Mock<IRepository<MusicRelease>>();
            _mockArtistRepository = new Mock<IRepository<Artist>>();
            _mockGenreRepository = new Mock<IRepository<Genre>>();
            _mockLabelRepository = new Mock<IRepository<Label>>();
            _mockCountryRepository = new Mock<IRepository<Country>>();
            _mockFormatRepository = new Mock<IRepository<Format>>();
            _mockPackagingRepository = new Mock<IRepository<Packaging>>();
            _mockStoreRepository = new Mock<IRepository<Store>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<MusicReleasesController>>();

            _controller = new MusicReleasesController(
                _mockMusicReleaseRepository.Object,
                _mockArtistRepository.Object,
                _mockGenreRepository.Object,
                _mockLabelRepository.Object,
                _mockCountryRepository.Object,
                _mockFormatRepository.Object,
                _mockPackagingRepository.Object,
                _mockStoreRepository.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        #region CreateMusicRelease - Basic Scenarios

        [Fact]
        public async Task CreateMusicRelease_WithExistingArtistIds_DoesNotCreateNewArtists()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1, 2 },
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Release);
            Assert.Equal("Test Album", response.Release.Title);
            Assert.Null(response.Created?.Artists); // No artists were created
            
            _mockArtistRepository.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Never);
        }

        [Fact]
        public async Task CreateMusicRelease_WithNewArtistNames_CreatesNewArtists()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistNames = new List<string> { "New Artist 1", "New Artist 2" },
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            // Artist doesn't exist
            _mockArtistRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Artist?)null);

            // Setup AddAsync to set ID
            var artistIdCounter = 100;
            _mockArtistRepository
                .Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .Callback<Artist>(a => a.Id = artistIdCounter++)
                .ReturnsAsync((Artist a) => a);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Created?.Artists);
            Assert.Equal(2, response.Created.Artists.Count);
            Assert.Equal("New Artist 1", response.Created.Artists[0].Name);
            Assert.Equal("New Artist 2", response.Created.Artists[1].Name);
            
            _mockArtistRepository.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateMusicRelease_WithMixedArtistIdsAndNames_UsesExistingAndCreatesNew()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1 },
                ArtistNames = new List<string> { "New Artist" },
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            // Artist doesn't exist
            _mockArtistRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Artist?)null);

            _mockArtistRepository
                .Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .Callback<Artist>(a => a.Id = 100)
                .ReturnsAsync((Artist a) => a);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Created?.Artists);
            Assert.Single(response.Created.Artists); // Only new artist in created list
            Assert.Equal("New Artist", response.Created.Artists[0].Name);
            
            _mockArtistRepository.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Once);
        }

        [Fact]
        public async Task CreateMusicRelease_WithExistingArtistName_ReusesExistingArtist()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistNames = new List<string> { "Existing Artist" },
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            var existingArtist = new Artist { Id = 50, Name = "Existing Artist" };
            _mockArtistRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(existingArtist);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.Null(response.Created?.Artists); // No new artists created
            
            _mockArtistRepository.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Never);
        }

        #endregion

        #region CreateMusicRelease - Genre Auto-Creation

        [Fact]
        public async Task CreateMusicRelease_WithNewGenreNames_CreatesNewGenres()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                GenreNames = new List<string> { "Progressive Metal", "Death Metal" },
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            _mockGenreRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Genre, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Genre?)null);

            var genreIdCounter = 200;
            _mockGenreRepository
                .Setup(r => r.AddAsync(It.IsAny<Genre>()))
                .Callback<Genre>(g => g.Id = genreIdCounter++)
                .ReturnsAsync((Genre g) => g);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Created?.Genres);
            Assert.Equal(2, response.Created.Genres.Count);
            
            _mockGenreRepository.Verify(r => r.AddAsync(It.IsAny<Genre>()), Times.Exactly(2));
        }

        #endregion

        #region CreateMusicRelease - Label Auto-Creation

        [Fact]
        public async Task CreateMusicRelease_WithNewLabelName_CreatesNewLabel()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                LabelName = "New Record Label",
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            _mockLabelRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Label?)null);

            _mockLabelRepository
                .Setup(r => r.AddAsync(It.IsAny<Label>()))
                .Callback<Label>(l => l.Id = 300)
                .ReturnsAsync((Label l) => l);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Created?.Labels);
            Assert.Single(response.Created.Labels);
            Assert.Equal("New Record Label", response.Created.Labels[0].Name);
            
            _mockLabelRepository.Verify(r => r.AddAsync(It.IsAny<Label>()), Times.Once);
        }

        [Fact]
        public async Task CreateMusicRelease_WithExistingLabelName_ReusesExistingLabel()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                LabelName = "Columbia Records",
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            var existingLabel = new Label { Id = 10, Name = "Columbia Records" };
            _mockLabelRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(existingLabel);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.Null(response.Created?.Labels);
            
            _mockLabelRepository.Verify(r => r.AddAsync(It.IsAny<Label>()), Times.Never);
        }

        #endregion

        #region CreateMusicRelease - Country, Format, Packaging Auto-Creation

        [Fact]
        public async Task CreateMusicRelease_WithNewCountryName_CreatesNewCountry()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                CountryName = "Germany",
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            _mockCountryRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Country, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Country?)null);

            _mockCountryRepository
                .Setup(r => r.AddAsync(It.IsAny<Country>()))
                .Callback<Country>(c => c.Id = 400)
                .ReturnsAsync((Country c) => c);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Created?.Countries);
            Assert.Single(response.Created.Countries);
            
            _mockCountryRepository.Verify(r => r.AddAsync(It.IsAny<Country>()), Times.Once);
        }

        [Fact]
        public async Task CreateMusicRelease_WithNewFormatName_CreatesNewFormat()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                FormatName = "Cassette",
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            _mockFormatRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Format, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Format?)null);

            _mockFormatRepository
                .Setup(r => r.AddAsync(It.IsAny<Format>()))
                .Callback<Format>(f => f.Id = 500)
                .ReturnsAsync((Format f) => f);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Created?.Formats);
            Assert.Single(response.Created.Formats);
            
            _mockFormatRepository.Verify(r => r.AddAsync(It.IsAny<Format>()), Times.Once);
        }

        [Fact]
        public async Task CreateMusicRelease_WithNewPackagingName_CreatesNewPackaging()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                PackagingName = "Digipak",
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            _mockPackagingRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Packaging, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Packaging?)null);

            _mockPackagingRepository
                .Setup(r => r.AddAsync(It.IsAny<Packaging>()))
                .Callback<Packaging>(p => p.Id = 600)
                .ReturnsAsync((Packaging p) => p);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Created?.Packagings);
            Assert.Single(response.Created.Packagings);
            
            _mockPackagingRepository.Verify(r => r.AddAsync(It.IsAny<Packaging>()), Times.Once);
        }

        #endregion

        #region CreateMusicRelease - Multiple Auto-Creations

        [Fact]
        public async Task CreateMusicRelease_WithAllNewLookupEntities_CreatesAllEntities()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistNames = new List<string> { "New Artist" },
                GenreNames = new List<string> { "New Genre" },
                LabelName = "New Label",
                CountryName = "New Country",
                FormatName = "New Format",
                PackagingName = "New Packaging",
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            // All lookups return null (not found)
            _mockArtistRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Artist?)null);
            _mockGenreRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Genre, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Genre?)null);
            _mockLabelRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Label?)null);
            _mockCountryRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Country, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Country?)null);
            _mockFormatRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Format, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Format?)null);
            _mockPackagingRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Packaging, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Packaging?)null);

            // Setup IDs for created entities
            _mockArtistRepository.Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .Callback<Artist>(a => a.Id = 100).ReturnsAsync((Artist a) => a);
            _mockGenreRepository.Setup(r => r.AddAsync(It.IsAny<Genre>()))
                .Callback<Genre>(g => g.Id = 200).ReturnsAsync((Genre g) => g);
            _mockLabelRepository.Setup(r => r.AddAsync(It.IsAny<Label>()))
                .Callback<Label>(l => l.Id = 300).ReturnsAsync((Label l) => l);
            _mockCountryRepository.Setup(r => r.AddAsync(It.IsAny<Country>()))
                .Callback<Country>(c => c.Id = 400).ReturnsAsync((Country c) => c);
            _mockFormatRepository.Setup(r => r.AddAsync(It.IsAny<Format>()))
                .Callback<Format>(f => f.Id = 500).ReturnsAsync((Format f) => f);
            _mockPackagingRepository.Setup(r => r.AddAsync(It.IsAny<Packaging>()))
                .Callback<Packaging>(p => p.Id = 600).ReturnsAsync((Packaging p) => p);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Created);
            Assert.NotNull(response.Created.Artists);
            Assert.NotNull(response.Created.Genres);
            Assert.NotNull(response.Created.Labels);
            Assert.NotNull(response.Created.Countries);
            Assert.NotNull(response.Created.Formats);
            Assert.NotNull(response.Created.Packagings);
            
            _mockArtistRepository.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Once);
            _mockGenreRepository.Verify(r => r.AddAsync(It.IsAny<Genre>()), Times.Once);
            _mockLabelRepository.Verify(r => r.AddAsync(It.IsAny<Label>()), Times.Once);
            _mockCountryRepository.Verify(r => r.AddAsync(It.IsAny<Country>()), Times.Once);
            _mockFormatRepository.Verify(r => r.AddAsync(It.IsAny<Format>()), Times.Once);
            _mockPackagingRepository.Verify(r => r.AddAsync(It.IsAny<Packaging>()), Times.Once);
        }

        #endregion

        #region CreateMusicRelease - Duplicate Detection

        [Fact]
        public async Task CreateMusicRelease_WithDuplicate_ReturnsBadRequestAndRollsBackTransaction()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Duplicate Album",
                LabelNumber = "CAT123",
                ArtistIds = new List<int> { 1 },
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();

            // Setup duplicate detection
            var duplicate = new MusicRelease
            {
                Id = 999,
                Title = "Duplicate Album",
                LabelNumber = "CAT123",
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            _mockMusicReleaseRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new List<MusicRelease> { duplicate });

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
            
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _mockMusicReleaseRepository.Verify(r => r.AddAsync(It.IsAny<MusicRelease>()), Times.Never);
        }

        #endregion

        #region CreateMusicRelease - Transaction Management

        [Fact]
        public async Task CreateMusicRelease_OnSuccess_CommitsTransaction()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateMusicRelease_OnException_RollsBackTransaction()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistNames = new List<string> { "Artist" },
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            SetupNoDuplicates();

            // Simulate exception during artist creation
            _mockArtistRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        #endregion

        #region CreateMusicRelease - Edge Cases

        [Fact]
        public async Task CreateMusicRelease_WithWhitespaceInNames_TrimsWhitespace()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistNames = new List<string> { "  Artist With Spaces  " },
                LabelName = "  Label With Spaces  ",
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            _mockArtistRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Artist?)null);
            _mockLabelRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Label?)null);

            _mockArtistRepository.Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .Callback<Artist>(a => a.Id = 100).ReturnsAsync((Artist a) => a);
            _mockLabelRepository.Setup(r => r.AddAsync(It.IsAny<Label>()))
                .Callback<Label>(l => l.Id = 300).ReturnsAsync((Label l) => l);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Created?.Artists);
            Assert.Equal("Artist With Spaces", response.Created.Artists[0].Name);
            Assert.NotNull(response.Created?.Labels);
            Assert.Equal("Label With Spaces", response.Created.Labels[0].Name);
        }

        [Fact]
        public async Task CreateMusicRelease_WithEmptyArtistNames_IgnoresEmpty()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistNames = new List<string> { "", "  ", "Valid Artist" },
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            _mockArtistRepository
                .Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Artist?)null);

            _mockArtistRepository.Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .Callback<Artist>(a => a.Id = 100).ReturnsAsync((Artist a) => a);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Created?.Artists);
            Assert.Single(response.Created.Artists); // Only "Valid Artist" should be created
            
            _mockArtistRepository.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Once);
        }

        [Fact]
        public async Task CreateMusicRelease_WithNoLookupData_CreatesReleaseSuccessfully()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Minimal Album",
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            
            Assert.NotNull(response.Release);
            Assert.Equal("Minimal Album", response.Release.Title);
            Assert.Null(response.Created); // No entities created
            
            _mockMusicReleaseRepository.Verify(r => r.AddAsync(It.IsAny<MusicRelease>()), Times.Once);
        }

        [Fact]
        public async Task CreateMusicRelease_WithUpcBarcode_StoresUpcCorrectly()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                Upc = "724382896722",
                ReleaseYear = new DateTime(2020, 1, 1)
            };

            SetupSuccessfulTransaction();
            SetupNoDuplicates();

            MusicRelease? capturedRelease = null;
            _mockMusicReleaseRepository
                .Setup(r => r.AddAsync(It.IsAny<MusicRelease>()))
                .Callback<MusicRelease>(mr => capturedRelease = mr)
                .ReturnsAsync((MusicRelease mr) => mr);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            Assert.NotNull(capturedRelease);
            Assert.Equal("724382896722", capturedRelease.Upc);
        }

        #endregion

        #region Helper Methods

        private void SetupSuccessfulTransaction()
        {
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _mockMusicReleaseRepository
                .Setup(r => r.AddAsync(It.IsAny<MusicRelease>()))
                .Callback<MusicRelease>(mr => mr.Id = 1000)
                .ReturnsAsync((MusicRelease mr) => mr);
        }

        private void SetupNoDuplicates()
        {
            _mockMusicReleaseRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new List<MusicRelease>());
        }

        #endregion
    }
}
