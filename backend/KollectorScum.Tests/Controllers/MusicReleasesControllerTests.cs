using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Unit tests for MusicReleasesController
    /// Tests controller behavior by mocking IMusicReleaseService
    /// Service layer is fully tested in MusicReleaseServiceTests (74/74 passing)
    /// Controller tests verify HTTP response handling and delegation to service
    /// </summary>
    public class MusicReleasesControllerTests
    {
        private readonly Mock<IMusicReleaseService> _mockService;
        private readonly Mock<ILogger<MusicReleasesController>> _mockLogger;
        private readonly MusicReleasesController _controller;

        public MusicReleasesControllerTests()
        {
            _mockService = new Mock<IMusicReleaseService>();
            _mockLogger = new Mock<ILogger<MusicReleasesController>>();
            _controller = new MusicReleasesController(_mockService.Object, _mockLogger.Object);
        }

        #region GetMusicReleases Tests

        [Fact]
        public async Task GetMusicReleases_ReturnsOkResult_WithPagedReleases()
        {
            // Arrange
            var expectedResult = new PagedResult<MusicReleaseSummaryDto>
            {
                Items = new List<MusicReleaseSummaryDto>
                {
                    new MusicReleaseSummaryDto { Id = 1, Title = "Album 1", ArtistNames = new List<string> { "Artist 1" } },
                    new MusicReleaseSummaryDto { Id = 2, Title = "Album 2", ArtistNames = new List<string> { "Artist 2" } }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService
                .Setup(s => s.GetMusicReleasesAsync(null, null, null, null, null, null, null, null, null, 1, 50))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetMusicReleases(null, null, null, null, null, null, null, null, null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<MusicReleaseSummaryDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.TotalCount);
            Assert.Equal(2, pagedResult.Items.ToList().Count);
            Assert.Equal("Album 1", pagedResult.Items.First().Title);
        }

        [Fact]
        public async Task GetMusicReleases_WithSearchTerm_FiltersResults()
        {
            // Arrange
            var expectedResult = new PagedResult<MusicReleaseSummaryDto>
            {
                Items = new List<MusicReleaseSummaryDto>
                {
                    new MusicReleaseSummaryDto { Id = 1, Title = "Metallica Album", ArtistNames = new List<string> { "Metallica" } }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService
                .Setup(s => s.GetMusicReleasesAsync("Metallica", null, null, null, null, null, null, null, null, 1, 50))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetMusicReleases("Metallica", null, null, null, null, null, null, null, null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<MusicReleaseSummaryDto>>(okResult.Value);
            Assert.Single(pagedResult.Items);
            Assert.Contains("Metallica", pagedResult.Items.First().Title);
        }

        [Fact]
        public async Task GetMusicReleases_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var expectedResult = new PagedResult<MusicReleaseSummaryDto>
            {
                Items = new List<MusicReleaseSummaryDto>
                {
                    new MusicReleaseSummaryDto { Id = 11, Title = "Album 11" }
                },
                TotalCount = 50,
                Page = 2,
                PageSize = 10,
                TotalPages = 5
            };

            _mockService
                .Setup(s => s.GetMusicReleasesAsync(null, null, null, null, null, null, null, null, null, 2, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetMusicReleases(null, null, null, null, null, null, null, null, null, 2, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<MusicReleaseSummaryDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.Page);
            Assert.Equal(10, pagedResult.PageSize);
            Assert.Equal(50, pagedResult.TotalCount);
        }

        [Fact]
        public async Task GetMusicReleases_ReturnsEmptyList_WhenNoResults()
        {
            // Arrange
            var expectedResult = new PagedResult<MusicReleaseSummaryDto>
            {
                Items = new List<MusicReleaseSummaryDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 50,
                TotalPages = 0
            };

            _mockService
                .Setup(s => s.GetMusicReleasesAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), 
                    It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool?>(), 
                    It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetMusicReleases(null, null, null, null, null, null, null, null, null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<MusicReleaseSummaryDto>>(okResult.Value);
            Assert.Empty(pagedResult.Items);
            Assert.Equal(0, pagedResult.TotalCount);
        }

        [Fact]
        public async Task GetMusicReleases_Returns500_OnServiceException()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetMusicReleasesAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), 
                    It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool?>(), 
                    It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetMusicReleases(null, null, null, null, null, null, null, null, null, 1, 50);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region GetMusicRelease Tests

        [Fact]
        public async Task GetMusicRelease_ReturnsOkResult_WhenFound()
        {
            // Arrange
            var expectedRelease = new MusicReleaseDto
            {
                Id = 1,
                Title = "Master of Puppets",
                Artists = new List<ArtistDto> { new ArtistDto { Id = 1, Name = "Metallica" } },
                Genres = new List<GenreDto> { new GenreDto { Id = 1, Name = "Thrash Metal" } }
            };

            _mockService
                .Setup(s => s.GetMusicReleaseAsync(1))
                .ReturnsAsync(expectedRelease);

            // Act
            var result = await _controller.GetMusicRelease(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var release = Assert.IsType<MusicReleaseDto>(okResult.Value);
            Assert.Equal(1, release.Id);
            Assert.Equal("Master of Puppets", release.Title);
            Assert.NotNull(release.Artists);
            Assert.Single(release.Artists!);
        }

        [Fact]
        public async Task GetMusicRelease_ReturnsNotFound_WhenNotFound()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetMusicReleaseAsync(999))
                .ReturnsAsync((MusicReleaseDto?)null);

            // Act
            var result = await _controller.GetMusicRelease(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("999", notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task GetMusicRelease_Returns500_OnServiceException()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetMusicReleaseAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetMusicRelease(1);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region CreateMusicRelease Tests

        [Fact]
        public async Task CreateMusicRelease_WithExistingArtistIds_ReturnsCreatedResult()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1, 2 }
            };

            var expectedResponse = new CreateMusicReleaseResponseDto
            {
                Release = new MusicReleaseDto
                {
                    Id = 1,
                    Title = "Test Album",
                    Artists = new List<ArtistDto>
                    {
                        new ArtistDto { Id = 1, Name = "Artist 1" },
                        new ArtistDto { Id = 2, Name = "Artist 2" }
                    }
                },
                Created = null // No new entities created
            };

            _mockService
                .Setup(s => s.CreateMusicReleaseAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(MusicReleasesController.GetMusicRelease), createdResult.ActionName);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            Assert.Equal("Test Album", response.Release.Title);
            Assert.Null(response.Created);
        }

        [Fact]
        public async Task CreateMusicRelease_WithNewArtistNames_ReturnsCreatedWithNewArtists()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistNames = new List<string> { "New Artist 1", "New Artist 2" }
            };

            var expectedResponse = new CreateMusicReleaseResponseDto
            {
                Release = new MusicReleaseDto
                {
                    Id = 1,
                    Title = "Test Album",
                    Artists = new List<ArtistDto>
                    {
                        new ArtistDto { Id = 100, Name = "New Artist 1" },
                        new ArtistDto { Id = 101, Name = "New Artist 2" }
                    }
                },
                Created = new CreatedEntitiesDto
                {
                    Artists = new List<ArtistDto>
                    {
                        new ArtistDto { Id = 100, Name = "New Artist 1" },
                        new ArtistDto { Id = 101, Name = "New Artist 2" }
                    }
                }
            };

            _mockService
                .Setup(s => s.CreateMusicReleaseAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            Assert.NotNull(response.Created?.Artists);
            Assert.Equal(2, response.Created.Artists.Count);
            Assert.Equal("New Artist 1", response.Created.Artists[0].Name);
        }

        [Fact]
        public async Task CreateMusicRelease_WithMixedArtistIdsAndNames_ReturnsCreatedResult()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1 },
                ArtistNames = new List<string> { "New Artist" }
            };

            var expectedResponse = new CreateMusicReleaseResponseDto
            {
                Release = new MusicReleaseDto
                {
                    Id = 1,
                    Title = "Test Album",
                    Artists = new List<ArtistDto>
                    {
                        new ArtistDto { Id = 1, Name = "Existing Artist" },
                        new ArtistDto { Id = 100, Name = "New Artist" }
                    }
                },
                Created = new CreatedEntitiesDto
                {
                    Artists = new List<ArtistDto>
                    {
                        new ArtistDto { Id = 100, Name = "New Artist" }
                    }
                }
            };

            _mockService
                .Setup(s => s.CreateMusicReleaseAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            Assert.NotNull(response.Release.Artists);
            Assert.Equal(2, response.Release.Artists!.Count);
            Assert.NotNull(response.Created?.Artists);
            Assert.Single(response.Created.Artists!);
        }

        [Fact]
        public async Task CreateMusicRelease_WithNewGenreNames_ReturnsCreatedWithNewGenres()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1 },
                GenreNames = new List<string> { "New Genre 1", "New Genre 2" }
            };

            var expectedResponse = new CreateMusicReleaseResponseDto
            {
                Release = new MusicReleaseDto { Id = 1, Title = "Test Album" },
                Created = new CreatedEntitiesDto
                {
                    Genres = new List<GenreDto>
                    {
                        new GenreDto { Id = 100, Name = "New Genre 1" },
                        new GenreDto { Id = 101, Name = "New Genre 2" }
                    }
                }
            };

            _mockService
                .Setup(s => s.CreateMusicReleaseAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            Assert.NotNull(response.Created?.Genres);
            Assert.Equal(2, response.Created.Genres.Count);
        }

        [Fact]
        public async Task CreateMusicRelease_WithNewLabelName_ReturnsCreatedWithNewLabel()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1 },
                LabelName = "New Label"
            };

            var expectedResponse = new CreateMusicReleaseResponseDto
            {
                Release = new MusicReleaseDto { Id = 1, Title = "Test Album" },
                Created = new CreatedEntitiesDto
                {
                    Labels = new List<LabelDto>
                    {
                        new LabelDto { Id = 100, Name = "New Label" }
                    }
                }
            };

            _mockService
                .Setup(s => s.CreateMusicReleaseAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            Assert.NotNull(response.Created?.Labels);
            Assert.Single(response.Created.Labels);
            Assert.Equal("New Label", response.Created.Labels[0].Name);
        }

        [Fact]
        public async Task CreateMusicRelease_WithNewCountryName_ReturnsCreatedWithNewCountry()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1 },
                CountryName = "New Country"
            };

            var expectedResponse = new CreateMusicReleaseResponseDto
            {
                Release = new MusicReleaseDto { Id = 1, Title = "Test Album" },
                Created = new CreatedEntitiesDto
                {
                    Countries = new List<CountryDto>
                    {
                        new CountryDto { Id = 100, Name = "New Country" }
                    }
                }
            };

            _mockService
                .Setup(s => s.CreateMusicReleaseAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            Assert.NotNull(response.Created?.Countries);
            Assert.Single(response.Created.Countries);
        }

        [Fact]
        public async Task CreateMusicRelease_WithNewFormatName_ReturnsCreatedWithNewFormat()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1 },
                FormatName = "New Format"
            };

            var expectedResponse = new CreateMusicReleaseResponseDto
            {
                Release = new MusicReleaseDto { Id = 1, Title = "Test Album" },
                Created = new CreatedEntitiesDto
                {
                    Formats = new List<FormatDto>
                    {
                        new FormatDto { Id = 100, Name = "New Format" }
                    }
                }
            };

            _mockService
                .Setup(s => s.CreateMusicReleaseAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            Assert.NotNull(response.Created?.Formats);
            Assert.Single(response.Created.Formats);
        }

        [Fact]
        public async Task CreateMusicRelease_WithNewPackagingName_ReturnsCreatedWithNewPackaging()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1 },
                PackagingName = "New Packaging"
            };

            var expectedResponse = new CreateMusicReleaseResponseDto
            {
                Release = new MusicReleaseDto { Id = 1, Title = "Test Album" },
                Created = new CreatedEntitiesDto
                {
                    Packagings = new List<PackagingDto>
                    {
                        new PackagingDto { Id = 100, Name = "New Packaging" }
                    }
                }
            };

            _mockService
                .Setup(s => s.CreateMusicReleaseAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            Assert.NotNull(response.Created?.Packagings);
            Assert.Single(response.Created.Packagings);
        }

        [Fact]
        public async Task CreateMusicRelease_WithAllNewLookupEntities_ReturnsCreatedWithAllEntities()
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
                PackagingName = "New Packaging"
            };

            var expectedResponse = new CreateMusicReleaseResponseDto
            {
                Release = new MusicReleaseDto { Id = 1, Title = "Test Album" },
                Created = new CreatedEntitiesDto
                {
                    Artists = new List<ArtistDto> { new ArtistDto { Id = 100, Name = "New Artist" } },
                    Genres = new List<GenreDto> { new GenreDto { Id = 100, Name = "New Genre" } },
                    Labels = new List<LabelDto> { new LabelDto { Id = 100, Name = "New Label" } },
                    Countries = new List<CountryDto> { new CountryDto { Id = 100, Name = "New Country" } },
                    Formats = new List<FormatDto> { new FormatDto { Id = 100, Name = "New Format" } },
                    Packagings = new List<PackagingDto> { new PackagingDto { Id = 100, Name = "New Packaging" } }
                }
            };

            _mockService
                .Setup(s => s.CreateMusicReleaseAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateMusicReleaseResponseDto>(createdResult.Value);
            Assert.NotNull(response.Created);
            Assert.NotNull(response.Created.Artists);
            Assert.Single(response.Created.Artists!);
            Assert.NotNull(response.Created.Genres);
            Assert.Single(response.Created.Genres!);
            Assert.NotNull(response.Created.Labels);
            Assert.Single(response.Created.Labels!);
            Assert.NotNull(response.Created.Countries);
            Assert.Single(response.Created.Countries!);
            Assert.NotNull(response.Created.Formats);
            Assert.Single(response.Created.Formats!);
            Assert.NotNull(response.Created.Packagings);
            Assert.Single(response.Created.Packagings!);
        }

        [Fact]
        public async Task CreateMusicRelease_WithValidationError_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto { Title = "" }; // Invalid

            _mockService
                .Setup(s => s.CreateMusicReleaseAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ThrowsAsync(new ArgumentException("Title is required"));

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Title is required", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task CreateMusicRelease_Returns500_OnServiceException()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1 }
            };

            _mockService
                .Setup(s => s.CreateMusicReleaseAsync(It.IsAny<CreateMusicReleaseDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateMusicRelease(createDto);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region UpdateMusicRelease Tests

        [Fact]
        public async Task UpdateMusicRelease_WithValidData_ReturnsOkResult()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Updated Title"
            };

            var expectedRelease = new MusicReleaseDto
            {
                Id = 1,
                Title = "Updated Title"
            };

            _mockService
                .Setup(s => s.UpdateMusicReleaseAsync(1, It.IsAny<UpdateMusicReleaseDto>()))
                .ReturnsAsync(expectedRelease);

            // Act
            var result = await _controller.UpdateMusicRelease(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var release = Assert.IsType<MusicReleaseDto>(okResult.Value);
            Assert.Equal("Updated Title", release.Title);
        }

        [Fact]
        public async Task UpdateMusicRelease_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto { Title = "Test" };

            _mockService
                .Setup(s => s.UpdateMusicReleaseAsync(999, It.IsAny<UpdateMusicReleaseDto>()))
                .ReturnsAsync((MusicReleaseDto?)null);

            // Act
            var result = await _controller.UpdateMusicRelease(999, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("999", notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task UpdateMusicRelease_WithValidationError_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto { Title = "" };

            _mockService
                .Setup(s => s.UpdateMusicReleaseAsync(It.IsAny<int>(), It.IsAny<UpdateMusicReleaseDto>()))
                .ThrowsAsync(new ArgumentException("Title is required"));

            // Act
            var result = await _controller.UpdateMusicRelease(1, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Title is required", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task UpdateMusicRelease_Returns500_OnServiceException()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto { Title = "Test" };

            _mockService
                .Setup(s => s.UpdateMusicReleaseAsync(It.IsAny<int>(), It.IsAny<UpdateMusicReleaseDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateMusicRelease(1, updateDto);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region DeleteMusicRelease Tests

        [Fact]
        public async Task DeleteMusicRelease_WhenExists_ReturnsNoContent()
        {
            // Arrange
            _mockService
                .Setup(s => s.DeleteMusicReleaseAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteMusicRelease(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteMusicRelease_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockService
                .Setup(s => s.DeleteMusicReleaseAsync(999))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteMusicRelease(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("999", notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task DeleteMusicRelease_Returns500_OnServiceException()
        {
            // Arrange
            _mockService
                .Setup(s => s.DeleteMusicReleaseAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteMusicRelease(1);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region GetSearchSuggestions Tests

        [Fact]
        public async Task GetSearchSuggestions_ReturnsArtistSuggestions()
        {
            // Arrange
            var expectedSuggestions = new List<SearchSuggestionDto>
            {
                new SearchSuggestionDto { Type = "Artist", Name = "Metallica", Id = 1 }
            };

            _mockService
                .Setup(s => s.GetSearchSuggestionsAsync("metal", 10))
                .ReturnsAsync(expectedSuggestions);

            // Act
            var result = await _controller.GetSearchSuggestions("metal", 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.Single(suggestions);
            Assert.Equal("Metallica", suggestions[0].Name);
            Assert.Equal("Artist", suggestions[0].Type);
        }

        [Fact]
        public async Task GetSearchSuggestions_ReturnsLabelSuggestions()
        {
            // Arrange
            var expectedSuggestions = new List<SearchSuggestionDto>
            {
                new SearchSuggestionDto { Type = "Label", Name = "Warner Music", Id = 1 }
            };

            _mockService
                .Setup(s => s.GetSearchSuggestionsAsync("warner", 10))
                .ReturnsAsync(expectedSuggestions);

            // Act
            var result = await _controller.GetSearchSuggestions("warner", 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.Single(suggestions);
            Assert.Equal("Warner Music", suggestions[0].Name);
        }

        [Fact]
        public async Task GetSearchSuggestions_ReturnsReleaseSuggestions()
        {
            // Arrange
            var expectedSuggestions = new List<SearchSuggestionDto>
            {
                new SearchSuggestionDto { Type = "Release", Name = "Master of Puppets", Id = 1, Subtitle = "1986" }
            };

            _mockService
                .Setup(s => s.GetSearchSuggestionsAsync("master", 10))
                .ReturnsAsync(expectedSuggestions);

            // Act
            var result = await _controller.GetSearchSuggestions("master", 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.Single(suggestions);
            Assert.Equal("Master of Puppets", suggestions[0].Name);
        }

        [Fact]
        public async Task GetSearchSuggestions_ReturnsCombinedSuggestions()
        {
            // Arrange
            var expectedSuggestions = new List<SearchSuggestionDto>
            {
                new SearchSuggestionDto { Type = "Artist", Name = "Metallica", Id = 1 },
                new SearchSuggestionDto { Type = "Release", Name = "Master of Puppets", Id = 1 },
                new SearchSuggestionDto { Type = "Label", Name = "Metal Blade", Id = 1 }
            };

            _mockService
                .Setup(s => s.GetSearchSuggestionsAsync("met", 10))
                .ReturnsAsync(expectedSuggestions);

            // Act
            var result = await _controller.GetSearchSuggestions("met", 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.Equal(3, suggestions.Count);
        }

        [Fact]
        public async Task GetSearchSuggestions_ReturnsEmpty_WhenNoMatches()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetSearchSuggestionsAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<SearchSuggestionDto>());

            // Act
            var result = await _controller.GetSearchSuggestions("xyz", 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var suggestions = Assert.IsType<List<SearchSuggestionDto>>(okResult.Value);
            Assert.Empty(suggestions);
        }

        [Fact]
        public async Task GetSearchSuggestions_Returns500_OnServiceException()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetSearchSuggestionsAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetSearchSuggestions("test", 10);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region GetCollectionStatistics Tests

        [Fact]
        public async Task GetCollectionStatistics_ReturnsStatistics()
        {
            // Arrange
            var expectedStats = new CollectionStatisticsDto
            {
                TotalReleases = 100,
                TotalArtists = 50,
                TotalGenres = 20,
                TotalLabels = 30
            };

            _mockService
                .Setup(s => s.GetCollectionStatisticsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetCollectionStatistics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<CollectionStatisticsDto>(okResult.Value);
            Assert.Equal(100, stats.TotalReleases);
            Assert.Equal(50, stats.TotalArtists);
        }

        [Fact]
        public async Task GetCollectionStatistics_ReturnsEmptyStats_WhenNoReleases()
        {
            // Arrange
            var expectedStats = new CollectionStatisticsDto
            {
                TotalReleases = 0,
                TotalArtists = 0,
                TotalGenres = 0,
                TotalLabels = 0
            };

            _mockService
                .Setup(s => s.GetCollectionStatisticsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetCollectionStatistics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<CollectionStatisticsDto>(okResult.Value);
            Assert.Equal(0, stats.TotalReleases);
        }

        [Fact]
        public async Task GetCollectionStatistics_Returns500_OnServiceException()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetCollectionStatisticsAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetCollectionStatistics();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion
    }
}
