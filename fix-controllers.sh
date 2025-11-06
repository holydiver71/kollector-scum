#!/bin/bash

# Copy ArtistsController pattern to create other controllers
for entity in "Country:Countries" "Format:Formats" "Packaging:Packagings" "Store:Stores"; do
    IFS=':' read -r singular plural <<< "$entity"
    singular_lower=$(echo "$singular" | tr '[:upper:]' '[:lower:]')
    
    cat > "backend/KollectorScum.Api/Controllers/${plural}Controller.cs" << EOF
using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing ${singular_lower}s
    /// </summary>
    public class ${plural}Controller : BaseApiController
    {
        private readonly IGenericCrudService<Models.${singular}, ${singular}Dto> _${singular_lower}Service;

        public ${plural}Controller(
            IGenericCrudService<Models.${singular}, ${singular}Dto> ${singular_lower}Service,
            ILogger<${plural}Controller> logger)
            : base(logger)
        {
            _${singular_lower}Service = ${singular_lower}Service ?? throw new ArgumentNullException(nameof(${singular_lower}Service));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<${singular}Dto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<${singular}Dto>>> Get${plural}(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var validationError = ValidatePaginationParameters(page, pageSize);
                if (validationError != null) return validationError;

                LogOperation("Get${plural}", new { search, page, pageSize });

                var result = await _${singular_lower}Service.GetAllAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(Get${plural}));
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(${singular}Dto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<${singular}Dto>> Get${singular}(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("${singular} ID must be greater than 0");

                LogOperation("Get${singular}", new { id });

                var ${singular_lower}Dto = await _${singular_lower}Service.GetByIdAsync(id);
                if (${singular_lower}Dto == null)
                {
                    return NotFound(\$"${singular} with ID {id} not found");
                }

                return Ok(${singular_lower}Dto);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(Get${singular}));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(${singular}Dto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<${singular}Dto>> Create${singular}([FromBody] ${singular}Dto create${singular}Dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("Create${singular}", new { name = create${singular}Dto.Name });

                var ${singular_lower}Dto = await _${singular_lower}Service.CreateAsync(create${singular}Dto);
                return CreatedAtAction(nameof(Get${singular}), new { id = ${singular_lower}Dto.Id }, ${singular_lower}Dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(Create${singular}));
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(${singular}Dto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<${singular}Dto>> Update${singular}(int id, [FromBody] ${singular}Dto update${singular}Dto)
        {
            try
            {
                if (id <= 0) return BadRequest("${singular} ID must be greater than 0");
                if (!ModelState.IsValid) return BadRequest(ModelState);

                LogOperation("Update${singular}", new { id, name = update${singular}Dto.Name });

                var ${singular_lower}Dto = await _${singular_lower}Service.UpdateAsync(id, update${singular}Dto);
                if (${singular_lower}Dto == null)
                {
                    return NotFound(\$"${singular} with ID {id} not found");
                }

                return Ok(${singular_lower}Dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(Update${singular}));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete${singular}(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("${singular} ID must be greater than 0");

                LogOperation("Delete${singular}", new { id });

                var deleted = await _${singular_lower}Service.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound(\$"${singular} with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError(ex, nameof(Delete${singular}));
            }
        }
    }
}
EOF
    echo "Created ${plural}Controller.cs"
done

echo "All controllers created!"
