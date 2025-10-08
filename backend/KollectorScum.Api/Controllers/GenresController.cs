using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.DTOs;
using System.Linq.Expressions;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing genres
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GenresController : ControllerBase
    {
        private readonly IRepository<Genre> _genreRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GenresController> _logger;

        public GenresController(
            IRepository<Genre> genreRepository,
            IUnitOfWork unitOfWork,
            ILogger<GenresController> logger)
        {
            _genreRepository = genreRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<GenreDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<GenreDto>>> GetGenres(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (page < 1) return BadRequest("Page must be greater than 0");
                if (pageSize < 1 || pageSize > 5000) return BadRequest("Page size must be between 1 and 5000");

                Expression<Func<Genre, bool>>? filter = null;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filter = g => g.Name.ToLower().Contains(search.ToLower());
                }

                var pagedResult = await _genreRepository.GetPagedAsync(
                    pageNumber: page,
                    pageSize: pageSize,
                    filter: filter,
                    orderBy: query => query.OrderBy(g => g.Name));

                var genreDtos = pagedResult.Items.Select(g => new GenreDto
                {
                    Id = g.Id,
                    Name = g.Name
                }).ToList();

                var result = new PagedResult<GenreDto>
                {
                    Items = genreDtos,
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount,
                    TotalPages = pagedResult.TotalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting genres");
                return StatusCode(500, "An error occurred while retrieving genres");
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(GenreDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<GenreDto>> GetGenre(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Genre ID must be greater than 0");

                var genre = await _genreRepository.GetByIdAsync(id);
                if (genre == null)
                {
                    return NotFound($"Genre with ID {id} not found");
                }

                var genreDto = new GenreDto
                {
                    Id = genre.Id,
                    Name = genre.Name
                };

                return Ok(genreDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting genre with ID: {GenreId}", id);
                return StatusCode(500, "An error occurred while retrieving the genre");
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(GenreDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<GenreDto>> CreateGenre([FromBody] CreateGenreDto createGenreDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var existingGenre = await _genreRepository.GetFirstOrDefaultAsync(
                    g => g.Name.ToLower() == createGenreDto.Name.ToLower());

                if (existingGenre != null)
                {
                    return Conflict($"Genre with name '{createGenreDto.Name}' already exists");
                }

                var genre = new Genre { Name = createGenreDto.Name.Trim() };
                await _genreRepository.AddAsync(genre);
                await _unitOfWork.SaveChangesAsync();

                var genreDto = new GenreDto { Id = genre.Id, Name = genre.Name };
                return CreatedAtAction(nameof(GetGenre), new { id = genre.Id }, genreDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating genre: {GenreName}", createGenreDto.Name);
                return StatusCode(500, "An error occurred while creating the genre");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(GenreDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<GenreDto>> UpdateGenre(int id, [FromBody] UpdateGenreDto updateGenreDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Genre ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var genre = await _genreRepository.GetByIdAsync(id);
                if (genre == null) return NotFound($"Genre with ID {id} not found");

                var existingGenre = await _genreRepository.GetFirstOrDefaultAsync(
                    g => g.Name.ToLower() == updateGenreDto.Name.ToLower() && g.Id != id);

                if (existingGenre != null)
                {
                    return Conflict($"Another genre with name '{updateGenreDto.Name}' already exists");
                }

                genre.Name = updateGenreDto.Name.Trim();
                _genreRepository.Update(genre);
                await _unitOfWork.SaveChangesAsync();

                var genreDto = new GenreDto { Id = genre.Id, Name = genre.Name };
                return Ok(genreDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating genre ID: {GenreId}", id);
                return StatusCode(500, "An error occurred while updating the genre");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Genre ID must be greater than 0");

                var genre = await _genreRepository.GetByIdAsync(id);
                if (genre == null) return NotFound($"Genre with ID {id} not found");

                var hasReferences = await _genreRepository.AnyAsync(
                    g => g.Id == id && g.MusicReleases.Any());

                if (hasReferences)
                {
                    return Conflict("Cannot delete genre that is referenced by music releases");
                }

                await _genreRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting genre ID: {GenreId}", id);
                return StatusCode(500, "An error occurred while deleting the genre");
            }
        }
    }
}
