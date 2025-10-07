using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    public interface IGenreRepository : IRepository<Genre>
    {
        Task<IEnumerable<Genre>> GetPaginatedAsync(int page, int pageSize, string? search = null);
        Task<int> GetTotalCountAsync(string? search = null);
        Task<IEnumerable<Genre>> SearchAsync(string searchTerm);
    }
}
