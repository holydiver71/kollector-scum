using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for Kollection service operations
    /// </summary>
    public interface IKollectionService
    {
        Task<PagedResult<KollectionDto>> GetAllAsync(int page, int pageSize, string? search = null);
        Task<KollectionDto?> GetByIdAsync(int id);
        Task<KollectionDto> CreateAsync(CreateKollectionDto createDto);
        Task<KollectionDto?> UpdateAsync(int id, UpdateKollectionDto updateDto);
        Task<bool> DeleteAsync(int id);
    }
}
