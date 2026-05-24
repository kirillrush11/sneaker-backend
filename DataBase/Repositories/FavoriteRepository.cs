using Microsoft.EntityFrameworkCore;
using SneakerAgregator.Services.Models;
using SneakerAgregator.DataBase.Models;
using SneakerAgregator.Converters;

namespace SneakerAgregator.DataBase.Repositories;

public class FavoriteRepository(AppDbContext db) : IFavoriteRepository
{
    public async Task<List<FavoriteModel>> GetByUserIdWithProductAsync(int userId)
    {
        var entities = await db.Favorites.Where(f => f.UserId == userId).Include(f => f.Product).ToListAsync();
        return entities.Select(FromEfToServiceConverter.ToModel).ToList();
    }

    public Task<bool> ExistsAsync(int userId, int productId) =>
        db.Favorites.AnyAsync(f => f.UserId == userId && f.ProductId == productId);

    public Task<bool> ProductExistsAsync(int productId) =>
        db.Products.AnyAsync(p => p.Id == productId);

    public async Task AddAsync(int userId, int productId)
    {
        db.Favorites.Add(new Favorite { UserId = userId, ProductId = productId });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveAsync(int userId, int productId)
    {
        var favorite = await db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
        if (favorite == null) return false;
        db.Favorites.Remove(favorite);
        await db.SaveChangesAsync();
        return true;
    }
}
