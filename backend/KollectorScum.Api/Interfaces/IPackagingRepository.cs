using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    public interface IPackagingRepository : IRepository<Packaging>
    {
        Task<IEnumerable<Packaging>> GetPaginatedAsync(int page, int pageSize, string? search = null);
        Task<int> GetTotalCountAsync(string? search = null);
        Task<IEnumerable<Packaging>> SearchAsync(string searchTerm);
    }
}
