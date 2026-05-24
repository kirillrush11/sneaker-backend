using Microsoft.EntityFrameworkCore;
using SneakerAgregator.Services.Models;
using SneakerAgregator.DataBase.Models;
using SneakerAgregator.Converters;

namespace SneakerAgregator.DataBase.Repositories;

public class OfferRepository(ProductsDbContext db) : IOfferRepository
{
    public async Task<List<OfferModel>> GetByProductIdsAsync(List<int> productIds)
    {
        var entities = await db.Offers.Where(o => productIds.Contains(o.ProductId)).ToListAsync();
        return entities.Select(FromEfToServiceConverter.ToModel).ToList();
    }

    public async Task<List<OfferModel>> GetByProductIdWithSizesAsync(int productId)
    {
        var entities = await db.Offers.Include(o => o.Store).Include(o => o.Sizes)
            .Where(o => o.ProductId == productId).ToListAsync();
        return entities.OrderBy(o => o.Price).Select(FromEfToServiceConverter.ToModel).ToList();
    }

    public async Task<OfferModel?> GetByIdWithSizesAsync(int id)
    {
        var entity = await db.Offers.Include(o => o.Store).Include(o => o.Sizes).FirstOrDefaultAsync(o => o.Id == id);
        return entity == null ? null : FromEfToServiceConverter.ToModel(entity);
    }

    public async Task<List<OfferModel>> GetPagedAsync(int page, int pageSize, int? storeId, bool? inStock)
    {
        var query = db.Offers.Include(o => o.Store).AsQueryable();
        if (storeId.HasValue) query = query.Where(o => o.StoreId == storeId.Value);
        if (inStock.HasValue)  query = query.Where(o => o.InStock == inStock.Value);
        var entities = await query.OrderByDescending(o => o.LastUpdated)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return entities.Select(FromEfToServiceConverter.ToModel).ToList();
    }

    public async Task<int> CountAsync(int? storeId, bool? inStock)
    {
        var query = db.Offers.AsQueryable();
        if (storeId.HasValue) query = query.Where(o => o.StoreId == storeId.Value);
        if (inStock.HasValue)  query = query.Where(o => o.InStock == inStock.Value);
        return await query.CountAsync();
    }

    public async Task<List<OfferModel>> GetByStoreIdAsync(int storeId)
    {
        var entities = await db.Offers.Where(o => o.StoreId == storeId && o.InStock).OrderBy(o => o.Price).ToListAsync();
        return entities.Select(FromEfToServiceConverter.ToModel).ToList();
    }

    public async Task UpsertAsync(int productId, int storeId, decimal price, string url, bool inStock, List<(string Size, bool Available)> sizes)
    {
        var offer = await db.Offers.Include(o => o.Sizes)
            .FirstOrDefaultAsync(o => o.ProductId == productId && o.StoreId == storeId);

        if (offer == null)
        {
            offer = new Offer { ProductId = productId, StoreId = storeId, Price = price, Url = url, InStock = inStock, LastUpdated = DateTime.UtcNow };
            db.Offers.Add(offer);
            await db.SaveChangesAsync();
        }
        else
        {
            offer.Price = price;
            offer.Url = url;
            offer.InStock = inStock;
            offer.LastUpdated = DateTime.UtcNow;
            db.Sizes.RemoveRange(offer.Sizes);
            await db.SaveChangesAsync();
        }

        foreach (var (size, available) in sizes)
            db.Sizes.Add(new SizeAvailability { OfferId = offer.Id, Size = size, Available = available });

        await db.SaveChangesAsync();
    }

    public async Task<OfferModel?> CreateAsync(int productId, int storeId, decimal price, string url, bool inStock, List<(string Size, bool Available)> sizes)
    {
        var exists = await db.Offers.AnyAsync(o => o.ProductId == productId && o.StoreId == storeId);
        if (exists) return null;

        var offer = new Offer { ProductId = productId, StoreId = storeId, Price = price, Url = url, InStock = inStock, LastUpdated = DateTime.UtcNow };
        db.Offers.Add(offer);
        await db.SaveChangesAsync();

        foreach (var (size, available) in sizes)
            db.Sizes.Add(new SizeAvailability { OfferId = offer.Id, Size = size, Available = available });
        await db.SaveChangesAsync();

        var created = await db.Offers.Include(o => o.Store).Include(o => o.Sizes).FirstAsync(o => o.Id == offer.Id);
        return FromEfToServiceConverter.ToModel(created);
    }

    public async Task<OfferModel?> UpdateAsync(int id, decimal? price, string? url, bool? inStock, List<(string Size, bool Available)>? sizes)
    {
        var offer = await db.Offers.Include(o => o.Store).Include(o => o.Sizes).FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null) return null;

        if (price   != null) offer.Price   = price.Value;
        if (url     != null) offer.Url     = url;
        if (inStock != null) offer.InStock = inStock.Value;
        offer.LastUpdated = DateTime.UtcNow;

        if (sizes != null)
        {
            db.Sizes.RemoveRange(offer.Sizes);
            await db.SaveChangesAsync();
            foreach (var (size, available) in sizes)
                db.Sizes.Add(new SizeAvailability { OfferId = offer.Id, Size = size, Available = available });
        }

        await db.SaveChangesAsync();

        var updated = await db.Offers.Include(o => o.Store).Include(o => o.Sizes).FirstAsync(o => o.Id == id);
        return FromEfToServiceConverter.ToModel(updated);
    }

    public async Task<Dictionary<int, List<string>>> GetAvailableSizesByProductAsync()
    {
        var rows = await db.Sizes
            .Where(s => s.Available && s.Offer.InStock)
            .Select(s => new { s.Offer.ProductId, s.Size })
            .Distinct()
            .ToListAsync();

        return rows.GroupBy(r => r.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Size).ToList());
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var offer = await db.Offers.Include(o => o.Sizes).FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null) return false;
        db.Sizes.RemoveRange(offer.Sizes);
        db.Offers.Remove(offer);
        await db.SaveChangesAsync();
        return true;
    }
}
