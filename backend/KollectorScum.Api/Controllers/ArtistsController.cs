using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing artists
    /// </summary>
    [Authorize]
    public class ArtistsController : BaseApiController
    {
        private readonly IGenericCrudService<Models.Artist, ArtistDto> _artistService;

        public ArtistsController(
            IGenericCrudService<Models.Artist, ArtistDto> artistService,
            ILogger<ArtistsController> logger)
            : base(logger)
        {
            _artistService = artistService ?? throw new ArgumentNullException(nameof(artistService));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ArtistDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<ArtistDto>>> GetArtists(
            [FromQuery] string? search = null,
            [FromQuery] string? startsWith = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var validationError = ValidatePaginationParameters(page, pageSize);
                if (validationError != null) return validationError;

                // Validate startsWith: must be a single letter A-Z (case-insensitive) or the literal "0-9"
                if (!string.IsNullOrWhiteSpace(startsWith))
                {
                    var trimmed = startsWith.Trim();
                    if (trimmed != "0-9" && (trimmed.Length != 1 || !char.IsLetter(trimmed[0])))
                    {
                        return BadRequest("startsWith must be a single letter A-Z or '0-9'");
                    }
                    startsWith = trimmed;
                }

                LogOperation("GetArtists", new { search, startsWith, page, pageSize });

                // Build optional starts-with letter filter
                System.Linq.Expressions.Expression<Func<Models.Artist, bool>>? letterFilter = null;
                if (!string.IsNullOrWhiteSpace(startsWith))
                {
                    if (startsWith == "0-9")
                    {
                        letterFilter = a => a.Name != null && (
                            a.Name.StartsWith("0") || a.Name.StartsWith("1") || a.Name.StartsWith("2") ||
                            a.Name.StartsWith("3") || a.Name.StartsWith("4") || a.Name.StartsWith("5") ||
                            a.Name.StartsWith("6") || a.Name.StartsWith("7") || a.Name.StartsWith("8") ||
                            a.Name.StartsWith("9"));
                    }
                    else
                    {
                        var letter = startsWith.ToLowerInvariant();
                        letterFilter = a => a.Name != null && a.Name.ToLower().StartsWith(letter);
                    }
                }

                var result = await _artistService.GetAllAsync(page, pageSize, search, letterFilter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetArtists));
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ArtistDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ArtistDto>> GetArtist(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Artist ID must be greater than 0");

                LogOperation("GetArtist", new { id });

                var artistDto = await _artistService.GetByIdAsync(id);
                if (artistDto == null)
                {
                    return NotFound($"Artist with ID {id} not found");
                }

                return Ok(artistDto);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetArtist));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(ArtistDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ArtistDto>> CreateArtist([FromBody] ArtistDto createArtistDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("CreateArtist", new { name = createArtistDto.Name });

                var artistDto = await _artistService.CreateAsync(createArtistDto);
                return CreatedAtAction(nameof(GetArtist), new { id = artistDto.Id }, artistDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(CreateArtist));
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ArtistDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ArtistDto>> UpdateArtist(int id, [FromBody] ArtistDto updateArtistDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Artist ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("UpdateArtist", new { id, name = updateArtistDto.Name });

                var artistDto = await _artistService.UpdateAsync(id, updateArtistDto);
                if (artistDto == null)
                {
                    return NotFound($"Artist with ID {id} not found");
                }

                return Ok(artistDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(UpdateArtist));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteArtist(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Artist ID must be greater than 0");

                LogOperation("DeleteArtist", new { id });

                var deleted = await _artistService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Artist with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(DeleteArtist));
            }
        }
    }
}
