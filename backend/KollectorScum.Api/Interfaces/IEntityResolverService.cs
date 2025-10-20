using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for resolving or creating lookup entities by ID or name
    /// </summary>
    public interface IEntityResolverService
    {
        /// <summary>
        /// Resolves artist IDs or creates new artists from names
        /// </summary>
        Task<List<int>?> ResolveOrCreateArtistsAsync(
            List<int>? artistIds, 
            List<string>? artistNames, 
            CreatedEntitiesDto createdEntities);

        /// <summary>
        /// Resolves genre IDs or creates new genres from names
        /// </summary>
        Task<List<int>?> ResolveOrCreateGenresAsync(
            List<int>? genreIds, 
            List<string>? genreNames, 
            CreatedEntitiesDto createdEntities);

        /// <summary>
        /// Resolves label ID or creates new label from name
        /// </summary>
        Task<int?> ResolveOrCreateLabelAsync(
            int? labelId, 
            string? labelName, 
            CreatedEntitiesDto createdEntities);

        /// <summary>
        /// Resolves country ID or creates new country from name
        /// </summary>
        Task<int?> ResolveOrCreateCountryAsync(
            int? countryId, 
            string? countryName, 
            CreatedEntitiesDto createdEntities);

        /// <summary>
        /// Resolves format ID or creates new format from name
        /// </summary>
        Task<int?> ResolveOrCreateFormatAsync(
            int? formatId, 
            string? formatName, 
            CreatedEntitiesDto createdEntities);

        /// <summary>
        /// Resolves packaging ID or creates new packaging from name
        /// </summary>
        Task<int?> ResolveOrCreatePackagingAsync(
            int? packagingId, 
            string? packagingName, 
            CreatedEntitiesDto createdEntities);
    }
}
