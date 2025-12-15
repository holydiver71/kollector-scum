using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using System.Linq.Expressions;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for Store CRUD operations
    /// </summary>
    public class StoreService : GenericCrudService<Store, StoreDto>
    {
        public StoreService(
            IRepository<Store> repository,
            IUnitOfWork unitOfWork,
            ILogger<StoreService> logger,
            IUserContext userContext)
            : base(repository, unitOfWork, logger, userContext)
        {
        }

        protected override StoreDto MapToDto(Store entity)
        {
            return new StoreDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        protected override Store MapToEntity(StoreDto dto)
        {
            return new Store
            {
                Name = dto.Name
            };
        }

        protected override void UpdateEntity(Store entity, StoreDto dto)
        {
            entity.Name = dto.Name;
        }

        protected override int GetEntityId(Store entity)
        {
            return entity.Id;
        }

        protected override Expression<Func<Store, bool>>? BuildSearchFilter(string search)
        {
            var searchLower = search.ToLower();
            return s => s.Name.ToLower().Contains(searchLower);
        }

        protected override Func<IQueryable<Store>, IOrderedQueryable<Store>>? BuildDefaultOrdering()
        {
            return query => query.OrderBy(s => s.Name);
        }

        protected override void ValidateDto(StoreDto dto)
        {
            base.ValidateDto(dto);

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Store name is required", nameof(dto.Name));
            }

            if (dto.Name.Length > 200)
            {
                throw new ArgumentException("Store name cannot exceed 200 characters", nameof(dto.Name));
            }
        }
    }
}
