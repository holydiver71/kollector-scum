using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KollectorScum.Api.Data;
using KollectorScum.Api.Models;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing now playing records
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NowPlayingController : ControllerBase
    {
        private readonly KollectorScumDbContext _context;
        private readonly ILogger<NowPlayingController> _logger;

        public NowPlayingController(
            KollectorScumDbContext context,
            ILogger<NowPlayingController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Records a new now playing entry for a music release
        /// </summary>
        /// <param name="createDto">The now playing data</param>
        /// <returns>The created now playing record</returns>
        [HttpPost]
        [ProducesResponseType(typeof(NowPlayingDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<NowPlayingDto>> CreateNowPlaying([FromBody] CreateNowPlayingDto createDto)
        {
            try
            {
                // Verify that the music release exists
                var releaseExists = await _context.MusicReleases
                    .AnyAsync(mr => mr.Id == createDto.MusicReleaseId);

                if (!releaseExists)
                {
                    _logger.LogWarning("Music release not found: {MusicReleaseId}", createDto.MusicReleaseId);
                    return NotFound($"Music release with ID {createDto.MusicReleaseId} not found");
                }

                var nowPlaying = new NowPlaying
                {
                    MusicReleaseId = createDto.MusicReleaseId,
                    PlayedAt = DateTime.UtcNow
                };

                _context.NowPlayings.Add(nowPlaying);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created now playing record {Id} for release {MusicReleaseId}", 
                    nowPlaying.Id, nowPlaying.MusicReleaseId);

                var dto = new NowPlayingDto
                {
                    Id = nowPlaying.Id,
                    MusicReleaseId = nowPlaying.MusicReleaseId,
                    PlayedAt = nowPlaying.PlayedAt
                };

                return CreatedAtAction(nameof(GetNowPlaying), new { id = nowPlaying.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating now playing record for release {MusicReleaseId}", 
                    createDto.MusicReleaseId);
                return StatusCode(500, "An error occurred while creating the now playing record");
            }
        }

        /// <summary>
        /// Gets a now playing record by ID
        /// </summary>
        /// <param name="id">The now playing record ID</param>
        /// <returns>The now playing record</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(NowPlayingDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<NowPlayingDto>> GetNowPlaying(int id)
        {
            try
            {
                var nowPlaying = await _context.NowPlayings
                    .FirstOrDefaultAsync(np => np.Id == id);

                if (nowPlaying == null)
                {
                    return NotFound($"Now playing record with ID {id} not found");
                }

                return Ok(new NowPlayingDto
                {
                    Id = nowPlaying.Id,
                    MusicReleaseId = nowPlaying.MusicReleaseId,
                    PlayedAt = nowPlaying.PlayedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting now playing record {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the now playing record");
            }
        }

        /// <summary>
        /// Gets the last played date for a music release
        /// </summary>
        /// <param name="releaseId">The music release ID</param>
        /// <returns>The last played date or null if never played</returns>
        [HttpGet("release/{releaseId}/last")]
        [ProducesResponseType(typeof(DateTime?), 200)]
        public async Task<ActionResult<DateTime?>> GetLastPlayedForRelease(int releaseId)
        {
            try
            {
                var lastPlayed = await _context.NowPlayings
                    .Where(np => np.MusicReleaseId == releaseId)
                    .OrderByDescending(np => np.PlayedAt)
                    .Select(np => (DateTime?)np.PlayedAt)
                    .FirstOrDefaultAsync();

                return Ok(lastPlayed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last played date for release {ReleaseId}", releaseId);
                return StatusCode(500, "An error occurred while retrieving the last played date");
            }
        }

        /// <summary>
        /// Deletes a now playing record
        /// </summary>
        /// <param name="id">The now playing record ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteNowPlaying(int id)
        {
            try
            {
                var nowPlaying = await _context.NowPlayings.FindAsync(id);
                if (nowPlaying == null)
                {
                    return NotFound($"Now playing record with ID {id} not found");
                }

                _context.NowPlayings.Remove(nowPlaying);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted now playing record {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting now playing record {Id}", id);
                return StatusCode(500, "An error occurred while deleting the now playing record");
            }
        }

        /// <summary>
        /// Gets the play history for a music release
        /// </summary>
        /// <param name="releaseId">The music release ID</param>
        /// <returns>The play count and list of all play dates</returns>
        [HttpGet("release/{releaseId}/history")]
        [ProducesResponseType(typeof(PlayHistoryDto), 200)]
        public async Task<ActionResult<PlayHistoryDto>> GetPlayHistoryForRelease(int releaseId)
        {
            try
            {
                var playDates = await _context.NowPlayings
                    .Where(np => np.MusicReleaseId == releaseId)
                    .OrderByDescending(np => np.PlayedAt)
                    .Select(np => new PlayHistoryItemDto 
                    { 
                        Id = np.Id, 
                        PlayedAt = np.PlayedAt 
                    })
                    .ToListAsync();

                var playHistory = new PlayHistoryDto
                {
                    MusicReleaseId = releaseId,
                    PlayCount = playDates.Count,
                    PlayDates = playDates
                };

                return Ok(playHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting play history for release {ReleaseId}", releaseId);
                return StatusCode(500, "An error occurred while retrieving the play history");
            }
        }

        /// <summary>
        /// Gets recently played releases with their cover images, ordered by most recent first
        /// </summary>
        /// <param name="limit">Maximum number of releases to return (default 24)</param>
        /// <returns>List of recently played releases with cover images</returns>
        [HttpGet("recent")]
        [ProducesResponseType(typeof(List<RecentlyPlayedItemDto>), 200)]
        public async Task<ActionResult<List<RecentlyPlayedItemDto>>> GetRecentlyPlayed([FromQuery] int limit = 24)
        {
            try
            {
                // Get the most recent play date and play count for each release using a subquery
                var releasePlayStats = _context.NowPlayings
                    .GroupBy(np => np.MusicReleaseId)
                    .Select(g => new { 
                        MusicReleaseId = g.Key, 
                        LatestPlayedAt = g.Max(np => np.PlayedAt),
                        PlayCount = g.Count()
                    });

                // Join back to get the full record and include the MusicRelease
                var recentlyPlayed = await _context.NowPlayings
                    .Include(np => np.MusicRelease)
                    .Join(releasePlayStats,
                        np => new { np.MusicReleaseId, LatestPlayedAt = np.PlayedAt },
                        stats => new { stats.MusicReleaseId, LatestPlayedAt = stats.LatestPlayedAt },
                        (np, stats) => new { NowPlaying = np, stats.PlayCount })
                    .OrderByDescending(x => x.NowPlaying.PlayedAt)
                    .Take(limit)
                    .ToListAsync();

                var result = recentlyPlayed.Select(x =>
                {
                    string? coverFront = null;
                    if (x.NowPlaying.MusicRelease?.Images != null)
                    {
                        try
                        {
                            var images = System.Text.Json.JsonSerializer.Deserialize<MusicReleaseImageDto>(
                                x.NowPlaying.MusicRelease.Images,
                                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            coverFront = images?.CoverFront;
                        }
                        catch
                        {
                            // If parsing fails, leave coverFront as null
                        }
                    }

                    return new RecentlyPlayedItemDto
                    {
                        Id = x.NowPlaying.MusicReleaseId,
                        CoverFront = coverFront,
                        PlayedAt = x.NowPlaying.PlayedAt,
                        PlayCount = x.PlayCount
                    };
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recently played releases");
                return StatusCode(500, "An error occurred while retrieving recently played releases");
            }
        }
    }
}
