using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
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
        protected readonly IUserContext _userContext;

        protected GenericCrudService(
            IRepository<TEntity> repository,
            IUnitOfWork unitOfWork,
            ILogger logger,
            IUserContext userContext)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
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
            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("User must be authenticated");
            }

            _logger.LogInformation("Getting all {EntityType} for user {UserId} - Page: {Page}, PageSize: {PageSize}, Search: {Search}",
                typeof(TEntity).Name, userId, page, pageSize, search);

            // Add userId filter for user-owned entities
            Expression<Func<TEntity, bool>>? combinedFilter = filter;
            if (typeof(Models.IUserOwnedEntity).IsAssignableFrom(typeof(TEntity)))
            {
                var userIdValue = userId.Value;
                Expression<Func<TEntity, bool>> userFilter = e => ((Models.IUserOwnedEntity)e).UserId == userIdValue;
                combinedFilter = combinedFilter == null ? userFilter : CombineFilters(combinedFilter, userFilter);
            }

            // Combine search filter with custom filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchFilter = BuildSearchFilter(search);
                if (searchFilter != null)
                {
                    combinedFilter = combinedFilter == null ? searchFilter : CombineFilters(combinedFilter, searchFilter);
                }
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
            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("User must be authenticated");
            }

            _logger.LogInformation("Getting {EntityType} by ID: {Id} for user {UserId}", typeof(TEntity).Name, id, userId);

            var entity = await _repository.GetByIdAsync(id);
            
            // Verify user owns the entity
            if (entity != null && typeof(Models.IUserOwnedEntity).IsAssignableFrom(typeof(TEntity)))
            {
                var userOwnedEntity = entity as Models.IUserOwnedEntity;
                if (userOwnedEntity?.UserId != userId.Value)
                {
                    _logger.LogWarning("User {UserId} attempted to access {EntityType} {Id} owned by {OwnerId}",
                        userId, typeof(TEntity).Name, id, userOwnedEntity?.UserId);
                    return default;
                }
            }
            
            return entity == null ? default : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new entity
        /// </summary>
        public virtual async Task<TDto> CreateAsync(TDto dto)
        {
            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("User must be authenticated");
            }

            _logger.LogInformation("Creating new {EntityType} for user {UserId}", typeof(TEntity).Name, userId);

            ValidateDto(dto);

            var entity = MapToEntity(dto);
            
            // Set UserId for user-owned entities
            if (entity is Models.IUserOwnedEntity userOwnedEntity)
            {
                userOwnedEntity.UserId = userId.Value;
            }
            
            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created {EntityType} with ID: {Id} for user {UserId}", 
                typeof(TEntity).Name, GetEntityId(entity), userId);

            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        public virtual async Task<TDto?> UpdateAsync(int id, TDto dto)
        {
            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("User must be authenticated");
            }

            _logger.LogInformation("Updating {EntityType} with ID: {Id} for user {UserId}", typeof(TEntity).Name, id, userId);

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                _logger.LogWarning("{EntityType} not found: {Id}", typeof(TEntity).Name, id);
                return default;
            }

            // Verify user owns the entity
            if (existingEntity is Models.IUserOwnedEntity userOwnedEntity)
            {
                if (userOwnedEntity.UserId != userId.Value)
                {
                    _logger.LogWarning("User {UserId} attempted to update {EntityType} {Id} owned by {OwnerId}",
                        userId, typeof(TEntity).Name, id, userOwnedEntity.UserId);
                    throw new UnauthorizedAccessException("Cannot update entity owned by another user");
                }
            }

            ValidateDto(dto);

            UpdateEntity(existingEntity, dto);
            _repository.Update(existingEntity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated {EntityType} with ID: {Id} for user {UserId}", typeof(TEntity).Name, id, userId);

            return MapToDto(existingEntity);
        }

        /// <summary>
        /// Deletes an entity by ID
        /// </summary>
        public virtual async Task<bool> DeleteAsync(int id)
        {
            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("User must be authenticated");
            }

            _logger.LogInformation("Deleting {EntityType} with ID: {Id} for user {UserId}", typeof(TEntity).Name, id, userId);

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("{EntityType} not found: {Id}", typeof(TEntity).Name, id);
                return false;
            }

            // Verify user owns the entity
            if (entity is Models.IUserOwnedEntity userOwnedEntity)
            {
                if (userOwnedEntity.UserId != userId.Value)
                {
                    _logger.LogWarning("User {UserId} attempted to delete {EntityType} {Id} owned by {OwnerId}",
                        userId, typeof(TEntity).Name, id, userOwnedEntity.UserId);
                    throw new UnauthorizedAccessException("Cannot delete entity owned by another user");
                }
            }

            _repository.Delete(entity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted {EntityType} with ID: {Id} for user {UserId}", typeof(TEntity).Name, id, userId);

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
        /// Gets or creates an entity by name (for lookup tables)
        /// </summary>
        public virtual async Task<TDto> GetOrCreateByNameAsync(string name)
        {
            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("User must be authenticated");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be empty", nameof(name));
            }

            _logger.LogInformation("Getting or creating {EntityType} with name '{Name}' for user {UserId}",
                typeof(TEntity).Name, name, userId);

            // Try to find existing entity by name and userId
            var userIdValue = userId.Value;
            // Note: Using EF.Property and reflection for generic lookup by name
            // This is intentional for the generic pattern - specific services can override if needed
            Expression<Func<TEntity, bool>> filter = e =>
                ((Models.IUserOwnedEntity)e).UserId == userIdValue &&
                EF.Property<string>(e, "Name") == name;

            var existing = await _repository.GetAsync(filter);
            var existingEntity = existing.FirstOrDefault();

            if (existingEntity != null)
            {
                _logger.LogInformation("Found existing {EntityType} with name '{Name}'", typeof(TEntity).Name, name);
                return MapToDto(existingEntity);
            }

            // Create new entity
            _logger.LogInformation("Creating new {EntityType} with name '{Name}' for user {UserId}",
                typeof(TEntity).Name, name, userId);

            // Note: Using Activator and reflection for generic entity creation
            // Performance impact is acceptable for lookup entities which are created infrequently
            // Specific services can override this method if different behavior is needed
            var entity = Activator.CreateInstance<TEntity>();
            
            // Set the Name property using reflection
            var nameProperty = typeof(TEntity).GetProperty("Name");
            if (nameProperty != null && nameProperty.CanWrite)
            {
                nameProperty.SetValue(entity, name);
            }
            
            // Set UserId
            if (entity is Models.IUserOwnedEntity userOwnedEntity)
            {
                userOwnedEntity.UserId = userId.Value;
            }

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created {EntityType} with name '{Name}' and ID: {Id} for user {UserId}",
                typeof(TEntity).Name, name, GetEntityId(entity), userId);

            return MapToDto(entity);
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
