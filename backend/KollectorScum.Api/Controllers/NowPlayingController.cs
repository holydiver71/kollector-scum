using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KollectorScum.Api.Data;
using KollectorScum.Api.Models;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;

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
        private readonly IMusicReleaseMapperService _mapperService;
        private readonly IUserContext _userContext;

        public NowPlayingController(
            KollectorScumDbContext context,
            ILogger<NowPlayingController> logger,
            IMusicReleaseMapperService mapperService,
            IUserContext userContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapperService = mapperService ?? throw new ArgumentNullException(nameof(mapperService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
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
                // We want to show a recent sequence of plays. The "xN" badge should only
                // indicate consecutive back-to-back plays that happened on the same day.
                // To do that we fetch a buffer of recent NowPlaying records (newest first),
                // then collapse adjacent entries that refer to the same release and share
                // the same date. Finally return up to `limit` items.

                var fetchLimit = Math.Min(limit * 10, 1000); // buffer to allow collapsing

                // Enforce multi-tenant isolation: only return plays for the current user's releases
                var userId = _userContext.GetActingUserId();
                if (userId == null)
                {
                    return Unauthorized();
                }

                var recentPlays = await _context.NowPlayings
                    .Include(np => np.MusicRelease)
                    .Where(np => np.MusicRelease != null && np.MusicRelease.UserId == userId.Value)
                    .OrderByDescending(np => np.PlayedAt)
                    .Take(fetchLimit)
                    .ToListAsync();

                var collapsed = new List<RecentlyPlayedItemDto>();

                foreach (var np in recentPlays)
                {
                    string? coverFront = null;
                    if (np.MusicRelease?.Images != null)
                    {
                        try
                        {
                            var images = System.Text.Json.JsonSerializer.Deserialize<MusicReleaseImageDto>(
                                np.MusicRelease.Images,
                                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            // Resolve the stored filename/path to a proper public URL so that R2-backed
                            // staging environments return the correct https:// URL instead of a bare filename.
                            var releaseUserId = np.MusicRelease.UserId;
                            coverFront = _mapperService.ResolveImageUrl(images?.CoverFront, releaseUserId);
                        }
                        catch
                        {
                            // ignore parse errors
                        }
                    }

                    if (collapsed.Count == 0)
                    {
                        collapsed.Add(new RecentlyPlayedItemDto
                        {
                            Id = np.MusicReleaseId,
                            CoverFront = coverFront,
                            PlayedAt = np.PlayedAt,
                            PlayCount = 1
                        });
                        continue;
                    }

                    var last = collapsed.Last();

                    // Consider as back-to-back same-day repeat only if the same release
                    // and the PlayedAt dates (UTC) fall on the same calendar day
                    if (last.Id == np.MusicReleaseId && last.PlayedAt.Date == np.PlayedAt.Date)
                    {
                        last.PlayCount += 1;
                        // Keep the PlayedAt as the most recent (already the case since we iterate desc)
                    }
                    else
                    {
                        collapsed.Add(new RecentlyPlayedItemDto
                        {
                            Id = np.MusicReleaseId,
                            CoverFront = coverFront,
                            PlayedAt = np.PlayedAt,
                            PlayCount = 1
                        });
                    }
                }

                var result = collapsed.Take(limit).ToList();
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
