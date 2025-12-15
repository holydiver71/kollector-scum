using KollectorScum.Api.DTOs;
using System.Linq.Expressions;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Generic interface for CRUD operations on entities
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TDto">The DTO type for the entity</typeparam>
    public interface IGenericCrudService<TEntity, TDto> where TEntity : class
    {
        /// <summary>
        /// Gets all entities with optional pagination, filtering, and sorting
        /// </summary>
        Task<PagedResult<TDto>> GetAllAsync(
            int page = 1,
            int pageSize = 50,
            string? search = null,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null);

        /// <summary>
        /// Gets a single entity by ID
        /// </summary>
        Task<TDto?> GetByIdAsync(int id);

        /// <summary>
        /// Creates a new entity
        /// </summary>
        Task<TDto> CreateAsync(TDto dto);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        Task<TDto?> UpdateAsync(int id, TDto dto);

        /// <summary>
        /// Deletes an entity by ID
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Gets or creates an entity by name (for lookup tables)
        /// </summary>
        Task<TDto> GetOrCreateByNameAsync(string name);
    }
}
