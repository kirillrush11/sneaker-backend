using SneakerAgregator.Controllers.Models;
using SneakerAgregator.Services.Models;

namespace SneakerAgregator.Services;

public interface IProductService
{
    Task<List<ProductSummaryDto>> GetCatalogAsync(string? brand = null, string? gender = null);
    Task<List<ProductSummaryDto>> GetNewArrivalsAsync(int count = 10);
    Task<ProductDetailDto?> GetProductAsync(int id);
    Task<List<ProductSummaryDto>> SearchAsync(string query);
    Task<object?> GetSizeAvailabilityAsync(int productId);
    Task<List<string>> GetBrandsAsync();
    Task<ProductModel?> CreateAsync(CreateProductRequest r);
    Task<ProductModel?> UpdateAsync(int id, UpdateProductRequest r);
    Task<bool> DeleteAsync(int id);
}
