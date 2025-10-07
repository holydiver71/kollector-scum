using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.DTOs;
using System.Linq.Expressions;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing packaging types
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PackagingsController : ControllerBase
    {
        private readonly IRepository<Packaging> _packagingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PackagingsController> _logger;

        public PackagingsController(
            IRepository<Packaging> packagingRepository,
            IUnitOfWork unitOfWork,
            ILogger<PackagingsController> logger)
        {
            _packagingRepository = packagingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<PackagingDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<PackagingDto>>> GetPackagings(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (page < 1) return BadRequest("Page must be greater than 0");
                if (pageSize < 1 || pageSize > 100) return BadRequest("Page size must be between 1 and 100");

                Expression<Func<Packaging, bool>>? filter = null;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filter = p => p.Name.ToLower().Contains(search.ToLower());
                }

                var pagedResult = await _packagingRepository.GetPagedAsync(
                    pageNumber: page,
                    pageSize: pageSize,
                    filter: filter,
                    orderBy: query => query.OrderBy(p => p.Name));

                var packagingDtos = pagedResult.Items.Select(p => new PackagingDto
                {
                    Id = p.Id,
                    Name = p.Name
                }).ToList();

                var result = new PagedResult<PackagingDto>
                {
                    Items = packagingDtos,
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount,
                    TotalPages = pagedResult.TotalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting packagings");
                return StatusCode(500, "An error occurred while retrieving packagings");
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PackagingDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PackagingDto>> GetPackaging(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Packaging ID must be greater than 0");

                var packaging = await _packagingRepository.GetByIdAsync(id);
                if (packaging == null)
                {
                    return NotFound($"Packaging with ID {id} not found");
                }

                var packagingDto = new PackagingDto
                {
                    Id = packaging.Id,
                    Name = packaging.Name
                };

                return Ok(packagingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting packaging with ID: {PackagingId}", id);
                return StatusCode(500, "An error occurred while retrieving the packaging");
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(PackagingDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<PackagingDto>> CreatePackaging([FromBody] CreatePackagingDto createPackagingDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var existingPackaging = await _packagingRepository.GetFirstOrDefaultAsync(
                    p => p.Name.ToLower() == createPackagingDto.Name.ToLower());

                if (existingPackaging != null)
                {
                    return Conflict($"Packaging with name '{createPackagingDto.Name}' already exists");
                }

                var packaging = new Packaging { Name = createPackagingDto.Name.Trim() };
                await _packagingRepository.AddAsync(packaging);
                await _unitOfWork.SaveChangesAsync();

                var packagingDto = new PackagingDto { Id = packaging.Id, Name = packaging.Name };
                return CreatedAtAction(nameof(GetPackaging), new { id = packaging.Id }, packagingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating packaging: {PackagingName}", createPackagingDto.Name);
                return StatusCode(500, "An error occurred while creating the packaging");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(PackagingDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<PackagingDto>> UpdatePackaging(int id, [FromBody] UpdatePackagingDto updatePackagingDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Packaging ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var packaging = await _packagingRepository.GetByIdAsync(id);
                if (packaging == null) return NotFound($"Packaging with ID {id} not found");

                var existingPackaging = await _packagingRepository.GetFirstOrDefaultAsync(
                    p => p.Name.ToLower() == updatePackagingDto.Name.ToLower() && p.Id != id);

                if (existingPackaging != null)
                {
                    return Conflict($"Another packaging with name '{updatePackagingDto.Name}' already exists");
                }

                packaging.Name = updatePackagingDto.Name.Trim();
                _packagingRepository.Update(packaging);
                await _unitOfWork.SaveChangesAsync();

                var packagingDto = new PackagingDto { Id = packaging.Id, Name = packaging.Name };
                return Ok(packagingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating packaging ID: {PackagingId}", id);
                return StatusCode(500, "An error occurred while updating the packaging");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> DeletePackaging(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Packaging ID must be greater than 0");

                var packaging = await _packagingRepository.GetByIdAsync(id);
                if (packaging == null) return NotFound($"Packaging with ID {id} not found");

                var hasReferences = await _packagingRepository.AnyAsync(
                    p => p.Id == id && p.MusicReleases.Any());

                if (hasReferences)
                {
                    return Conflict("Cannot delete packaging that is referenced by music releases");
                }

                await _packagingRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting packaging ID: {PackagingId}", id);
                return StatusCode(500, "An error occurred while deleting the packaging");
            }
        }
    }
}
