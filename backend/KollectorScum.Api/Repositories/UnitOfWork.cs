using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace KollectorScum.Api.Repositories
{
    /// <summary>
    /// Unit of Work implementation for managing database transactions and repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly KollectorScumDbContext _context;
        private IDbContextTransaction? _currentTransaction;
        private readonly Dictionary<Type, object> _repositories = new();

        // Lazy-loaded repositories
        private IRepository<Country>? _countries;
        private IRepository<Store>? _stores;
        private IRepository<Format>? _formats;
        private IRepository<Genre>? _genres;
        private IRepository<Label>? _labels;
        private IRepository<Artist>? _artists;
        private IRepository<Packaging>? _packagings;
        private IRepository<MusicRelease>? _musicReleases;

        /// <summary>
        /// Initializes a new instance of the UnitOfWork class
        /// </summary>
        /// <param name="context">Database context</param>
        public UnitOfWork(KollectorScumDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Repository for Country entities
        /// </summary>
        public IRepository<Country> Countries
        {
            get { return _countries ??= new Repository<Country>(_context); }
        }

        /// <summary>
        /// Repository for Store entities
        /// </summary>
        public IRepository<Store> Stores
        {
            get { return _stores ??= new Repository<Store>(_context); }
        }

        /// <summary>
        /// Repository for Format entities
        /// </summary>
        public IRepository<Format> Formats
        {
            get { return _formats ??= new Repository<Format>(_context); }
        }

        /// <summary>
        /// Repository for Genre entities
        /// </summary>
        public IRepository<Genre> Genres
        {
            get { return _genres ??= new Repository<Genre>(_context); }
        }

        /// <summary>
        /// Repository for Label entities
        /// </summary>
        public IRepository<Label> Labels
        {
            get { return _labels ??= new Repository<Label>(_context); }
        }

        /// <summary>
        /// Repository for Artist entities
        /// </summary>
        public IRepository<Artist> Artists
        {
            get { return _artists ??= new Repository<Artist>(_context); }
        }

        /// <summary>
        /// Repository for Packaging entities
        /// </summary>
        public IRepository<Packaging> Packagings
        {
            get { return _packagings ??= new Repository<Packaging>(_context); }
        }

        /// <summary>
        /// Repository for MusicRelease entities
        /// </summary>
        public IRepository<MusicRelease> MusicReleases
        {
            get { return _musicReleases ??= new Repository<MusicRelease>(_context); }
        }

        /// <summary>
        /// Gets a generic repository for any entity type
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <returns>Repository for the entity type</returns>
        public IRepository<T> GetRepository<T>() where T : class
        {
            var type = typeof(T);
            
            if (!_repositories.ContainsKey(type))
            {
                _repositories[type] = new Repository<T>(_context);
            }

            return (IRepository<T>)_repositories[type];
        }

        /// <summary>
        /// Saves all changes to the database
        /// </summary>
        /// <returns>Number of state entries written to the database</returns>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }

            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No transaction in progress.");
            }

            try
            {
                await SaveChangesAsync();
                await _currentTransaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No transaction in progress.");
            }

            try
            {
                await _currentTransaction.RollbackAsync();
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        /// <summary>
        /// Disposes the unit of work and releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        /// <param name="disposing">Whether disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _currentTransaction?.Dispose();
                _context?.Dispose();
            }
        }
    }
}
