using Microsoft.EntityFrameworkCore;
using SneakerAgregator.Services.Models;
using SneakerAgregator.DataBase.Models;
using SneakerAgregator.Converters;

namespace SneakerAgregator.DataBase.Repositories;

public class StoreRepository(ProductsDbContext db) : IStoreRepository
{
    public async Task<List<StoreModel>> GetAllAsync()
    {
        var entities = await db.Stores.ToListAsync();
        return entities.Select(FromEfToServiceConverter.ToModel).ToList();
    }

    public async Task<StoreModel?> GetByIdAsync(int id)
    {
        var entity = await db.Stores.FindAsync(id);
        return entity == null ? null : FromEfToServiceConverter.ToModel(entity);
    }

    public async Task<StoreModel?> GetByNameAsync(string name)
    {
        var entity = await db.Stores.FirstOrDefaultAsync(s => s.Name == name);
        return entity == null ? null : FromEfToServiceConverter.ToModel(entity);
    }

    public Task<bool> ExistsAsync(int id) => db.Stores.AnyAsync(s => s.Id == id);

    public async Task<StoreModel> CreateAsync(string name, string baseUrl, string logoUrl)
    {
        var store = new Store { Name = name, BaseUrl = baseUrl, LogoUrl = logoUrl };
        db.Stores.Add(store);
        await db.SaveChangesAsync();
        return FromEfToServiceConverter.ToModel(store);
    }

    public async Task<StoreModel?> UpdateAsync(int id, string? name, string? baseUrl, string? logoUrl)
    {
        var store = await db.Stores.FindAsync(id);
        if (store == null) return null;

        if (name    != null) store.Name    = name;
        if (baseUrl != null) store.BaseUrl = baseUrl;
        if (logoUrl != null) store.LogoUrl = logoUrl;

        await db.SaveChangesAsync();
        return FromEfToServiceConverter.ToModel(store);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var store = await db.Stores.FindAsync(id);
        if (store == null) return false;
        db.Stores.Remove(store);
        await db.SaveChangesAsync();
        return true;
    }
}
