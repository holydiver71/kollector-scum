namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for building complex queries in a composable way
    /// Implements the Builder pattern for IQueryable construction
    /// </summary>
    /// <typeparam name="T">The entity type being queried</typeparam>
    public interface IQueryBuilder<T> where T : class
    {
        /// <summary>
        /// Apply search logic to the query
        /// </summary>
        /// <param name="searchTerm">Search term to apply</param>
        /// <returns>The builder for method chaining</returns>
        IQueryBuilder<T> ApplySearch(string? searchTerm);

        /// <summary>
        /// Apply filter logic to the query
        /// </summary>
        /// <param name="filterAction">Action that applies filters to the query</param>
        /// <returns>The builder for method chaining</returns>
        IQueryBuilder<T> ApplyFilters(Action<IQueryable<T>>? filterAction);

        /// <summary>
        /// Apply pagination to the query
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>The builder for method chaining</returns>
        IQueryBuilder<T> ApplyPagination(int pageNumber, int pageSize);

        /// <summary>
        /// Apply sorting logic to the query
        /// </summary>
        /// <param name="sortExpression">Expression defining sort order</param>
        /// <returns>The builder for method chaining</returns>
        IQueryBuilder<T> ApplySorting(Func<IQueryable<T>, IOrderedQueryable<T>>? sortExpression);

        /// <summary>
        /// Build and return the final queryable
        /// </summary>
        /// <returns>The constructed IQueryable</returns>
        IQueryable<T> Build();
    }
}
