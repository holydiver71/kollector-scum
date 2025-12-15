using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing packagings
    /// </summary>
    [Authorize]
    public class PackagingsController : BaseApiController
    {
        private readonly IGenericCrudService<Models.Packaging, PackagingDto> _packagingService;

        public PackagingsController(
            IGenericCrudService<Models.Packaging, PackagingDto> packagingService,
            ILogger<PackagingsController> logger)
            : base(logger)
        {
            _packagingService = packagingService ?? throw new ArgumentNullException(nameof(packagingService));
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
                var validationError = ValidatePaginationParameters(page, pageSize);
                if (validationError != null) return validationError;

                LogOperation("GetPackagings", new { search, page, pageSize });

                var result = await _packagingService.GetAllAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetPackagings));
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

                LogOperation("GetPackaging", new { id });

                var packagingDto = await _packagingService.GetByIdAsync(id);
                if (packagingDto == null)
                {
                    return NotFound($"Packaging with ID {id} not found");
                }

                return Ok(packagingDto);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetPackaging));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(PackagingDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PackagingDto>> CreatePackaging([FromBody] PackagingDto createPackagingDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("CreatePackaging", new { name = createPackagingDto.Name });

                var packagingDto = await _packagingService.CreateAsync(createPackagingDto);
                return CreatedAtAction(nameof(GetPackaging), new { id = packagingDto.Id }, packagingDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(CreatePackaging));
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(PackagingDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PackagingDto>> UpdatePackaging(int id, [FromBody] PackagingDto updatePackagingDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Packaging ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("UpdatePackaging", new { id, name = updatePackagingDto.Name });

                var packagingDto = await _packagingService.UpdateAsync(id, updatePackagingDto);
                if (packagingDto == null)
                {
                    return NotFound($"Packaging with ID {id} not found");
                }

                return Ok(packagingDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(UpdatePackaging));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePackaging(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Packaging ID must be greater than 0");

                LogOperation("DeletePackaging", new { id });

                var deleted = await _packagingService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Packaging with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(DeletePackaging));
            }
        }
    }
}
