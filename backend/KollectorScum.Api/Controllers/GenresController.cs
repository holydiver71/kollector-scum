using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing genres
    /// </summary>
    [Authorize]
    public class GenresController : BaseApiController
    {
        private readonly IGenericCrudService<Models.Genre, GenreDto> _genreService;

        public GenresController(
            IGenericCrudService<Models.Genre, GenreDto> genreService,
            ILogger<GenresController> logger)
            : base(logger)
        {
            _genreService = genreService ?? throw new ArgumentNullException(nameof(genreService));
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
                var validationError = ValidatePaginationParameters(page, pageSize);
                if (validationError != null) return validationError;

                LogOperation("GetGenres", new { search, page, pageSize });

                var result = await _genreService.GetAllAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetGenres));
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

                LogOperation("GetGenre", new { id });

                var genreDto = await _genreService.GetByIdAsync(id);
                if (genreDto == null)
                {
                    return NotFound($"Genre with ID {id} not found");
                }

                return Ok(genreDto);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetGenre));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(GenreDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<GenreDto>> CreateGenre([FromBody] GenreDto createGenreDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("CreateGenre", new { name = createGenreDto.Name });

                var genreDto = await _genreService.CreateAsync(createGenreDto);
                return CreatedAtAction(nameof(GetGenre), new { id = genreDto.Id }, genreDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(CreateGenre));
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(GenreDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<GenreDto>> UpdateGenre(int id, [FromBody] GenreDto updateGenreDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Genre ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("UpdateGenre", new { id, name = updateGenreDto.Name });

                var genreDto = await _genreService.UpdateAsync(id, updateGenreDto);
                if (genreDto == null)
                {
                    return NotFound($"Genre with ID {id} not found");
                }

                return Ok(genreDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(UpdateGenre));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Genre ID must be greater than 0");

                LogOperation("DeleteGenre", new { id });

                var deleted = await _genreService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Genre with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(DeleteGenre));
            }
        }
    }
}
