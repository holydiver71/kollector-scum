using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.DTOs;
using System.Linq.Expressions;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing formats
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class FormatsController : ControllerBase
    {
        private readonly IRepository<Format> _formatRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FormatsController> _logger;

        /// <summary>
        /// Initializes a new instance of the FormatsController
        /// </summary>
        /// <param name="formatRepository">The format repository</param>
        /// <param name="unitOfWork">The unit of work</param>
        /// <param name="logger">The logger</param>
        public FormatsController(
            IRepository<Format> formatRepository,
            IUnitOfWork unitOfWork,
            ILogger<FormatsController> logger)
        {
            _formatRepository = formatRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Gets all formats with optional filtering and pagination
        /// </summary>
        /// <param name="search">Optional search term to filter by name</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50, max: 100)</param>
        /// <returns>List of formats</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<FormatDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<FormatDto>>> GetFormats(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1)
                {
                    return BadRequest("Page must be greater than 0");
                }

                if (pageSize < 1 || pageSize > 5000)
                {
                    return BadRequest("Page size must be between 1 and 5000");
                }

                _logger.LogInformation("Getting formats - Page: {Page}, PageSize: {PageSize}, Search: {Search}",
                    page, pageSize, search);

                // Build filter expression
                Expression<Func<Format, bool>>? filter = null;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filter = f => f.Name.ToLower().Contains(search.ToLower());
                }

                // Get paginated results
                var pagedResult = await _formatRepository.GetPagedAsync(
                    pageNumber: page,
                    pageSize: pageSize,
                    filter: filter,
                    orderBy: query => query.OrderBy(f => f.Name));

                // Map to DTOs
                var formatDtos = pagedResult.Items.Select(f => new FormatDto
                {
                    Id = f.Id,
                    Name = f.Name
                }).ToList();

                var result = new PagedResult<FormatDto>
                {
                    Items = formatDtos,
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount,
                    TotalPages = pagedResult.TotalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting formats");
                return StatusCode(500, "An error occurred while retrieving formats");
            }
        }

        /// <summary>
        /// Gets a specific format by ID
        /// </summary>
        /// <param name="id">The format ID</param>
        /// <returns>The format</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(FormatDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<FormatDto>> GetFormat(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Format ID must be greater than 0");
                }

                _logger.LogInformation("Getting format with ID: {FormatId}", id);

                var format = await _formatRepository.GetByIdAsync(id);
                if (format == null)
                {
                    return NotFound($"Format with ID {id} not found");
                }

                var formatDto = new FormatDto
                {
                    Id = format.Id,
                    Name = format.Name
                };

                return Ok(formatDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting format with ID: {FormatId}", id);
                return StatusCode(500, "An error occurred while retrieving the format");
            }
        }

        /// <summary>
        /// Creates a new format
        /// </summary>
        /// <param name="createFormatDto">The format data</param>
        /// <returns>The created format</returns>
        [HttpPost]
        [ProducesResponseType(typeof(FormatDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<FormatDto>> CreateFormat([FromBody] CreateFormatDto createFormatDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Creating format: {FormatName}", createFormatDto.Name);

                // Check if format already exists
                var existingFormat = await _formatRepository.GetFirstOrDefaultAsync(
                    f => f.Name.ToLower() == createFormatDto.Name.ToLower());

                if (existingFormat != null)
                {
                    return Conflict($"Format with name '{createFormatDto.Name}' already exists");
                }

                var format = new Format
                {
                    Name = createFormatDto.Name.Trim()
                };

                await _formatRepository.AddAsync(format);
                await _unitOfWork.SaveChangesAsync();

                var formatDto = new FormatDto
                {
                    Id = format.Id,
                    Name = format.Name
                };

                return CreatedAtAction(nameof(GetFormat), new { id = format.Id }, formatDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating format: {FormatName}", createFormatDto.Name);
                return StatusCode(500, "An error occurred while creating the format");
            }
        }

        /// <summary>
        /// Updates an existing format
        /// </summary>
        /// <param name="id">The format ID</param>
        /// <param name="updateFormatDto">The updated format data</param>
        /// <returns>The updated format</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(FormatDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<FormatDto>> UpdateFormat(int id, [FromBody] UpdateFormatDto updateFormatDto)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Format ID must be greater than 0");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Updating format ID: {FormatId}", id);

                var format = await _formatRepository.GetByIdAsync(id);
                if (format == null)
                {
                    return NotFound($"Format with ID {id} not found");
                }

                // Check if another format with the same name exists
                var existingFormat = await _formatRepository.GetFirstOrDefaultAsync(
                    f => f.Name.ToLower() == updateFormatDto.Name.ToLower() && f.Id != id);

                if (existingFormat != null)
                {
                    return Conflict($"Another format with name '{updateFormatDto.Name}' already exists");
                }

                format.Name = updateFormatDto.Name.Trim();
                _formatRepository.Update(format);
                await _unitOfWork.SaveChangesAsync();

                var formatDto = new FormatDto
                {
                    Id = format.Id,
                    Name = format.Name
                };

                return Ok(formatDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating format ID: {FormatId}", id);
                return StatusCode(500, "An error occurred while updating the format");
            }
        }

        /// <summary>
        /// Deletes a format
        /// </summary>
        /// <param name="id">The format ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> DeleteFormat(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Format ID must be greater than 0");
                }

                _logger.LogInformation("Deleting format ID: {FormatId}", id);

                var format = await _formatRepository.GetByIdAsync(id);
                if (format == null)
                {
                    return NotFound($"Format with ID {id} not found");
                }

                // Check if format is referenced by any music releases
                var hasReferences = await _formatRepository.AnyAsync(
                    f => f.Id == id && f.MusicReleases.Any());

                if (hasReferences)
                {
                    return Conflict("Cannot delete format that is referenced by music releases");
                }

                await _formatRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting format ID: {FormatId}", id);
                return StatusCode(500, "An error occurred while deleting the format");
            }
        }
    }
}
