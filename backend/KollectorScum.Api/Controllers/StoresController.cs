using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.DTOs;
using System.Linq.Expressions;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing stores
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class StoresController : ControllerBase
    {
        private readonly IRepository<Store> _storeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StoresController> _logger;

        /// <summary>
        /// Initializes a new instance of the StoresController
        /// </summary>
        /// <param name="storeRepository">The store repository</param>
        /// <param name="unitOfWork">The unit of work</param>
        /// <param name="logger">The logger</param>
        public StoresController(
            IRepository<Store> storeRepository,
            IUnitOfWork unitOfWork,
            ILogger<StoresController> logger)
        {
            _storeRepository = storeRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Gets all stores with optional filtering and pagination
        /// </summary>
        /// <param name="search">Optional search term to filter by name</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50, max: 100)</param>
        /// <returns>List of stores</returns>
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
                // Validate pagination parameters
                if (page < 1)
                {
                    return BadRequest("Page must be greater than 0");
                }

                if (pageSize < 1 || pageSize > 5000)
                {
                    return BadRequest("Page size must be between 1 and 5000");
                }

                _logger.LogInformation("Getting stores - Page: {Page}, PageSize: {PageSize}, Search: {Search}",
                    page, pageSize, search);

                // Build filter expression
                Expression<Func<Store, bool>>? filter = null;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filter = s => s.Name.ToLower().Contains(search.ToLower());
                }

                // Get paginated results
                var pagedResult = await _storeRepository.GetPagedAsync(
                    pageNumber: page,
                    pageSize: pageSize,
                    filter: filter,
                    orderBy: query => query.OrderBy(s => s.Name));

                // Map to DTOs
                var storeDtos = pagedResult.Items.Select(s => new StoreDto
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList();

                var result = new PagedResult<StoreDto>
                {
                    Items = storeDtos,
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount,
                    TotalPages = pagedResult.TotalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stores");
                return StatusCode(500, "An error occurred while retrieving stores");
            }
        }

        /// <summary>
        /// Gets a specific store by ID
        /// </summary>
        /// <param name="id">The store ID</param>
        /// <returns>The store</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(StoreDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<StoreDto>> GetStore(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Store ID must be greater than 0");
                }

                _logger.LogInformation("Getting store with ID: {StoreId}", id);

                var store = await _storeRepository.GetByIdAsync(id);
                if (store == null)
                {
                    return NotFound($"Store with ID {id} not found");
                }

                var storeDto = new StoreDto
                {
                    Id = store.Id,
                    Name = store.Name
                };

                return Ok(storeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting store with ID: {StoreId}", id);
                return StatusCode(500, "An error occurred while retrieving the store");
            }
        }

        /// <summary>
        /// Creates a new store
        /// </summary>
        /// <param name="createStoreDto">The store data</param>
        /// <returns>The created store</returns>
        [HttpPost]
        [ProducesResponseType(typeof(StoreDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<StoreDto>> CreateStore([FromBody] CreateStoreDto createStoreDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Creating store: {StoreName}", createStoreDto.Name);

                // Check if store already exists
                var existingStore = await _storeRepository.GetFirstOrDefaultAsync(
                    s => s.Name.ToLower() == createStoreDto.Name.ToLower());

                if (existingStore != null)
                {
                    return Conflict($"Store with name '{createStoreDto.Name}' already exists");
                }

                var store = new Store
                {
                    Name = createStoreDto.Name.Trim()
                };

                await _storeRepository.AddAsync(store);
                await _unitOfWork.SaveChangesAsync();

                var storeDto = new StoreDto
                {
                    Id = store.Id,
                    Name = store.Name
                };

                return CreatedAtAction(nameof(GetStore), new { id = store.Id }, storeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating store: {StoreName}", createStoreDto.Name);
                return StatusCode(500, "An error occurred while creating the store");
            }
        }

        /// <summary>
        /// Updates an existing store
        /// </summary>
        /// <param name="id">The store ID</param>
        /// <param name="updateStoreDto">The updated store data</param>
        /// <returns>The updated store</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(StoreDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<StoreDto>> UpdateStore(int id, [FromBody] UpdateStoreDto updateStoreDto)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Store ID must be greater than 0");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Updating store ID: {StoreId}", id);

                var store = await _storeRepository.GetByIdAsync(id);
                if (store == null)
                {
                    return NotFound($"Store with ID {id} not found");
                }

                // Check if another store with the same name exists
                var existingStore = await _storeRepository.GetFirstOrDefaultAsync(
                    s => s.Name.ToLower() == updateStoreDto.Name.ToLower() && s.Id != id);

                if (existingStore != null)
                {
                    return Conflict($"Another store with name '{updateStoreDto.Name}' already exists");
                }

                store.Name = updateStoreDto.Name.Trim();
                _storeRepository.Update(store);
                await _unitOfWork.SaveChangesAsync();

                var storeDto = new StoreDto
                {
                    Id = store.Id,
                    Name = store.Name
                };

                return Ok(storeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store ID: {StoreId}", id);
                return StatusCode(500, "An error occurred while updating the store");
            }
        }

        /// <summary>
        /// Deletes a store
        /// </summary>
        /// <param name="id">The store ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> DeleteStore(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Store ID must be greater than 0");
                }

                _logger.LogInformation("Deleting store ID: {StoreId}", id);

                var store = await _storeRepository.GetByIdAsync(id);
                if (store == null)
                {
                    return NotFound($"Store with ID {id} not found");
                }

                // Check if store is referenced by any music releases
                var hasReferences = await _storeRepository.AnyAsync(
                    s => s.Id == id && s.MusicReleases.Any());

                if (hasReferences)
                {
                    return Conflict("Cannot delete store that is referenced by music releases");
                }

                await _storeRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting store ID: {StoreId}", id);
                return StatusCode(500, "An error occurred while deleting the store");
            }
        }
    }
}
