using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using System.Linq.Expressions;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for Label CRUD operations
    /// </summary>
    public class LabelService : GenericCrudService<Label, LabelDto>
    {
        public LabelService(
            IRepository<Label> repository,
            IUnitOfWork unitOfWork,
            ILogger<LabelService> logger)
            : base(repository, unitOfWork, logger)
        {
        }

        protected override LabelDto MapToDto(Label entity)
        {
            return new LabelDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        protected override Label MapToEntity(LabelDto dto)
        {
            return new Label
            {
                Name = dto.Name
            };
        }

        protected override void UpdateEntity(Label entity, LabelDto dto)
        {
            entity.Name = dto.Name;
        }

        protected override int GetEntityId(Label entity)
        {
            return entity.Id;
        }

        protected override Expression<Func<Label, bool>>? BuildSearchFilter(string search)
        {
            var searchLower = search.ToLower();
            return l => l.Name.ToLower().Contains(searchLower);
        }

        protected override Func<IQueryable<Label>, IOrderedQueryable<Label>>? BuildDefaultOrdering()
        {
            return query => query.OrderBy(l => l.Name);
        }

        protected override void ValidateDto(LabelDto dto)
        {
            base.ValidateDto(dto);

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Label name is required", nameof(dto.Name));
            }

            if (dto.Name.Length > 200)
            {
                throw new ArgumentException("Label name cannot exceed 200 characters", nameof(dto.Name));
            }
        }
    }
}
