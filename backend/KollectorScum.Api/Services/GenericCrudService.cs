using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Generic CRUD service providing standard operations for entities
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TDto">The DTO type</typeparam>
    public abstract class GenericCrudService<TEntity, TDto> : IGenericCrudService<TEntity, TDto> 
        where TEntity : class
    {
        protected readonly IRepository<TEntity> _repository;
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly ILogger _logger;

        protected GenericCrudService(
            IRepository<TEntity> repository,
            IUnitOfWork unitOfWork,
            ILogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all entities with pagination, filtering, and sorting
        /// </summary>
        public virtual async Task<PagedResult<TDto>> GetAllAsync(
            int page = 1,
            int pageSize = 50,
            string? search = null,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
        {
            _logger.LogInformation("Getting all {EntityType} - Page: {Page}, PageSize: {PageSize}, Search: {Search}",
                typeof(TEntity).Name, page, pageSize, search);

            // Combine search filter with custom filter
            Expression<Func<TEntity, bool>>? combinedFilter = filter;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchFilter = BuildSearchFilter(search);
                combinedFilter = combinedFilter == null ? searchFilter : CombineFilters(combinedFilter, searchFilter);
            }

            // Default ordering if none provided
            orderBy ??= BuildDefaultOrdering();

            var pagedResult = await _repository.GetPagedAsync(
                pageNumber: page,
                pageSize: pageSize,
                filter: combinedFilter,
                orderBy: orderBy);

            var dtos = pagedResult.Items.Select(entity => MapToDto(entity)).ToList();

            return new PagedResult<TDto>
            {
                Items = dtos,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount,
                TotalPages = pagedResult.TotalPages
            };
        }

        /// <summary>
        /// Gets a single entity by ID
        /// </summary>
        public virtual async Task<TDto?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Getting {EntityType} by ID: {Id}", typeof(TEntity).Name, id);

            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? default : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new entity
        /// </summary>
        public virtual async Task<TDto> CreateAsync(TDto dto)
        {
            _logger.LogInformation("Creating new {EntityType}", typeof(TEntity).Name);

            ValidateDto(dto);

            var entity = MapToEntity(dto);
            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created {EntityType} with ID: {Id}", typeof(TEntity).Name, GetEntityId(entity));

            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        public virtual async Task<TDto?> UpdateAsync(int id, TDto dto)
        {
            _logger.LogInformation("Updating {EntityType} with ID: {Id}", typeof(TEntity).Name, id);

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                _logger.LogWarning("{EntityType} not found: {Id}", typeof(TEntity).Name, id);
                return default;
            }

            ValidateDto(dto);

            UpdateEntity(existingEntity, dto);
            _repository.Update(existingEntity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated {EntityType} with ID: {Id}", typeof(TEntity).Name, id);

            return MapToDto(existingEntity);
        }

        /// <summary>
        /// Deletes an entity by ID
        /// </summary>
        public virtual async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting {EntityType} with ID: {Id}", typeof(TEntity).Name, id);

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("{EntityType} not found: {Id}", typeof(TEntity).Name, id);
                return false;
            }

            _repository.Delete(entity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted {EntityType} with ID: {Id}", typeof(TEntity).Name, id);

            return true;
        }

        /// <summary>
        /// Maps entity to DTO - must be implemented by derived classes
        /// </summary>
        protected abstract TDto MapToDto(TEntity entity);

        /// <summary>
        /// Maps DTO to entity for creation - must be implemented by derived classes
        /// </summary>
        protected abstract TEntity MapToEntity(TDto dto);

        /// <summary>
        /// Updates entity with DTO values - must be implemented by derived classes
        /// </summary>
        protected abstract void UpdateEntity(TEntity entity, TDto dto);

        /// <summary>
        /// Gets the ID of an entity - must be implemented by derived classes
        /// </summary>
        protected abstract int GetEntityId(TEntity entity);

        /// <summary>
        /// Builds a search filter expression - can be overridden by derived classes
        /// </summary>
        protected virtual Expression<Func<TEntity, bool>>? BuildSearchFilter(string search)
        {
            // Default: no search filter. Override in derived classes to implement search
            return null;
        }

        /// <summary>
        /// Builds default ordering - can be overridden by derived classes
        /// </summary>
        protected virtual Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? BuildDefaultOrdering()
        {
            // Default: no specific ordering. Override in derived classes
            return null;
        }

        /// <summary>
        /// Validates DTO before create/update - can be overridden by derived classes
        /// </summary>
        protected virtual void ValidateDto(TDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }
            // Additional validation can be added in derived classes
        }

        /// <summary>
        /// Combines two filter expressions with AND logic
        /// </summary>
        private Expression<Func<TEntity, bool>> CombineFilters(
            Expression<Func<TEntity, bool>> filter1,
            Expression<Func<TEntity, bool>> filter2)
        {
            var parameter = Expression.Parameter(typeof(TEntity));

            var combined = Expression.AndAlso(
                Expression.Invoke(filter1, parameter),
                Expression.Invoke(filter2, parameter)
            );

            return Expression.Lambda<Func<TEntity, bool>>(combined, parameter);
        }
    }
}
