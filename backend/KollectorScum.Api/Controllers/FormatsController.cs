using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing formats
    /// </summary>
    public class FormatsController : BaseApiController
    {
        private readonly IGenericCrudService<Models.Format, FormatDto> _formatService;

        public FormatsController(
            IGenericCrudService<Models.Format, FormatDto> formatService,
            ILogger<FormatsController> logger)
            : base(logger)
        {
            _formatService = formatService ?? throw new ArgumentNullException(nameof(formatService));
        }

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
                var validationError = ValidatePaginationParameters(page, pageSize);
                if (validationError != null) return validationError;

                LogOperation("GetFormats", new { search, page, pageSize });

                var result = await _formatService.GetAllAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetFormats));
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(FormatDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<FormatDto>> GetFormat(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Format ID must be greater than 0");

                LogOperation("GetFormat", new { id });

                var formatDto = await _formatService.GetByIdAsync(id);
                if (formatDto == null)
                {
                    return NotFound($"Format with ID {id} not found");
                }

                return Ok(formatDto);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetFormat));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(FormatDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<FormatDto>> CreateFormat([FromBody] FormatDto createFormatDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("CreateFormat", new { name = createFormatDto.Name });

                var formatDto = await _formatService.CreateAsync(createFormatDto);
                return CreatedAtAction(nameof(GetFormat), new { id = formatDto.Id }, formatDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(CreateFormat));
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(FormatDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<FormatDto>> UpdateFormat(int id, [FromBody] FormatDto updateFormatDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Format ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("UpdateFormat", new { id, name = updateFormatDto.Name });

                var formatDto = await _formatService.UpdateAsync(id, updateFormatDto);
                if (formatDto == null)
                {
                    return NotFound($"Format with ID {id} not found");
                }

                return Ok(formatDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(UpdateFormat));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteFormat(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Format ID must be greater than 0");

                LogOperation("DeleteFormat", new { id });

                var deleted = await _formatService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Format with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(DeleteFormat));
            }
        }
    }
}
