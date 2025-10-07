using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Unit of Work pattern interface for managing database transactions
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Repository for Country entities
        /// </summary>
        IRepository<Country> Countries { get; }

        /// <summary>
        /// Repository for Store entities
        /// </summary>
        IRepository<Store> Stores { get; }

        /// <summary>
        /// Repository for Format entities
        /// </summary>
        IRepository<Format> Formats { get; }

        /// <summary>
        /// Repository for Genre entities
        /// </summary>
        IRepository<Genre> Genres { get; }

        /// <summary>
        /// Repository for Label entities
        /// </summary>
        IRepository<Label> Labels { get; }

        /// <summary>
        /// Repository for Artist entities
        /// </summary>
        IRepository<Artist> Artists { get; }

        /// <summary>
        /// Repository for Packaging entities
        /// </summary>
        IRepository<Packaging> Packagings { get; }

        /// <summary>
        /// Repository for MusicRelease entities
        /// </summary>
        IRepository<MusicRelease> MusicReleases { get; }

        /// <summary>
        /// Saves all changes to the database
        /// </summary>
        /// <returns>Number of state entries written to the database</returns>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        /// <returns>Database transaction</returns>
        Task BeginTransactionAsync();

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        Task RollbackTransactionAsync();

        /// <summary>
        /// Gets a generic repository for any entity type
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <returns>Repository for the entity type</returns>
        IRepository<T> GetRepository<T>() where T : class;
    }
}
