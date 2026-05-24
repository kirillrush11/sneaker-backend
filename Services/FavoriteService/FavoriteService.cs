using SneakerAgregator.DataBase.Repositories;
using SneakerAgregator.Controllers.Models;
using SneakerAgregator.Converters;

namespace SneakerAgregator.Services;

public class FavoriteService(IFavoriteRepository favoriteRepo, IOfferRepository offerRepo) : IFavoriteService
{
    public async Task<List<FavoriteDto>> GetFavoritesAsync(int userId)
    {
        var favorites  = await favoriteRepo.GetByUserIdWithProductAsync(userId);
        var productIds = favorites.Select(f => f.ProductId).ToList();
        var offers     = await offerRepo.GetByProductIdsAsync(productIds);

        var minPriceByProduct = offers
            .Where(o => o.InStock)
            .GroupBy(o => o.ProductId)
            .ToDictionary(g => g.Key, g => g.Min(o => o.Price));

        return favorites.Select(f =>
            FromServiceToViewModelConverter.ToDto(f, minPriceByProduct.GetValueOrDefault(f.ProductId, 0))
        ).ToList();
    }

    public async Task<bool> AddFavoriteAsync(int userId, int productId)
    {
        if (await favoriteRepo.ExistsAsync(userId, productId)) return false;
        if (!await favoriteRepo.ProductExistsAsync(productId)) return false;
        await favoriteRepo.AddAsync(userId, productId);
        return true;
    }

    public Task<bool> RemoveFavoriteAsync(int userId, int productId) =>
        favoriteRepo.RemoveAsync(userId, productId);
}
