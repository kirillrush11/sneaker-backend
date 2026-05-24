using SneakerAgregator.Services.Models;
using SneakerAgregator.Converters;
using SneakerAgregator.DataBase.Repositories;
using SneakerAgregator.Controllers.Models;

namespace SneakerAgregator.Services;

public class ProductService(IProductRepository productRepo, IOfferRepository offerRepo) : IProductService
{
    public async Task<List<ProductSummaryDto>> GetCatalogAsync(string? brand = null, string? gender = null)
    {
        var products   = await productRepo.GetFilteredAsync(brand, gender);
        var productIds = products.Select(p => p.Id).ToList();
        var offers     = await offerRepo.GetByProductIdsAsync(productIds);
        var offerMap   = offers.GroupBy(o => o.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        return products
            .Select(p => FromServiceToViewModelConverter.ToSummary(p, offerMap.GetValueOrDefault(p.Id, [])))
            .OrderByDescending(p => p.StoreCount > 1)
            .ToList();
    }

    public async Task<List<ProductSummaryDto>> GetNewArrivalsAsync(int count = 10)
    {
        var products   = await productRepo.GetNewArrivalsAsync(count);
        var productIds = products.Select(p => p.Id).ToList();
        var offers     = await offerRepo.GetByProductIdsAsync(productIds);
        var offerMap   = offers.GroupBy(o => o.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        return products.Select(p =>
            FromServiceToViewModelConverter.ToSummary(p, offerMap.GetValueOrDefault(p.Id, []))
        ).ToList();
    }

    public async Task<ProductDetailDto?> GetProductAsync(int id)
    {
        var product = await productRepo.GetByIdAsync(id);
        if (product == null) return null;

        var offers = await offerRepo.GetByProductIdWithSizesAsync(id);
        return FromServiceToViewModelConverter.ToDetail(product, offers);
    }

    public async Task<List<ProductDetailDto>> SearchAsync(string query)
    {
        var products = await productRepo.SearchAsync(query);
        if (products.Count == 0) return [];

        var productIds = products.Select(p => p.Id).ToList();
        var offers     = await offerRepo.GetByProductIdsAsync(productIds);
        var offerMap   = offers.GroupBy(o => o.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        return products.Select(p =>
            FromServiceToViewModelConverter.ToDetail(p, offerMap.GetValueOrDefault(p.Id, []))
        ).ToList();
    }

    public async Task<object?> GetSizeAvailabilityAsync(int productId)
    {
        var product = await productRepo.GetByIdAsync(productId);
        if (product == null) return null;

        var offers   = await offerRepo.GetByProductIdWithSizesAsync(productId);
        var allSizes = offers.SelectMany(o => o.Sizes).Select(s => s.Size).Distinct().OrderBy(s => s).ToList();

        var sizes = allSizes.Select(size => new
        {
            Size   = size,
            Stores = offers
                .Where(o => o.Sizes.Any(s => s.Size == size && s.Available))
                .Select(o => new { o.Store.Name, o.Price, o.Url })
                .OrderBy(s => s.Price)
                .ToList()
        });

        return new { product.Id, product.Brand, product.Model, Sizes = sizes };
    }

    public Task<List<string>> GetBrandsAsync() => productRepo.GetBrandsAsync();

    public async Task<ProductModel?> CreateAsync(CreateProductRequest r)
    {
        if (await productRepo.GetByArticleAsync(r.GlobalArticle) != null) return null;
        return await productRepo.UpsertAsync(r.GlobalArticle, r.Brand, r.Model, r.Gender, r.ImageUrl);
    }

    public Task<ProductModel?> UpdateAsync(int id, UpdateProductRequest r) =>
        productRepo.UpdateAsync(id, r.Brand, r.Model, r.Gender, r.ImageUrl);

    public Task<bool> DeleteAsync(int id) => productRepo.DeleteAsync(id);
}
