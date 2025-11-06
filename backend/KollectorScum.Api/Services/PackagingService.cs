using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using System.Linq.Expressions;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for Packaging CRUD operations
    /// </summary>
    public class PackagingService : GenericCrudService<Packaging, PackagingDto>
    {
        public PackagingService(
            IRepository<Packaging> repository,
            IUnitOfWork unitOfWork,
            ILogger<PackagingService> logger)
            : base(repository, unitOfWork, logger)
        {
        }

        protected override PackagingDto MapToDto(Packaging entity)
        {
            return new PackagingDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        protected override Packaging MapToEntity(PackagingDto dto)
        {
            return new Packaging
            {
                Name = dto.Name
            };
        }

        protected override void UpdateEntity(Packaging entity, PackagingDto dto)
        {
            entity.Name = dto.Name;
        }

        protected override int GetEntityId(Packaging entity)
        {
            return entity.Id;
        }

        protected override Expression<Func<Packaging, bool>>? BuildSearchFilter(string search)
        {
            var searchLower = search.ToLower();
            return p => p.Name.ToLower().Contains(searchLower);
        }

        protected override Func<IQueryable<Packaging>, IOrderedQueryable<Packaging>>? BuildDefaultOrdering()
        {
            return query => query.OrderBy(p => p.Name);
        }

        protected override void ValidateDto(PackagingDto dto)
        {
            base.ValidateDto(dto);

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Packaging name is required", nameof(dto.Name));
            }

            if (dto.Name.Length > 50)
            {
                throw new ArgumentException("Packaging name cannot exceed 50 characters", nameof(dto.Name));
            }
        }
    }
}
