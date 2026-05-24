using SneakerAgregator.Controllers.Models;

namespace SneakerAgregator.Services;

public interface IFavoriteService
{
    Task<List<FavoriteDto>> GetFavoritesAsync(int userId);
    Task<bool> AddFavoriteAsync(int userId, int productId);
    Task<bool> RemoveFavoriteAsync(int userId, int productId);
}
