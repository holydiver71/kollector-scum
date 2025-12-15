using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing countrys
    /// </summary>
    [Authorize]
    public class CountriesController : BaseApiController
    {
        private readonly IGenericCrudService<Models.Country, CountryDto> _countryService;

        public CountriesController(
            IGenericCrudService<Models.Country, CountryDto> countryService,
            ILogger<CountriesController> logger)
            : base(logger)
        {
            _countryService = countryService ?? throw new ArgumentNullException(nameof(countryService));
        }

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
                var validationError = ValidatePaginationParameters(page, pageSize);
                if (validationError != null) return validationError;

                LogOperation("GetCountries", new { search, page, pageSize });

                var result = await _countryService.GetAllAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetCountries));
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CountryDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<CountryDto>> GetCountry(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Country ID must be greater than 0");

                LogOperation("GetCountry", new { id });

                var countryDto = await _countryService.GetByIdAsync(id);
                if (countryDto == null)
                {
                    return NotFound($"Country with ID {id} not found");
                }

                return Ok(countryDto);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(GetCountry));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(CountryDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<CountryDto>> CreateCountry([FromBody] CountryDto createCountryDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("CreateCountry", new { name = createCountryDto.Name });

                var countryDto = await _countryService.CreateAsync(createCountryDto);
                return CreatedAtAction(nameof(GetCountry), new { id = countryDto.Id }, countryDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(CreateCountry));
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(CountryDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CountryDto>> UpdateCountry(int id, [FromBody] CountryDto updateCountryDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Country ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("UpdateCountry", new { id, name = updateCountryDto.Name });

                var countryDto = await _countryService.UpdateAsync(id, updateCountryDto);
                if (countryDto == null)
                {
                    return NotFound($"Country with ID {id} not found");
                }

                return Ok(countryDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(UpdateCountry));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteCountry(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("Country ID must be greater than 0");

                LogOperation("DeleteCountry", new { id });

                var deleted = await _countryService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Country with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(DeleteCountry));
            }
        }
    }
}
