using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    public interface IMusicReleaseRepository : IRepository<MusicRelease>
    {
        Task<IEnumerable<MusicRelease>> GetPaginatedAsync(int page, int pageSize, string? search = null, 
            int? artistId = null, int? genreId = null, int? formatId = null, int? countryId = null, int? labelId = null);
        Task<int> GetTotalCountAsync(string? search = null, int? artistId = null, int? genreId = null, 
            int? formatId = null, int? countryId = null, int? labelId = null);
        Task<MusicRelease?> GetWithDetailsAsync(int id);
        Task<IEnumerable<MusicRelease>> SearchAsync(string searchTerm);
    }
}
