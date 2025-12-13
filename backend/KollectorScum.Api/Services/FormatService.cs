using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using System.Linq.Expressions;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for Format CRUD operations
    /// </summary>
    public class FormatService : GenericCrudService<Format, FormatDto>
    {
        public FormatService(
            IRepository<Format> repository,
            IUnitOfWork unitOfWork,
            ILogger<FormatService> logger,
            IUserContext userContext)
            : base(repository, unitOfWork, logger, userContext)
        {
        }

        protected override FormatDto MapToDto(Format entity)
        {
            return new FormatDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        protected override Format MapToEntity(FormatDto dto)
        {
            return new Format
            {
                Name = dto.Name
            };
        }

        protected override void UpdateEntity(Format entity, FormatDto dto)
        {
            entity.Name = dto.Name;
        }

        protected override int GetEntityId(Format entity)
        {
            return entity.Id;
        }

        protected override Expression<Func<Format, bool>>? BuildSearchFilter(string search)
        {
            var searchLower = search.ToLower();
            return f => f.Name.ToLower().Contains(searchLower);
        }

        protected override Func<IQueryable<Format>, IOrderedQueryable<Format>>? BuildDefaultOrdering()
        {
            return query => query.OrderBy(f => f.Name);
        }

        protected override void ValidateDto(FormatDto dto)
        {
            base.ValidateDto(dto);

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Format name is required", nameof(dto.Name));
            }

            if (dto.Name.Length > 50)
            {
                throw new ArgumentException("Format name cannot exceed 50 characters", nameof(dto.Name));
            }
        }
    }
}
