using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    public interface ILabelRepository : IRepository<Label>
    {
        Task<IEnumerable<Label>> GetPaginatedAsync(int page, int pageSize, string? search = null);
        Task<int> GetTotalCountAsync(string? search = null);
        Task<IEnumerable<Label>> SearchAsync(string searchTerm);
    }
}
