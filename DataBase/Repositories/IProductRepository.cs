using SneakerAgregator.Services.Models;

namespace SneakerAgregator.DataBase.Repositories;

public interface IProductRepository
{
    Task<List<ProductModel>> GetFilteredAsync(string? brand, string? gender);
    Task<ProductModel?> GetByIdAsync(int id);
    Task<ProductModel?> GetByArticleAsync(string article);
    Task<List<ProductModel>> SearchAsync(string query);
    Task<List<ProductModel>> GetNewArrivalsAsync(int count);
    Task<List<string>> GetBrandsAsync();
    Task<ProductModel> UpsertAsync(string article, string brand, string model, string gender, string imageUrl);
    Task<ProductModel?> UpdateAsync(int id, string? brand, string? model, string? gender, string? imageUrl);
    Task<bool> DeleteAsync(int id);
}
