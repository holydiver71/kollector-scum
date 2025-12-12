using Microsoft.EntityFrameworkCore;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for managing lists
    /// </summary>
    public class ListService : IListService
    {
        private readonly KollectorScumDbContext _context;
        private readonly ILogger<ListService> _logger;

        public ListService(KollectorScumDbContext context, ILogger<ListService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<List<ListSummaryDto>>> GetAllListsAsync()
        {
            try
            {
                var lists = await _context.Lists
                    .Include(l => l.ListReleases)
                    .OrderByDescending(l => l.LastModified)
                    .Select(l => new ListSummaryDto
                    {
                        Id = l.Id,
                        Name = l.Name,
                        ReleaseCount = l.ListReleases.Count,
                        CreatedAt = l.CreatedAt,
                        LastModified = l.LastModified
                    })
                    .ToListAsync();

                return Result<List<ListSummaryDto>>.Success(lists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all lists");
                return Result<List<ListSummaryDto>>.Failure("An error occurred while retrieving lists", ErrorType.InternalError);
            }
        }

        public async Task<Result<ListDto>> GetListAsync(int id)
        {
            try
            {
                var list = await _context.Lists
                    .Include(l => l.ListReleases)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (list == null)
                {
                    return Result<ListDto>.Failure($"List with ID {id} not found", ErrorType.NotFound);
                }

                var dto = new ListDto
                {
                    Id = list.Id,
                    Name = list.Name,
                    CreatedAt = list.CreatedAt,
                    LastModified = list.LastModified,
                    ReleaseIds = list.ListReleases.Select(lr => lr.ReleaseId).ToList()
                };

                return Result<ListDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting list {ListId}", id);
                return Result<ListDto>.Failure("An error occurred while retrieving the list", ErrorType.InternalError);
            }
        }

        public async Task<Result<List<int>>> GetListReleasesAsync(int id)
        {
            try
            {
                var list = await _context.Lists
                    .Include(l => l.ListReleases)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (list == null)
                {
                    return Result<List<int>>.Failure($"List with ID {id} not found", ErrorType.NotFound);
                }

                var releaseIds = list.ListReleases.Select(lr => lr.ReleaseId).ToList();
                return Result<List<int>>.Success(releaseIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting releases for list {ListId}", id);
                return Result<List<int>>.Failure("An error occurred while retrieving list releases", ErrorType.InternalError);
            }
        }

        public async Task<Result<ListDto>> CreateListAsync(CreateListDto createDto)
        {
            try
            {
                // Check if a list with the same name already exists
                var existingList = await _context.Lists
                    .FirstOrDefaultAsync(l => l.Name.ToLower() == createDto.Name.ToLower());

                if (existingList != null)
                {
                    return Result<ListDto>.Failure($"A list with the name '{createDto.Name}' already exists", ErrorType.DuplicateError);
                }

                var list = new Models.List
                {
                    Name = createDto.Name,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                _context.Lists.Add(list);
                await _context.SaveChangesAsync();

                var dto = new ListDto
                {
                    Id = list.Id,
                    Name = list.Name,
                    CreatedAt = list.CreatedAt,
                    LastModified = list.LastModified,
                    ReleaseIds = new List<int>()
                };

                return Result<ListDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating list");
                return Result<ListDto>.Failure("An error occurred while creating the list", ErrorType.InternalError);
            }
        }

        public async Task<Result<ListDto>> UpdateListAsync(int id, UpdateListDto updateDto)
        {
            try
            {
                var list = await _context.Lists
                    .Include(l => l.ListReleases)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (list == null)
                {
                    return Result<ListDto>.Failure($"List with ID {id} not found", ErrorType.NotFound);
                }

                // Check if another list with the same name already exists
                var existingList = await _context.Lists
                    .FirstOrDefaultAsync(l => l.Name.ToLower() == updateDto.Name.ToLower() && l.Id != id);

                if (existingList != null)
                {
                    return Result<ListDto>.Failure($"A list with the name '{updateDto.Name}' already exists", ErrorType.DuplicateError);
                }

                list.Name = updateDto.Name;
                list.LastModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var dto = new ListDto
                {
                    Id = list.Id,
                    Name = list.Name,
                    CreatedAt = list.CreatedAt,
                    LastModified = list.LastModified,
                    ReleaseIds = list.ListReleases.Select(lr => lr.ReleaseId).ToList()
                };

                return Result<ListDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating list {ListId}", id);
                return Result<ListDto>.Failure("An error occurred while updating the list", ErrorType.InternalError);
            }
        }

        public async Task<Result<bool>> DeleteListAsync(int id)
        {
            try
            {
                var list = await _context.Lists.FindAsync(id);

                if (list == null)
                {
                    return Result<bool>.Failure($"List with ID {id} not found", ErrorType.NotFound);
                }

                _context.Lists.Remove(list);
                await _context.SaveChangesAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting list {ListId}", id);
                return Result<bool>.Failure("An error occurred while deleting the list", ErrorType.InternalError);
            }
        }

        public async Task<Result<bool>> AddReleaseToListAsync(int listId, int releaseId)
        {
            try
            {
                // Check if the list exists
                var list = await _context.Lists.FindAsync(listId);
                if (list == null)
                {
                    return Result<bool>.Failure($"List with ID {listId} not found", ErrorType.NotFound);
                }

                // Check if the release exists
                var release = await _context.MusicReleases.FindAsync(releaseId);
                if (release == null)
                {
                    return Result<bool>.Failure($"Release with ID {releaseId} not found", ErrorType.NotFound);
                }

                // Check if the release is already in the list
                var existingListRelease = await _context.ListReleases
                    .FirstOrDefaultAsync(lr => lr.ListId == listId && lr.ReleaseId == releaseId);

                if (existingListRelease != null)
                {
                    return Result<bool>.Failure("This release is already in the list", ErrorType.DuplicateError);
                }

                var listRelease = new ListRelease
                {
                    ListId = listId,
                    ReleaseId = releaseId,
                    AddedAt = DateTime.UtcNow
                };

                _context.ListReleases.Add(listRelease);
                
                // Update the list's LastModified timestamp
                list.LastModified = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding release {ReleaseId} to list {ListId}", releaseId, listId);
                return Result<bool>.Failure("An error occurred while adding the release to the list", ErrorType.InternalError);
            }
        }

        public async Task<Result<bool>> RemoveReleaseFromListAsync(int listId, int releaseId)
        {
            try
            {
                var listRelease = await _context.ListReleases
                    .FirstOrDefaultAsync(lr => lr.ListId == listId && lr.ReleaseId == releaseId);

                if (listRelease == null)
                {
                    return Result<bool>.Failure("Release not found in this list", ErrorType.NotFound);
                }

                _context.ListReleases.Remove(listRelease);
                
                // Update the list's LastModified timestamp
                var list = await _context.Lists.FindAsync(listId);
                if (list != null)
                {
                    list.LastModified = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing release {ReleaseId} from list {ListId}", releaseId, listId);
                return Result<bool>.Failure("An error occurred while removing the release from the list", ErrorType.InternalError);
            }
        }

        public async Task<Result<List<ListSummaryDto>>> GetListsForReleaseAsync(int releaseId)
        {
            try
            {
                // Check if the release exists
                var release = await _context.MusicReleases.FindAsync(releaseId);
                if (release == null)
                {
                    return Result<List<ListSummaryDto>>.Failure($"Release with ID {releaseId} not found", ErrorType.NotFound);
                }

                var lists = await _context.Lists
                    .Where(l => l.ListReleases.Any(lr => lr.ReleaseId == releaseId))
                    .Include(l => l.ListReleases)
                    .OrderByDescending(l => l.LastModified)
                    .Select(l => new ListSummaryDto
                    {
                        Id = l.Id,
                        Name = l.Name,
                        ReleaseCount = l.ListReleases.Count,
                        CreatedAt = l.CreatedAt,
                        LastModified = l.LastModified
                    })
                    .ToListAsync();

                return Result<List<ListSummaryDto>>.Success(lists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lists for release {ReleaseId}", releaseId);
                return Result<List<ListSummaryDto>>.Failure("An error occurred while retrieving lists for the release", ErrorType.InternalError);
            }
        }
    }
}
