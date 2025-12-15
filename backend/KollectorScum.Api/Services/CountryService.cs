using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using System.Linq.Expressions;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for Country CRUD operations
    /// </summary>
    public class CountryService : GenericCrudService<Country, CountryDto>
    {
        public CountryService(
            IRepository<Country> repository,
            IUnitOfWork unitOfWork,
            ILogger<CountryService> logger,
            IUserContext userContext)
            : base(repository, unitOfWork, logger, userContext)
        {
        }

        protected override CountryDto MapToDto(Country entity)
        {
            return new CountryDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        protected override Country MapToEntity(CountryDto dto)
        {
            return new Country
            {
                Name = dto.Name
            };
        }

        protected override void UpdateEntity(Country entity, CountryDto dto)
        {
            entity.Name = dto.Name;
        }

        protected override int GetEntityId(Country entity)
        {
            return entity.Id;
        }

        protected override Expression<Func<Country, bool>>? BuildSearchFilter(string search)
        {
            var searchLower = search.ToLower();
            return c => c.Name.ToLower().Contains(searchLower);
        }

        protected override Func<IQueryable<Country>, IOrderedQueryable<Country>>? BuildDefaultOrdering()
        {
            return query => query.OrderBy(c => c.Name);
        }

        protected override void ValidateDto(CountryDto dto)
        {
            base.ValidateDto(dto);

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Country name is required", nameof(dto.Name));
            }

            if (dto.Name.Length > 100)
            {
                throw new ArgumentException("Country name cannot exceed 100 characters", nameof(dto.Name));
            }
        }
    }
}
