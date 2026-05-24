using Microsoft.EntityFrameworkCore;
using SneakerAgregator.Services.Models;
using SneakerAgregator.DataBase.Models;
using SneakerAgregator.Converters;

namespace SneakerAgregator.DataBase.Repositories;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<List<ProductModel>> GetFilteredAsync(string? brand, string? gender)
    {
        var query = db.Products.AsQueryable();
        if (!string.IsNullOrEmpty(brand))
            query = query.Where(p => p.Brand.ToLower() == brand.ToLower());
        if (!string.IsNullOrEmpty(gender))
            query = query.Where(p => p.Gender == gender);
        var entities = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return entities.Select(FromEfToServiceConverter.ToModel).ToList();
    }

    public async Task<ProductModel?> GetByIdAsync(int id)
    {
        var entity = await db.Products.FindAsync(id);
        return entity == null ? null : FromEfToServiceConverter.ToModel(entity);
    }

    public async Task<ProductModel?> GetByArticleAsync(string article)
    {
        var entity = await db.Products.FirstOrDefaultAsync(p => p.GlobalArticle == article);
        return entity == null ? null : FromEfToServiceConverter.ToModel(entity);
    }

    public async Task<List<ProductModel>> SearchAsync(string query)
    {
        var q = query.ToLower();
        var entities = await db.Products
            .Where(p => p.Model.ToLower().Contains(q)
                     || p.Brand.ToLower().Contains(q)
                     || p.GlobalArticle.ToLower().Contains(q))
            .ToListAsync();
        return entities.Select(FromEfToServiceConverter.ToModel).ToList();
    }

    public async Task<List<ProductModel>> GetNewArrivalsAsync(int count)
    {
        var entities = await db.Products.OrderByDescending(p => p.CreatedAt).Take(count).ToListAsync();
        return entities.Select(FromEfToServiceConverter.ToModel).ToList();
    }

    public Task<List<string>> GetBrandsAsync() =>
        db.Products.Where(p => p.Brand != "").Select(p => p.Brand).Distinct().OrderBy(b => b).ToListAsync();

    public async Task<ProductModel> UpsertAsync(string article, string brand, string model, string gender, string imageUrl)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.GlobalArticle == article);
        if (product == null)
        {
            product = new Product { GlobalArticle = article, Brand = brand, Model = model, Gender = gender, ImageUrl = imageUrl };
            db.Products.Add(product);
        }
        else
        {
            bool changed = false;
            if (product.Model != model)                                                      { product.Model    = model;    changed = true; }
            if (product.Brand != brand && !string.IsNullOrEmpty(brand))                     { product.Brand    = brand;    changed = true; }
            if (string.IsNullOrEmpty(product.ImageUrl) && !string.IsNullOrEmpty(imageUrl))  { product.ImageUrl = imageUrl; changed = true; }
            if (!string.IsNullOrEmpty(gender) && product.Gender != gender)                  { product.Gender   = gender;   changed = true; }
            if (!changed) return FromEfToServiceConverter.ToModel(product);
        }
        await db.SaveChangesAsync();
        return FromEfToServiceConverter.ToModel(product);
    }

    public async Task<ProductModel?> UpdateAsync(int id, string? brand, string? model, string? gender, string? imageUrl)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return null;

        if (brand    != null) product.Brand    = brand;
        if (model    != null) product.Model    = model;
        if (gender   != null) product.Gender   = gender;
        if (imageUrl != null) product.ImageUrl = imageUrl;

        await db.SaveChangesAsync();
        return FromEfToServiceConverter.ToModel(product);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return false;
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return true;
    }
}
