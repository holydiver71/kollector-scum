using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.DTOs;
using System.Linq.Expressions;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing labels
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class LabelsController : ControllerBase
    {
        private readonly IRepository<Label> _labelRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LabelsController> _logger;

        public LabelsController(
            IRepository<Label> labelRepository,
            IUnitOfWork unitOfWork,
            ILogger<LabelsController> logger)
        {
            _labelRepository = labelRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<LabelDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<LabelDto>>> GetLabels(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (page < 1) return BadRequest("Page must be greater than 0");
                if (pageSize < 1 || pageSize > 5000) return BadRequest("Page size must be between 1 and 5000");

                Expression<Func<Label, bool>>? filter = null;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filter = l => l.Name.ToLower().Contains(search.ToLower());
                }

                var pagedResult = await _labelRepository.GetPagedAsync(
                    pageNumber: page,
                    pageSize: pageSize,
                    filter: filter,
                    orderBy: query => query.OrderBy(l => l.Name));

                var labelDtos = pagedResult.Items.Select(l => new LabelDto
                {
                    Id = l.Id,
                    Name = l.Name
                }).ToList();

                var result = new PagedResult<LabelDto>
                {
                    Items = labelDtos,
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount,
                    TotalPages = pagedResult.TotalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting labels");
                return StatusCode(500, "An error occurred while retrieving labels");
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LabelDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<LabelDto>> GetLabel(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Label ID must be greater than 0");

                var label = await _labelRepository.GetByIdAsync(id);
                if (label == null)
                {
                    return NotFound($"Label with ID {id} not found");
                }

                var labelDto = new LabelDto
                {
                    Id = label.Id,
                    Name = label.Name
                };

                return Ok(labelDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting label with ID: {LabelId}", id);
                return StatusCode(500, "An error occurred while retrieving the label");
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(LabelDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<LabelDto>> CreateLabel([FromBody] CreateLabelDto createLabelDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var existingLabel = await _labelRepository.GetFirstOrDefaultAsync(
                    l => l.Name.ToLower() == createLabelDto.Name.ToLower());

                if (existingLabel != null)
                {
                    return Conflict($"Label with name '{createLabelDto.Name}' already exists");
                }

                var label = new Label { Name = createLabelDto.Name.Trim() };
                await _labelRepository.AddAsync(label);
                await _unitOfWork.SaveChangesAsync();

                var labelDto = new LabelDto { Id = label.Id, Name = label.Name };
                return CreatedAtAction(nameof(GetLabel), new { id = label.Id }, labelDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating label: {LabelName}", createLabelDto.Name);
                return StatusCode(500, "An error occurred while creating the label");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(LabelDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<LabelDto>> UpdateLabel(int id, [FromBody] UpdateLabelDto updateLabelDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Label ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var label = await _labelRepository.GetByIdAsync(id);
                if (label == null) return NotFound($"Label with ID {id} not found");

                var existingLabel = await _labelRepository.GetFirstOrDefaultAsync(
                    l => l.Name.ToLower() == updateLabelDto.Name.ToLower() && l.Id != id);

                if (existingLabel != null)
                {
                    return Conflict($"Another label with name '{updateLabelDto.Name}' already exists");
                }

                label.Name = updateLabelDto.Name.Trim();
                _labelRepository.Update(label);
                await _unitOfWork.SaveChangesAsync();

                var labelDto = new LabelDto { Id = label.Id, Name = label.Name };
                return Ok(labelDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating label ID: {LabelId}", id);
                return StatusCode(500, "An error occurred while updating the label");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> DeleteLabel(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Label ID must be greater than 0");

                var label = await _labelRepository.GetByIdAsync(id);
                if (label == null) return NotFound($"Label with ID {id} not found");

                var hasReferences = await _labelRepository.AnyAsync(
                    l => l.Id == id && l.MusicReleases.Any());

                if (hasReferences)
                {
                    return Conflict("Cannot delete label that is referenced by music releases");
                }

                await _labelRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting label ID: {LabelId}", id);
                return StatusCode(500, "An error occurred while deleting the label");
            }
        }
    }
}
