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

        #region GetMusicReleases

        [Fact]
        public async Task GetMusicReleases_ReturnsAllReleases_WhenNoFiltersApplied()
        {
            // Arrange
            var releases = new List<MusicRelease>
            {
                new MusicRelease { Id = 1, Title = "Album 1", ReleaseYear = new DateTime(2020, 1, 1) },
                new MusicRelease { Id = 2, Title = "Album 2", ReleaseYear = new DateTime(2021, 1, 1) }
            };
            _mockMusicReleaseRepository
                .Setup(r => r.GetPagedAsync(
                    1, 50, null, It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(), "Label,Country,Format"))
                .ReturnsAsync(new PagedResult<MusicRelease> { Items = releases, Page = 1, PageSize = 50, TotalCount = 2, TotalPages = 1 });

            // Act
            var result = await _controller.GetMusicReleases(null, null, null, null, null, null, null, null, null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<MusicReleaseSummaryDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.Items.Count());
        }

        [Fact]
        public async Task GetMusicReleases_FiltersBySearchTerm()
        {
            // Arrange
            var releases = new List<MusicRelease>
            {
                new MusicRelease { Id = 1, Title = "Metal Album" },
                new MusicRelease { Id = 2, Title = "Jazz Album" }
            };
            _mockMusicReleaseRepository
                .Setup(r => r.GetPagedAsync(
                    1, 50, It.IsAny<Expression<Func<MusicRelease, bool>>>(), It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(), "Label,Country,Format"))
                .ReturnsAsync(new PagedResult<MusicRelease> { Items = releases.Where(r => r.Title.Contains("Metal")).ToList(), Page = 1, PageSize = 50, TotalCount = 1, TotalPages = 1 });

            // Act
            var result = await _controller.GetMusicReleases("Metal", null, null, null, null, null, null, null, null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<MusicReleaseSummaryDto>>(okResult.Value);
            Assert.Single(pagedResult.Items);
            Assert.Equal("Metal Album", pagedResult.Items.First().Title);
        }

        [Fact]
        public async Task GetMusicReleases_AppliesPagination()
        {
            // Arrange
            var releases = Enumerable.Range(1, 30).Select(i => new MusicRelease { Id = i, Title = $"Album {i}" }).ToList();
            _mockMusicReleaseRepository
                .Setup(r => r.GetPagedAsync(
                    2, 10, null, It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(), "Label,Country,Format"))
                .ReturnsAsync(new PagedResult<MusicRelease> { Items = releases.Skip(10).Take(10).ToList(), Page = 2, PageSize = 10, TotalCount = 30, TotalPages = 3 });

            // Act
            var result = await _controller.GetMusicReleases(null, null, null, null, null, null, null, null, null, 2, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<MusicReleaseSummaryDto>>(okResult.Value);
            Assert.Equal(10, pagedResult.Items.Count());
            Assert.Equal(2, pagedResult.Page);
            Assert.Equal(10, pagedResult.PageSize);
        }

        [Fact]
        public async Task GetMusicReleases_ReturnsEmpty_WhenNoResults()
        {
            // Arrange
            _mockMusicReleaseRepository
                .Setup(r => r.GetPagedAsync(
                    1, 50, null, It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(), "Label,Country,Format"))
                .ReturnsAsync(new PagedResult<MusicRelease> { Items = new List<MusicRelease>(), Page = 1, PageSize = 50, TotalCount = 0, TotalPages = 0 });

            // Act
            var result = await _controller.GetMusicReleases(null, null, null, null, null, null, null, null, null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<MusicReleaseSummaryDto>>(okResult.Value);
            Assert.Empty(pagedResult.Items);
        }

        [Fact]
        public async Task GetMusicReleases_Returns500_OnException()
        {
            // Arrange
            _mockMusicReleaseRepository
                .Setup(r => r.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<MusicRelease, bool>>>(), It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.GetMusicReleases(null, null, null, null, null, null, null, null, null, 1, 50);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region GetMusicRelease

        [Fact]
        public async Task GetMusicRelease_ReturnsRelease_WhenFound()
        {
            // Arrange
            var release = new MusicRelease { Id = 1, Title = "Test Album", ReleaseYear = new DateTime(2020, 1, 1) };
            _mockMusicReleaseRepository
                .Setup(r => r.GetByIdAsync(1, "Label,Country,Format,Packaging"))
                .ReturnsAsync(release);
            _mockArtistRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Artist { Id = 1, Name = "Artist" });
            _mockGenreRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Genre { Id = 1, Name = "Genre" });

            // Act
            var result = await _controller.GetMusicRelease(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<MusicReleaseDto>(okResult.Value);
            Assert.Equal(1, dto.Id);
            Assert.Equal("Test Album", dto.Title);
        }

        [Fact]
        public async Task GetMusicRelease_ReturnsNotFound_WhenMissing()
        {
            // Arrange
            _mockMusicReleaseRepository
                .Setup(r => r.GetByIdAsync(999, "Label,Country,Format,Packaging"))
                .ReturnsAsync((MusicRelease)null);

            // Act
            var result = await _controller.GetMusicRelease(999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetMusicRelease_Returns500_OnException()
        {
            // Arrange
            _mockMusicReleaseRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.GetMusicRelease(1);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region GetSearchSuggestions

        [Fact]
        public async Task GetSearchSuggestions_ReturnsEmpty_WhenQueryIsNullOrShort()
        {
            // Act
            var result = await _controller.GetSearchSuggestions(null);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.Empty(suggestions);

            result = await _controller.GetSearchSuggestions("");
            okResult = Assert.IsType<OkObjectResult>(result.Result);
            suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.Empty(suggestions);

            result = await _controller.GetSearchSuggestions("a");
            okResult = Assert.IsType<OkObjectResult>(result.Result);
            suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.Empty(suggestions);
        }

        [Fact]
        public async Task GetSearchSuggestions_ReturnsReleaseSuggestions()
        {
            // Arrange
            var releases = new List<MusicRelease> { new MusicRelease { Id = 1, Title = "Metal Album", ReleaseYear = new DateTime(2020, 1, 1) } };
            _mockMusicReleaseRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MusicRelease, bool>>>(), It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(), It.IsAny<string>())).ReturnsAsync(releases);

            // Act
            var result = await _controller.GetSearchSuggestions("Metal");
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.Single(suggestions);
            Assert.Equal("release", suggestions[0].Type);
            Assert.Equal("Metal Album", suggestions[0].Name);
        }

        [Fact]
        public async Task GetSearchSuggestions_ReturnsArtistSuggestions()
        {
            // Arrange
            var artists = new List<Artist> { new Artist { Id = 1, Name = "Metal Band" } };
            _mockArtistRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(), It.IsAny<string>())).ReturnsAsync(artists);

            // Act
            var result = await _controller.GetSearchSuggestions("Metal");
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.Single(suggestions);
            Assert.Equal("artist", suggestions[0].Type);
            Assert.Equal("Metal Band", suggestions[0].Name);
        }

        [Fact]
        public async Task GetSearchSuggestions_ReturnsLabelSuggestions()
        {
            // Arrange
            var labels = new List<Label> { new Label { Id = 1, Name = "Metal Label" } };
            _mockLabelRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<Func<IQueryable<Label>, IOrderedQueryable<Label>>>(), It.IsAny<string>())).ReturnsAsync(labels);

            // Act
            var result = await _controller.GetSearchSuggestions("Metal");
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.Single(suggestions);
            Assert.Equal("label", suggestions[0].Type);
            Assert.Equal("Metal Label", suggestions[0].Name);
        }

        [Fact]
        public async Task GetSearchSuggestions_ReturnsCombinedSuggestions_AndLimits()
        {
            // Arrange
            var releases = new List<MusicRelease> {
                new MusicRelease { Id = 0, Title = "AMetal Album 0" }
            };
            releases.AddRange(Enumerable.Range(1, 5).Select(i => new MusicRelease { Id = i, Title = $"ZMetal Album {i}" }));
            var artists = new List<Artist> {
                new Artist { Id = 1, Name = "BMetal Band 1" },
                new Artist { Id = 2, Name = "CMetal Band 2" },
                new Artist { Id = 3, Name = "DMetal Band 3" },
                new Artist { Id = 4, Name = "EMetal Band 4" },
                new Artist { Id = 5, Name = "FMetal Band 5" }
            };
            var labels = new List<Label> {
                new Label { Id = 1, Name = "BMetal Label 1" },
                new Label { Id = 2, Name = "CMetal Label 2" },
                new Label { Id = 3, Name = "DMetal Label 3" },
                new Label { Id = 4, Name = "EMetal Label 4" },
                new Label { Id = 5, Name = "FMetal Label 5" }
            };
            _mockMusicReleaseRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MusicRelease, bool>>>(), It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(), It.IsAny<string>())).ReturnsAsync(releases);
            _mockArtistRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(), It.IsAny<string>())).ReturnsAsync(artists);
            _mockLabelRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<Func<IQueryable<Label>, IOrderedQueryable<Label>>>(), It.IsAny<string>())).ReturnsAsync(labels);

            // Act
            var result = await _controller.GetSearchSuggestions("Metal");
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.True(suggestions.Count <= 10);
            Assert.Contains(suggestions, s => s.Type == "release");
            Assert.Contains(suggestions, s => s.Type == "artist");
            Assert.Contains(suggestions, s => s.Type == "label");
        }

        [Fact]
        public async Task GetSearchSuggestions_Returns500_OnException()
        {
            // Arrange
            _mockMusicReleaseRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MusicRelease, bool>>>(), It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(), It.IsAny<string>())).ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.GetSearchSuggestions("Metal");
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region GetCollectionStatistics

        [Fact]
        public async Task GetCollectionStatistics_ReturnsBasicStats()
        {
            // Arrange
            var releases = new List<MusicRelease> {
                new MusicRelease {
                    Id = 1,
                    Title = "A",
                    ReleaseYear = new DateTime(2020, 1, 1),
                    CountryId = 1,
                    FormatId = 1,
                    Genres = "[1]",
                    Artists = "[1]",
                    LabelId = 1 // Ensure label ID matches mock label
                }
            };
            _mockMusicReleaseRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(releases);
            _mockCountryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Country> { new Country { Id = 1, Name = "USA" } });
            _mockFormatRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Format> { new Format { Id = 1, Name = "CD" } });
            _mockGenreRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Genre> { new Genre { Id = 1, Name = "Metal" } });
            _mockLabelRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Label> { new Label { Id = 1, Name = "Label1" } });

            // Act
            var result = await _controller.GetCollectionStatistics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<CollectionStatisticsDto>(okResult.Value);
            Assert.Equal(1, stats.TotalReleases);
            Assert.Equal(1, stats.TotalArtists);
            Assert.Equal(1, stats.TotalGenres);
            Assert.Equal(1, stats.TotalLabels);
        }

        [Fact]
        public async Task GetCollectionStatistics_ReturnsEmptyStats_WhenNoReleases()
        {
            // Arrange
            _mockMusicReleaseRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<MusicRelease>());

            // Act
            var result = await _controller.GetCollectionStatistics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<CollectionStatisticsDto>(okResult.Value);
            Assert.Equal(0, stats.TotalReleases);
            Assert.Equal(0, stats.TotalArtists);
            Assert.Equal(0, stats.TotalGenres);
            Assert.Equal(0, stats.TotalLabels);
        }

        [Fact]
        public async Task GetCollectionStatistics_Returns500_OnException()
        {
            // Arrange
            _mockMusicReleaseRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.GetCollectionStatistics();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region UpdateMusicRelease

        [Fact]
        public async Task UpdateMusicRelease_ReturnsNotFound_WhenMissing()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto { Title = "Updated" };
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MusicRelease)null);

            // Act
            var result = await _controller.UpdateMusicRelease(999, updateDto);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task UpdateMusicRelease_UpdatesFields_Successfully()
        {
            // Arrange
            var existingRelease = new MusicRelease { Id = 1, Title = "Old Title", ReleaseYear = new DateTime(2020, 1, 1) };
            var updateDto = new UpdateMusicReleaseDto { Title = "New Title" };
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepository.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            // Act
            var result = await _controller.UpdateMusicRelease(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<MusicReleaseDto>(okResult.Value);
            Assert.Equal("New Title", dto.Title);
        }

        [Fact]
        public async Task UpdateMusicRelease_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var release = new MusicRelease { Id = 1, Title = "Old" };
            var updateDto = new UpdateMusicReleaseDto { Title = "New", LabelId = 999 };
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(release);
            _mockLabelRepository.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateMusicRelease(1, updateDto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("does not exist", badRequest.Value.ToString());
        }

        [Fact]
        public async Task UpdateMusicRelease_Returns500_OnException()
        {
            // Arrange
            var release = new MusicRelease { Id = 1, Title = "Old" };
            var updateDto = new UpdateMusicReleaseDto { Title = "New" };
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(release);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new Exception("DB error"));
            _mockMusicReleaseRepository.Setup(r => r.Update(It.IsAny<MusicRelease>())).Callback<MusicRelease>(r => throw new Exception("DB error"));

            // Act
            var result = await _controller.UpdateMusicRelease(1, updateDto);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region DeleteMusicRelease

        [Fact]
        public async Task DeleteMusicRelease_ReturnsNotFound_WhenMissing()
        {
            // Arrange
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((MusicRelease)null);

            // Act
            var result = await _controller.DeleteMusicRelease(999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task DeleteMusicRelease_DeletesRelease_Successfully()
        {
            // Arrange
            var release = new MusicRelease { Id = 1, Title = "To Delete" };
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(release);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepository.Setup(r => r.DeleteAsync(It.IsAny<int>())).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteMusicRelease(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockMusicReleaseRepository.Verify(r => r.Delete(release), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMusicRelease_Returns500_OnException()
        {
            // Arrange
            var release = new MusicRelease { Id = 1, Title = "To Delete" };
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(release);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new Exception("DB error"));
            _mockMusicReleaseRepository.Setup(r => r.DeleteAsync(It.IsAny<int>())).ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.DeleteMusicRelease(1);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
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
