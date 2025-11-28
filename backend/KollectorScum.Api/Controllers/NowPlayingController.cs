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
        /// Gets the play history for a music release including count and all play dates
        /// </summary>
        /// <param name="releaseId">The music release ID</param>
        /// <returns>Play history with count and list of dates</returns>
        [HttpGet("release/{releaseId}/history")]
        [ProducesResponseType(typeof(PlayHistoryDto), 200)]
        public async Task<ActionResult<PlayHistoryDto>> GetPlayHistoryForRelease(int releaseId)
        {
            try
            {
                var playedDates = await _context.NowPlayings
                    .Where(np => np.MusicReleaseId == releaseId)
                    .OrderByDescending(np => np.PlayedAt)
                    .Select(np => np.PlayedAt)
                    .ToListAsync();

                var result = new PlayHistoryDto
                {
                    MusicReleaseId = releaseId,
                    PlayCount = playedDates.Count,
                    PlayedDates = playedDates
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting play history for release {ReleaseId}", releaseId);
                return StatusCode(500, "An error occurred while retrieving the play history");
            }
        }
    }
}
