using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing kollections
    /// </summary>
    public class KollectionsController : BaseApiController
    {
        private readonly IKollectionService _kollectionService;

        public KollectionsController(
            IKollectionService kollectionService,
            ILogger<KollectionsController> logger)
            : base(logger)
        {
            _kollectionService = kollectionService ?? throw new ArgumentNullException(nameof(kollectionService));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<KollectionDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<KollectionDto>>> GetKollections(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var validationError = ValidatePaginationParameters(page, pageSize);
                if (validationError != null) return validationError;

                LogOperation("GetKollections", new { search, page, pageSize });

                var result = await _kollectionService.GetAllAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetKollections));
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(KollectionDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<KollectionDto>> GetKollection(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Kollection ID must be greater than 0");

                LogOperation("GetKollection", new { id });

                var kollectionDto = await _kollectionService.GetByIdAsync(id);
                if (kollectionDto == null)
                {
                    return NotFound($"Kollection with ID {id} not found");
                }

                return Ok(kollectionDto);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetKollection));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(KollectionDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<KollectionDto>> CreateKollection([FromBody] CreateKollectionDto createKollectionDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("CreateKollection", new { name = createKollectionDto.Name, genreCount = createKollectionDto.GenreIds.Count });

                var kollectionDto = await _kollectionService.CreateAsync(createKollectionDto);
                return CreatedAtAction(nameof(GetKollection), new { id = kollectionDto.Id }, kollectionDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(CreateKollection));
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(KollectionDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<KollectionDto>> UpdateKollection(int id, [FromBody] UpdateKollectionDto updateKollectionDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Kollection ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("UpdateKollection", new { id, name = updateKollectionDto.Name, genreCount = updateKollectionDto.GenreIds.Count });

                var kollectionDto = await _kollectionService.UpdateAsync(id, updateKollectionDto);
                if (kollectionDto == null)
                {
                    return NotFound($"Kollection with ID {id} not found");
                }

                return Ok(kollectionDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(UpdateKollection));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteKollection(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Kollection ID must be greater than 0");

                LogOperation("DeleteKollection", new { id });

                var deleted = await _kollectionService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Kollection with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(DeleteKollection));
            }
        }
    }
}
