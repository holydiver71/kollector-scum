using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.DTOs;
using System.Linq.Expressions;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing countries
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CountriesController : ControllerBase
    {
        private readonly IRepository<Country> _countryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CountriesController> _logger;

        /// <summary>
        /// Initializes a new instance of the CountriesController
        /// </summary>
        /// <param name="countryRepository">The country repository</param>
        /// <param name="unitOfWork">The unit of work</param>
        /// <param name="logger">The logger</param>
        public CountriesController(
            IRepository<Country> countryRepository,
            IUnitOfWork unitOfWork,
            ILogger<CountriesController> logger)
        {
            _countryRepository = countryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Gets all countries with optional filtering and pagination
        /// </summary>
        /// <param name="search">Optional search term to filter by name</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50, max: 100)</param>
        /// <returns>List of countries</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CountryDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<CountryDto>>> GetCountries(
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

                _logger.LogInformation("Getting countries - Page: {Page}, PageSize: {PageSize}, Search: {Search}",
                    page, pageSize, search);

                // Build filter expression
                var filter = !string.IsNullOrEmpty(search) 
            ? (Expression<Func<Country, bool>>)(c => c.Name.ToLower().Contains(search.ToLower()))
            : null;

                // Get paginated results
                var pagedResult = await _countryRepository.GetPagedAsync(
                    pageNumber: page,
                    pageSize: pageSize,
                    filter: filter,
                    orderBy: query => query.OrderBy(c => c.Name));

                // Map to DTOs
                var countryDtos = pagedResult.Items.Select(c => new CountryDto
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList();

                var result = new PagedResult<CountryDto>
                {
                    Items = countryDtos,
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount,
                    TotalPages = pagedResult.TotalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting countries");
                return StatusCode(500, "An error occurred while retrieving countries");
            }
        }

        /// <summary>
        /// Gets a specific country by ID
        /// </summary>
        /// <param name="id">The country ID</param>
        /// <returns>The country</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CountryDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<CountryDto>> GetCountry(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Country ID must be greater than 0");
                }

                _logger.LogInformation("Getting country with ID: {CountryId}", id);

                var country = await _countryRepository.GetByIdAsync(id);
                if (country == null)
                {
                    return NotFound($"Country with ID {id} not found");
                }

                var countryDto = new CountryDto
                {
                    Id = country.Id,
                    Name = country.Name
                };

                return Ok(countryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting country with ID: {CountryId}", id);
                return StatusCode(500, "An error occurred while retrieving the country");
            }
        }

        /// <summary>
        /// Creates a new country
        /// </summary>
        /// <param name="createCountryDto">The country data</param>
        /// <returns>The created country</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CountryDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<CountryDto>> CreateCountry([FromBody] CreateCountryDto createCountryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Creating country: {CountryName}", createCountryDto.Name);

                // Check if country already exists
                var existingCountry = await _countryRepository.GetFirstOrDefaultAsync(
                    c => c.Name.ToLower() == createCountryDto.Name.ToLower());

                if (existingCountry != null)
                {
                    return Conflict($"Country with name '{createCountryDto.Name}' already exists");
                }

                var country = new Country
                {
                    Name = createCountryDto.Name.Trim()
                };

                await _countryRepository.AddAsync(country);
                await _unitOfWork.SaveChangesAsync();

                var countryDto = new CountryDto
                {
                    Id = country.Id,
                    Name = country.Name
                };

                return CreatedAtAction(nameof(GetCountry), new { id = country.Id }, countryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating country: {CountryName}", createCountryDto.Name);
                return StatusCode(500, "An error occurred while creating the country");
            }
        }

        /// <summary>
        /// Updates an existing country
        /// </summary>
        /// <param name="id">The country ID</param>
        /// <param name="updateCountryDto">The updated country data</param>
        /// <returns>The updated country</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(CountryDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<CountryDto>> UpdateCountry(int id, [FromBody] UpdateCountryDto updateCountryDto)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Country ID must be greater than 0");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Updating country ID: {CountryId}", id);

                var country = await _countryRepository.GetByIdAsync(id);
                if (country == null)
                {
                    return NotFound($"Country with ID {id} not found");
                }

                // Check if another country with the same name exists
                var existingCountry = await _countryRepository.GetFirstOrDefaultAsync(
                    c => c.Name.ToLower() == updateCountryDto.Name.ToLower() && c.Id != id);

                if (existingCountry != null)
                {
                    return Conflict($"Another country with name '{updateCountryDto.Name}' already exists");
                }

                country.Name = updateCountryDto.Name.Trim();
                _countryRepository.Update(country);
                await _unitOfWork.SaveChangesAsync();

                var countryDto = new CountryDto
                {
                    Id = country.Id,
                    Name = country.Name
                };

                return Ok(countryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating country ID: {CountryId}", id);
                return StatusCode(500, "An error occurred while updating the country");
            }
        }

        /// <summary>
        /// Deletes a country
        /// </summary>
        /// <param name="id">The country ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> DeleteCountry(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Country ID must be greater than 0");
                }

                _logger.LogInformation("Deleting country ID: {CountryId}", id);

                var country = await _countryRepository.GetByIdAsync(id);
                if (country == null)
                {
                    return NotFound($"Country with ID {id} not found");
                }

                // Check if country is referenced by any music releases
                var hasReferences = await _countryRepository.AnyAsync(
                    c => c.Id == id && c.MusicReleases.Any());

                if (hasReferences)
                {
                    return Conflict("Cannot delete country that is referenced by music releases");
                }

                await _countryRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting country ID: {CountryId}", id);
                return StatusCode(500, "An error occurred while deleting the country");
            }
        }
    }
}
