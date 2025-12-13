using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing stores
    /// </summary>
    [Authorize]
    public class StoresController : BaseApiController
    {
        private readonly IGenericCrudService<Models.Store, StoreDto> _storeService;

        public StoresController(
            IGenericCrudService<Models.Store, StoreDto> storeService,
            ILogger<StoresController> logger)
            : base(logger)
        {
            _storeService = storeService ?? throw new ArgumentNullException(nameof(storeService));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<StoreDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<StoreDto>>> GetStores(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var validationError = ValidatePaginationParameters(page, pageSize);
                if (validationError != null) return validationError;

                LogOperation("GetStores", new { search, page, pageSize });

                var result = await _storeService.GetAllAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetStores));
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(StoreDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<StoreDto>> GetStore(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Store ID must be greater than 0");

                LogOperation("GetStore", new { id });

                var storeDto = await _storeService.GetByIdAsync(id);
                if (storeDto == null)
                {
                    return NotFound($"Store with ID {id} not found");
                }

                return Ok(storeDto);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetStore));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(StoreDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<StoreDto>> CreateStore([FromBody] StoreDto createStoreDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("CreateStore", new { name = createStoreDto.Name });

                var storeDto = await _storeService.CreateAsync(createStoreDto);
                return CreatedAtAction(nameof(GetStore), new { id = storeDto.Id }, storeDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(CreateStore));
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(StoreDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<StoreDto>> UpdateStore(int id, [FromBody] StoreDto updateStoreDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Store ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("UpdateStore", new { id, name = updateStoreDto.Name });

                var storeDto = await _storeService.UpdateAsync(id, updateStoreDto);
                if (storeDto == null)
                {
                    return NotFound($"Store with ID {id} not found");
                }

                return Ok(storeDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(UpdateStore));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteStore(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Store ID must be greater than 0");

                LogOperation("DeleteStore", new { id });

                var deleted = await _storeService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Store with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(DeleteStore));
            }
        }
    }
}
