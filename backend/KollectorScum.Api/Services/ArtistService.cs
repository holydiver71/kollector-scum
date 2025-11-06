using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using System.Linq.Expressions;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for Artist CRUD operations
    /// </summary>
    public class ArtistService : GenericCrudService<Artist, ArtistDto>
    {
        public ArtistService(
            IRepository<Artist> repository,
            IUnitOfWork unitOfWork,
            ILogger<ArtistService> logger)
            : base(repository, unitOfWork, logger)
        {
        }

        protected override ArtistDto MapToDto(Artist entity)
        {
            return new ArtistDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        protected override Artist MapToEntity(ArtistDto dto)
        {
            return new Artist
            {
                Name = dto.Name
            };
        }

        protected override void UpdateEntity(Artist entity, ArtistDto dto)
        {
            entity.Name = dto.Name;
        }

        protected override int GetEntityId(Artist entity)
        {
            return entity.Id;
        }

        protected override Expression<Func<Artist, bool>>? BuildSearchFilter(string search)
        {
            var searchLower = search.ToLower();
            return a => a.Name.ToLower().Contains(searchLower);
        }

        protected override Func<IQueryable<Artist>, IOrderedQueryable<Artist>>? BuildDefaultOrdering()
        {
            return query => query.OrderBy(a => a.Name);
        }

        protected override void ValidateDto(ArtistDto dto)
        {
            base.ValidateDto(dto);

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Artist name is required", nameof(dto.Name));
            }

            if (dto.Name.Length > 200)
            {
                throw new ArgumentException("Artist name cannot exceed 200 characters", nameof(dto.Name));
            }
        }
    }
}
