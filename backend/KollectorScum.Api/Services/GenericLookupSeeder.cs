using KollectorScum.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Generic base class for seeding lookup table data from JSON files
    /// Eliminates repetitive seeding code patterns
    /// </summary>
    /// <typeparam name="TEntity">The entity type to seed</typeparam>
    /// <typeparam name="TDto">The DTO type for JSON deserialization</typeparam>
    /// <typeparam name="TContainer">The container type that wraps the DTO array in JSON</typeparam>
    public abstract class GenericLookupSeeder<TEntity, TDto, TContainer> : ILookupSeeder<TEntity, TDto>
        where TEntity : class
        where TDto : class
        where TContainer : class
    {
        private readonly IJsonFileReader _fileReader;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;
        private readonly string _dataPath;

        protected GenericLookupSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger logger,
            IConfiguration configuration)
        {
            _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            var config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dataPath = config["DataPath"] ?? throw new InvalidOperationException("DataPath configuration is missing");
        }

        // Constructor for testing with explicit data path
        protected GenericLookupSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger logger,
            string dataPath)
        {
            _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataPath = dataPath ?? throw new ArgumentNullException(nameof(dataPath));
        }

        public abstract string TableName { get; }
        public abstract string FileName { get; }

        /// <summary>
        /// Extract the DTO array from the container object
        /// </summary>
        protected abstract List<TDto>? ExtractDtosFromContainer(TContainer container);

        /// <summary>
        /// Map a single DTO to an entity
        /// </summary>
        protected abstract TEntity MapDtoToEntity(TDto dto);

        /// <summary>
        /// Get the repository for the entity type
        /// </summary>
        protected abstract IRepository<TEntity> GetRepository();

        public async Task<int> SeedAsync()
        {
            var filePath = Path.Combine(_dataPath, FileName);

            if (!_fileReader.FileExists(filePath))
            {
                _logger.LogWarning("{TableName} JSON file not found at: {FilePath}", TableName, filePath);
                return 0;
            }

            // Check if data already exists
            var repository = GetRepository();
            if (await repository.AnyAsync(_ => true))
            {
                _logger.LogInformation("{TableName} data already exists, skipping seeding", TableName);
                return 0;
            }

            _logger.LogInformation("Seeding {TableName} from: {FilePath}", TableName, filePath);

            try
            {
                var container = await _fileReader.ReadJsonFileAsync<TContainer>(filePath);

                if (container == null)
                {
                    _logger.LogWarning("Failed to deserialize {TableName} from {FilePath}", TableName, filePath);
                    return 0;
                }

                var dtos = ExtractDtosFromContainer(container);

                if (dtos == null || dtos.Count == 0)
                {
                    _logger.LogWarning("No {TableName} data found in {FilePath}", TableName, filePath);
                    return 0;
                }

                var entities = dtos.Select(MapDtoToEntity).ToList();

                await repository.AddRangeAsync(entities);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} {TableName}", entities.Count, TableName);
                return entities.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding {TableName} from {FilePath}", TableName, filePath);
                throw;
            }
        }
    }
}
