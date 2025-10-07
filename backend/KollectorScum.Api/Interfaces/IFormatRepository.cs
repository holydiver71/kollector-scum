using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    public interface IFormatRepository : IRepository<Format>
    {
        Task<IEnumerable<Format>> GetPaginatedAsync(int page, int pageSize, string? search = null);
        Task<int> GetTotalCountAsync(string? search = null);
        Task<IEnumerable<Format>> SearchAsync(string searchTerm);
    }
}
