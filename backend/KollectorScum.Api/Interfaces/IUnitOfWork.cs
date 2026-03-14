using KollectorScum.Api.Models;
using System.Threading;

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
        /// Saves all changes to the database
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of state entries written to the database</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        /// <returns>Database transaction</returns>
        Task BeginTransactionAsync();

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task BeginTransactionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task CommitTransactionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        Task RollbackTransactionAsync();

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RollbackTransactionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets a generic repository for any entity type
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <returns>Repository for the entity type</returns>
        IRepository<T> GetRepository<T>() where T : class;
    }
}
