using SneakerAgregator.Services.Models;

namespace SneakerAgregator.DataBase.Repositories;

public interface IFavoriteRepository
{
    Task<List<FavoriteModel>> GetByUserIdWithProductAsync(int userId);
    Task<bool> ExistsAsync(int userId, int productId);
    Task<bool> ProductExistsAsync(int productId);
    Task AddAsync(int userId, int productId);
    Task<bool> RemoveAsync(int userId, int productId);
}
