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

        public KollectionService(
            KollectorScumDbContext context,
            ILogger<KollectionService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<KollectionDto>> GetAllAsync(int page, int pageSize, string? search = null)
        {
            _logger.LogInformation("Getting kollections - Page: {Page}, PageSize: {PageSize}, Search: {Search}", 
                page, pageSize, search);

            var query = _context.Kollections
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

            var kollection = await _context.Kollections
                .Include(k => k.KollectionGenres)
                .ThenInclude(kg => kg.Genre)
                .FirstOrDefaultAsync(k => k.Id == id);

            return kollection != null ? MapToDto(kollection) : null;
        }

        public async Task<KollectionDto> CreateAsync(CreateKollectionDto createDto)
        {
            _logger.LogInformation("Creating kollection: {Name}", createDto.Name);

            // Check if name already exists
            if (await _context.Kollections.AnyAsync(k => k.Name.ToLower() == createDto.Name.ToLower()))
            {
                throw new ArgumentException($"A kollection with the name '{createDto.Name}' already exists.");
            }

            // Verify all genres exist
            var genres = await _context.Genres
                .Where(g => createDto.GenreIds.Contains(g.Id))
                .ToListAsync();

            if (genres.Count != createDto.GenreIds.Count)
            {
                throw new ArgumentException("One or more genre IDs are invalid.");
            }

            var kollection = new Kollection
            {
                Name = createDto.Name
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

            var kollection = await _context.Kollections
                .Include(k => k.KollectionGenres)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kollection == null)
            {
                return null;
            }

            // Check if name already exists (excluding current kollection)
            if (await _context.Kollections.AnyAsync(k => k.Name.ToLower() == updateDto.Name.ToLower() && k.Id != id))
            {
                throw new ArgumentException($"A kollection with the name '{updateDto.Name}' already exists.");
            }

            // Verify all genres exist
            var genres = await _context.Genres
                .Where(g => updateDto.GenreIds.Contains(g.Id))
                .ToListAsync();

            if (genres.Count != updateDto.GenreIds.Count)
            {
                throw new ArgumentException("One or more genre IDs are invalid.");
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

            var kollection = await _context.Kollections.FindAsync(id);
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
