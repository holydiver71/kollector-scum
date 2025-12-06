using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KollectorScum.Api.Data;
using KollectorScum.Api.Models;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing user-defined music release collections (Kollections)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class KollectionsController : ControllerBase
    {
        private readonly KollectorScumDbContext _context;
        private readonly ILogger<KollectionsController> _logger;

        public KollectionsController(
            KollectorScumDbContext context,
            ILogger<KollectionsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all kollections with item counts
        /// </summary>
        /// <returns>List of kollection summaries</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<KollectionSummaryDto>), 200)]
        public async Task<ActionResult<List<KollectionSummaryDto>>> GetKollections()
        {
            try
            {
                var kollections = await _context.Kollections
                    .Include(k => k.Items)
                    .Select(k => new KollectionSummaryDto
                    {
                        Id = k.Id,
                        Name = k.Name,
                        CreatedAt = k.CreatedAt,
                        LastModified = k.LastModified,
                        ItemCount = k.Items.Count
                    })
                    .OrderBy(k => k.Name)
                    .ToListAsync();

                return Ok(kollections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting kollections");
                return StatusCode(500, "An error occurred while retrieving kollections");
            }
        }

        /// <summary>
        /// Gets a specific kollection with all its releases
        /// </summary>
        /// <param name="id">Kollection ID</param>
        /// <returns>Kollection details with releases</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(KollectionDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<KollectionDto>> GetKollection(int id)
        {
            try
            {
                var kollection = await _context.Kollections
                    .Include(k => k.Items)
                        .ThenInclude(ki => ki.MusicRelease)
                            .ThenInclude(mr => mr.Label)
                    .Include(k => k.Items)
                        .ThenInclude(ki => ki.MusicRelease)
                            .ThenInclude(mr => mr.Country)
                    .Include(k => k.Items)
                        .ThenInclude(ki => ki.MusicRelease)
                            .ThenInclude(mr => mr.Format)
                    .FirstOrDefaultAsync(k => k.Id == id);

                if (kollection == null)
                {
                    _logger.LogWarning("Kollection not found: {Id}", id);
                    return NotFound($"Kollection with ID {id} not found");
                }

                var dto = new KollectionDto
                {
                    Id = kollection.Id,
                    Name = kollection.Name,
                    CreatedAt = kollection.CreatedAt,
                    LastModified = kollection.LastModified,
                    Releases = kollection.Items.Select(ki => MapToMusicReleaseSummaryDto(ki.MusicRelease)).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting kollection by ID: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the kollection");
            }
        }

        /// <summary>
        /// Creates a new kollection
        /// </summary>
        /// <param name="createDto">Kollection data</param>
        /// <returns>Created kollection</returns>
        [HttpPost]
        [ProducesResponseType(typeof(KollectionSummaryDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<KollectionSummaryDto>> CreateKollection([FromBody] CreateKollectionDto createDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(createDto.Name))
                {
                    return BadRequest("Kollection name is required");
                }

                // Check for duplicate name
                var existingKollection = await _context.Kollections
                    .FirstOrDefaultAsync(k => k.Name.ToLower() == createDto.Name.ToLower());

                if (existingKollection != null)
                {
                    return BadRequest($"A kollection with the name '{createDto.Name}' already exists");
                }

                var kollection = new Kollection
                {
                    Name = createDto.Name,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                _context.Kollections.Add(kollection);
                await _context.SaveChangesAsync();

                var dto = new KollectionSummaryDto
                {
                    Id = kollection.Id,
                    Name = kollection.Name,
                    CreatedAt = kollection.CreatedAt,
                    LastModified = kollection.LastModified,
                    ItemCount = 0
                };

                return CreatedAtAction(nameof(GetKollection), new { id = kollection.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating kollection");
                return StatusCode(500, "An error occurred while creating the kollection");
            }
        }

        /// <summary>
        /// Updates a kollection's name
        /// </summary>
        /// <param name="id">Kollection ID</param>
        /// <param name="updateDto">Updated kollection data</param>
        /// <returns>Updated kollection</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(KollectionSummaryDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<KollectionSummaryDto>> UpdateKollection(int id, [FromBody] UpdateKollectionDto updateDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(updateDto.Name))
                {
                    return BadRequest("Kollection name is required");
                }

                var kollection = await _context.Kollections
                    .Include(k => k.Items)
                    .FirstOrDefaultAsync(k => k.Id == id);

                if (kollection == null)
                {
                    return NotFound($"Kollection with ID {id} not found");
                }

                // Check for duplicate name (excluding current kollection)
                var existingKollection = await _context.Kollections
                    .FirstOrDefaultAsync(k => k.Id != id && k.Name.ToLower() == updateDto.Name.ToLower());

                if (existingKollection != null)
                {
                    return BadRequest($"A kollection with the name '{updateDto.Name}' already exists");
                }

                kollection.Name = updateDto.Name;
                kollection.LastModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var dto = new KollectionSummaryDto
                {
                    Id = kollection.Id,
                    Name = kollection.Name,
                    CreatedAt = kollection.CreatedAt,
                    LastModified = kollection.LastModified,
                    ItemCount = kollection.Items.Count
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating kollection: {Id}", id);
                return StatusCode(500, "An error occurred while updating the kollection");
            }
        }

        /// <summary>
        /// Deletes a kollection
        /// </summary>
        /// <param name="id">Kollection ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteKollection(int id)
        {
            try
            {
                var kollection = await _context.Kollections.FindAsync(id);

                if (kollection == null)
                {
                    return NotFound($"Kollection with ID {id} not found");
                }

                _context.Kollections.Remove(kollection);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting kollection: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the kollection");
            }
        }

        /// <summary>
        /// Adds a music release to a kollection (either existing or new)
        /// </summary>
        /// <param name="dto">Add to kollection data</param>
        /// <returns>Updated kollection summary</returns>
        [HttpPost("add-release")]
        [ProducesResponseType(typeof(KollectionSummaryDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<KollectionSummaryDto>> AddReleaseToKollection([FromBody] AddToKollectionDto dto)
        {
            try
            {
                // Validate that music release exists
                var releaseExists = await _context.MusicReleases.AnyAsync(mr => mr.Id == dto.MusicReleaseId);
                if (!releaseExists)
                {
                    return NotFound($"Music release with ID {dto.MusicReleaseId} not found");
                }

                Kollection kollection;

                // If creating a new kollection
                if (dto.KollectionId == null)
                {
                    if (string.IsNullOrWhiteSpace(dto.NewKollectionName))
                    {
                        return BadRequest("New kollection name is required when creating a new kollection");
                    }

                    // Check for duplicate name
                    var existingKollection = await _context.Kollections
                        .FirstOrDefaultAsync(k => k.Name.ToLower() == dto.NewKollectionName.ToLower());

                    if (existingKollection != null)
                    {
                        // If kollection exists, use it instead of creating duplicate
                        kollection = existingKollection;
                    }
                    else
                    {
                        // Create new kollection
                        kollection = new Kollection
                        {
                            Name = dto.NewKollectionName,
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        };
                        _context.Kollections.Add(kollection);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Use existing kollection
                    kollection = await _context.Kollections
                        .Include(k => k.Items)
                        .FirstOrDefaultAsync(k => k.Id == dto.KollectionId.Value);

                    if (kollection == null)
                    {
                        return NotFound($"Kollection with ID {dto.KollectionId.Value} not found");
                    }
                }

                // Check if release already exists in kollection
                var existingItem = await _context.KollectionItems
                    .FirstOrDefaultAsync(ki => ki.KollectionId == kollection.Id && ki.MusicReleaseId == dto.MusicReleaseId);

                if (existingItem != null)
                {
                    return BadRequest($"Release is already in this kollection");
                }

                // Add release to kollection
                var kollectionItem = new KollectionItem
                {
                    KollectionId = kollection.Id,
                    MusicReleaseId = dto.MusicReleaseId,
                    AddedAt = DateTime.UtcNow
                };

                _context.KollectionItems.Add(kollectionItem);
                kollection.LastModified = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Return updated kollection summary
                var itemCount = await _context.KollectionItems.CountAsync(ki => ki.KollectionId == kollection.Id);

                var summaryDto = new KollectionSummaryDto
                {
                    Id = kollection.Id,
                    Name = kollection.Name,
                    CreatedAt = kollection.CreatedAt,
                    LastModified = kollection.LastModified,
                    ItemCount = itemCount
                };

                return Ok(summaryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding release to kollection");
                return StatusCode(500, "An error occurred while adding the release to the kollection");
            }
        }

        /// <summary>
        /// Removes a music release from a kollection
        /// </summary>
        /// <param name="kollectionId">Kollection ID</param>
        /// <param name="releaseId">Music release ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{kollectionId}/releases/{releaseId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RemoveReleaseFromKollection(int kollectionId, int releaseId)
        {
            try
            {
                var kollectionItem = await _context.KollectionItems
                    .FirstOrDefaultAsync(ki => ki.KollectionId == kollectionId && ki.MusicReleaseId == releaseId);

                if (kollectionItem == null)
                {
                    return NotFound($"Release not found in kollection");
                }

                _context.KollectionItems.Remove(kollectionItem);

                // Update kollection's last modified date
                var kollection = await _context.Kollections.FindAsync(kollectionId);
                if (kollection != null)
                {
                    kollection.LastModified = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing release from kollection: {KollectionId}, {ReleaseId}", kollectionId, releaseId);
                return StatusCode(500, "An error occurred while removing the release from the kollection");
            }
        }

        /// <summary>
        /// Maps a MusicRelease entity to a MusicReleaseSummaryDto
        /// </summary>
        private MusicReleaseSummaryDto MapToMusicReleaseSummaryDto(MusicRelease release)
        {
            // Parse artist names from JSON
            List<string>? artistNames = null;
            if (!string.IsNullOrEmpty(release.Artists))
            {
                try
                {
                    var artistIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(release.Artists);
                    if (artistIds != null && artistIds.Any())
                    {
                        artistNames = _context.Artists
                            .Where(a => artistIds.Contains(a.Id))
                            .Select(a => a.Name)
                            .ToList();
                    }
                }
                catch { }
            }

            // Parse genre names from JSON
            List<string>? genreNames = null;
            if (!string.IsNullOrEmpty(release.Genres))
            {
                try
                {
                    var genreIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(release.Genres);
                    if (genreIds != null && genreIds.Any())
                    {
                        genreNames = _context.Genres
                            .Where(g => genreIds.Contains(g.Id))
                            .Select(g => g.Name)
                            .ToList();
                    }
                }
                catch { }
            }

            // Parse cover image from JSON
            string? coverImageUrl = null;
            if (!string.IsNullOrEmpty(release.Images))
            {
                try
                {
                    var images = System.Text.Json.JsonSerializer.Deserialize<Models.ValueObjects.Images>(release.Images);
                    coverImageUrl = images?.Thumbnail ?? images?.CoverFront;
                }
                catch { }
            }

            return new MusicReleaseSummaryDto
            {
                Id = release.Id,
                Title = release.Title,
                ReleaseYear = release.ReleaseYear,
                OrigReleaseYear = release.OrigReleaseYear,
                ArtistNames = artistNames,
                GenreNames = genreNames,
                LabelName = release.Label?.Name,
                CountryName = release.Country?.Name,
                FormatName = release.Format?.Name,
                CoverImageUrl = coverImageUrl,
                DateAdded = release.DateAdded
            };
        }
    }
}
