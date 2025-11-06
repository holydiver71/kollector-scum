using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using System.Linq.Expressions;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for Genre CRUD operations
    /// </summary>
    public class GenreService : GenericCrudService<Genre, GenreDto>
    {
        public GenreService(
            IRepository<Genre> repository,
            IUnitOfWork unitOfWork,
            ILogger<GenreService> logger)
            : base(repository, unitOfWork, logger)
        {
        }

        protected override GenreDto MapToDto(Genre entity)
        {
            return new GenreDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        protected override Genre MapToEntity(GenreDto dto)
        {
            return new Genre
            {
                Name = dto.Name
            };
        }

        protected override void UpdateEntity(Genre entity, GenreDto dto)
        {
            entity.Name = dto.Name;
        }

        protected override int GetEntityId(Genre entity)
        {
            return entity.Id;
        }

        protected override Expression<Func<Genre, bool>>? BuildSearchFilter(string search)
        {
            var searchLower = search.ToLower();
            return g => g.Name.ToLower().Contains(searchLower);
        }

        protected override Func<IQueryable<Genre>, IOrderedQueryable<Genre>>? BuildDefaultOrdering()
        {
            return query => query.OrderBy(g => g.Name);
        }

        protected override void ValidateDto(GenreDto dto)
        {
            base.ValidateDto(dto);

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Genre name is required", nameof(dto.Name));
            }

            if (dto.Name.Length > 100)
            {
                throw new ArgumentException("Genre name cannot exceed 100 characters", nameof(dto.Name));
            }
        }
    }
}
