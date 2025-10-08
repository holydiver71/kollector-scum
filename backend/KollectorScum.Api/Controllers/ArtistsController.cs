using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.DTOs;
using System.Linq.Expressions;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing artists
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ArtistsController : ControllerBase
    {
        private readonly IRepository<Artist> _artistRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ArtistsController> _logger;

        public ArtistsController(
            IRepository<Artist> artistRepository,
            IUnitOfWork unitOfWork,
            ILogger<ArtistsController> logger)
        {
            _artistRepository = artistRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ArtistDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<ArtistDto>>> GetArtists(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (page < 1) return BadRequest("Page must be greater than 0");
                if (pageSize < 1 || pageSize > 5000) return BadRequest("Page size must be between 1 and 5000");

                var filter = !string.IsNullOrEmpty(search)
            ? (Expression<Func<Artist, bool>>)(a => a.Name.ToLower().Contains(search.ToLower()))
            : null;

                var pagedResult = await _artistRepository.GetPagedAsync(
                    pageNumber: page,
                    pageSize: pageSize,
                    filter: filter,
                    orderBy: query => query.OrderBy(a => a.Name));

                var artistDtos = pagedResult.Items.Select(a => new ArtistDto
                {
                    Id = a.Id,
                    Name = a.Name
                }).ToList();

                var result = new PagedResult<ArtistDto>
                {
                    Items = artistDtos,
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount,
                    TotalPages = pagedResult.TotalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting artists");
                return StatusCode(500, "An error occurred while retrieving artists");
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

                var artist = await _artistRepository.GetByIdAsync(id);
                if (artist == null)
                {
                    return NotFound($"Artist with ID {id} not found");
                }

                var artistDto = new ArtistDto
                {
                    Id = artist.Id,
                    Name = artist.Name
                };

                return Ok(artistDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting artist with ID: {ArtistId}", id);
                return StatusCode(500, "An error occurred while retrieving the artist");
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(ArtistDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<ArtistDto>> CreateArtist([FromBody] CreateArtistDto createArtistDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var existingArtist = await _artistRepository.GetFirstOrDefaultAsync(
                    a => a.Name.ToLower() == createArtistDto.Name.ToLower());

                if (existingArtist != null)
                {
                    return Conflict($"Artist with name '{createArtistDto.Name}' already exists");
                }

                var artist = new Artist { Name = createArtistDto.Name.Trim() };
                await _artistRepository.AddAsync(artist);
                await _unitOfWork.SaveChangesAsync();

                var artistDto = new ArtistDto { Id = artist.Id, Name = artist.Name };
                return CreatedAtAction(nameof(GetArtist), new { id = artist.Id }, artistDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating artist: {ArtistName}", createArtistDto.Name);
                return StatusCode(500, "An error occurred while creating the artist");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ArtistDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<ArtistDto>> UpdateArtist(int id, [FromBody] UpdateArtistDto updateArtistDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Artist ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var artist = await _artistRepository.GetByIdAsync(id);
                if (artist == null) return NotFound($"Artist with ID {id} not found");

                var existingArtist = await _artistRepository.GetFirstOrDefaultAsync(
                    a => a.Name.ToLower() == updateArtistDto.Name.ToLower() && a.Id != id);

                if (existingArtist != null)
                {
                    return Conflict($"Another artist with name '{updateArtistDto.Name}' already exists");
                }

                artist.Name = updateArtistDto.Name.Trim();
                _artistRepository.Update(artist);
                await _unitOfWork.SaveChangesAsync();

                var artistDto = new ArtistDto { Id = artist.Id, Name = artist.Name };
                return Ok(artistDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating artist ID: {ArtistId}", id);
                return StatusCode(500, "An error occurred while updating the artist");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> DeleteArtist(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Artist ID must be greater than 0");

                var artist = await _artistRepository.GetByIdAsync(id);
                if (artist == null) return NotFound($"Artist with ID {id} not found");

                var hasReferences = await _artistRepository.AnyAsync(
                    a => a.Id == id && a.MusicReleases.Any());

                if (hasReferences)
                {
                    return Conflict("Cannot delete artist that is referenced by music releases");
                }

                await _artistRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting artist ID: {ArtistId}", id);
                return StatusCode(500, "An error occurred while deleting the artist");
            }
        }
    }
}
