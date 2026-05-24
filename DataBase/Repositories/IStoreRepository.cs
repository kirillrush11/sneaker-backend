using SneakerAgregator.Services.Models;

namespace SneakerAgregator.DataBase.Repositories;

public interface IStoreRepository
{
    Task<List<StoreModel>> GetAllAsync();
    Task<StoreModel?> GetByIdAsync(int id);
    Task<StoreModel?> GetByNameAsync(string name);
    Task<bool> ExistsAsync(int id);
    Task<StoreModel> CreateAsync(string name, string baseUrl, string logoUrl);
    Task<StoreModel?> UpdateAsync(int id, string? name, string? baseUrl, string? logoUrl);
    Task<bool> DeleteAsync(int id);
}
