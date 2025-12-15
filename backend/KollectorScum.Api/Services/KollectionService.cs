using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for managing kollections
    /// </summary>
    public class KollectionService : IKollectionService
    {
        private readonly KollectorScumDbContext _context;
        private readonly ILogger<KollectionService> _logger;
        private readonly IUserContext _userContext;

        public KollectionService(
            KollectorScumDbContext context,
            ILogger<KollectionService> logger,
            IUserContext userContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        public async Task<PagedResult<KollectionDto>> GetAllAsync(int page, int pageSize, string? search = null)
        {
            _logger.LogInformation("Getting kollections - Page: {Page}, PageSize: {PageSize}, Search: {Search}", 
                page, pageSize, search);

            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
            {
                return new PagedResult<KollectionDto>
                {
                    Items = new List<KollectionDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0
                };
            }

            var query = _context.Kollections
                .Where(k => k.UserId == userId.Value)
                .Include(k => k.KollectionGenres)
                .ThenInclude(kg => kg.Genre)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(k => k.Name.ToLower().Contains(search.ToLower()));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(k => k.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(k => MapToDto(k)).ToList();

            return new PagedResult<KollectionDto>
            {
                Items = dtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<KollectionDto?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Getting kollection by ID: {Id}", id);

            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue) return null;

            var kollection = await _context.Kollections
                .Include(k => k.KollectionGenres)
                .ThenInclude(kg => kg.Genre)
                .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId.Value);

            return kollection != null ? MapToDto(kollection) : null;
        }

        public async Task<KollectionDto> CreateAsync(CreateKollectionDto createDto)
        {
            _logger.LogInformation("Creating kollection: {Name}", createDto.Name);

            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue) throw new UnauthorizedAccessException("User must be logged in to create a kollection.");

            // Check if name already exists for this user
            if (await _context.Kollections.AnyAsync(k => k.UserId == userId.Value && k.Name.ToLower() == createDto.Name.ToLower()))
            {
                throw new ArgumentException($"A kollection with the name '{createDto.Name}' already exists.");
            }

            // Verify all genres exist (genres are shared or user-specific? Assuming shared for now or handled by resolver)
            // Ideally we should check if genres belong to user or are global, but for now let's just check existence.
            // If genres are user-specific, we should filter here too.
            // Based on EntityResolverService, genres are user-specific.
            // So we should check if genres belong to the user.
            
            var genres = await _context.Genres
                .Where(g => createDto.GenreIds.Contains(g.Id) && g.UserId == userId.Value)
                .ToListAsync();

            if (genres.Count != createDto.GenreIds.Count)
            {
                throw new ArgumentException("One or more genre IDs are invalid or do not belong to the user.");
            }

            var kollection = new Kollection
            {
                Name = createDto.Name,
                UserId = userId.Value
            };

            _context.Kollections.Add(kollection);
            await _context.SaveChangesAsync();

            // Add genre relationships
            foreach (var genreId in createDto.GenreIds)
            {
                _context.KollectionGenres.Add(new KollectionGenre
                {
                    KollectionId = kollection.Id,
                    GenreId = genreId
                });
            }

            await _context.SaveChangesAsync();

            // Reload with genres
            kollection = await _context.Kollections
                .Include(k => k.KollectionGenres)
                .ThenInclude(kg => kg.Genre)
                .FirstAsync(k => k.Id == kollection.Id);

            return MapToDto(kollection);
        }

        public async Task<KollectionDto?> UpdateAsync(int id, UpdateKollectionDto updateDto)
        {
            _logger.LogInformation("Updating kollection: {Id}", id);

            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue) return null;

            var kollection = await _context.Kollections
                .Include(k => k.KollectionGenres)
                .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId.Value);

            if (kollection == null)
            {
                return null;
            }

            // Check if name already exists (excluding current kollection)
            if (await _context.Kollections.AnyAsync(k => k.UserId == userId.Value && k.Name.ToLower() == updateDto.Name.ToLower() && k.Id != id))
            {
                throw new ArgumentException($"A kollection with the name '{updateDto.Name}' already exists.");
            }

            // Verify all genres exist and belong to user
            var genres = await _context.Genres
                .Where(g => updateDto.GenreIds.Contains(g.Id) && g.UserId == userId.Value)
                .ToListAsync();

            if (genres.Count != updateDto.GenreIds.Count)
            {
                throw new ArgumentException("One or more genre IDs are invalid or do not belong to the user.");
            }

            kollection.Name = updateDto.Name;

            // Remove existing genre relationships
            _context.KollectionGenres.RemoveRange(kollection.KollectionGenres);

            // Add new genre relationships
            foreach (var genreId in updateDto.GenreIds)
            {
                _context.KollectionGenres.Add(new KollectionGenre
                {
                    KollectionId = kollection.Id,
                    GenreId = genreId
                });
            }

            await _context.SaveChangesAsync();

            // Reload with genres
            kollection = await _context.Kollections
                .Include(k => k.KollectionGenres)
                .ThenInclude(kg => kg.Genre)
                .FirstAsync(k => k.Id == kollection.Id);

            return MapToDto(kollection);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting kollection: {Id}", id);

            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue) return false;

            var kollection = await _context.Kollections.FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId.Value);
            if (kollection == null)
            {
                return false;
            }

            _context.Kollections.Remove(kollection);
            await _context.SaveChangesAsync();

            return true;
        }

        private static KollectionDto MapToDto(Kollection kollection)
        {
            return new KollectionDto
            {
                Id = kollection.Id,
                Name = kollection.Name,
                GenreIds = kollection.KollectionGenres.Select(kg => kg.GenreId).OrderBy(id => id).ToList(),
                GenreNames = kollection.KollectionGenres.Select(kg => kg.Genre.Name).OrderBy(name => name).ToList()
            };
        }
    }
}
