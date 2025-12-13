using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing labels
    /// </summary>
    [Authorize]
    public class LabelsController : BaseApiController
    {
        private readonly IGenericCrudService<Models.Label, LabelDto> _labelService;

        public LabelsController(
            IGenericCrudService<Models.Label, LabelDto> labelService,
            ILogger<LabelsController> logger)
            : base(logger)
        {
            _labelService = labelService ?? throw new ArgumentNullException(nameof(labelService));
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
                var validationError = ValidatePaginationParameters(page, pageSize);
                if (validationError != null) return validationError;

                LogOperation("GetLabels", new { search, page, pageSize });

                var result = await _labelService.GetAllAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetLabels));
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

                LogOperation("GetLabel", new { id });

                var labelDto = await _labelService.GetByIdAsync(id);
                if (labelDto == null)
                {
                    return NotFound($"Label with ID {id} not found");
                }

                return Ok(labelDto);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetLabel));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(LabelDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<LabelDto>> CreateLabel([FromBody] LabelDto createLabelDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("CreateLabel", new { name = createLabelDto.Name });

                var labelDto = await _labelService.CreateAsync(createLabelDto);
                return CreatedAtAction(nameof(GetLabel), new { id = labelDto.Id }, labelDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(CreateLabel));
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(LabelDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<LabelDto>> UpdateLabel(int id, [FromBody] LabelDto updateLabelDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Label ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("UpdateLabel", new { id, name = updateLabelDto.Name });

                var labelDto = await _labelService.UpdateAsync(id, updateLabelDto);
                if (labelDto == null)
                {
                    return NotFound($"Label with ID {id} not found");
                }

                return Ok(labelDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(UpdateLabel));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteLabel(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Label ID must be greater than 0");

                LogOperation("DeleteLabel", new { id });

                var deleted = await _labelService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Label with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(DeleteLabel));
            }
        }
    }
}
