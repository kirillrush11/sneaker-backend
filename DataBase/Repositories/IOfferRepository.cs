using SneakerAgregator.Services.Models;

namespace SneakerAgregator.DataBase.Repositories;

public interface IOfferRepository
{
    Task<List<OfferModel>> GetByProductIdsAsync(List<int> productIds);
    Task<List<OfferModel>> GetByProductIdWithSizesAsync(int productId);
    Task<OfferModel?> GetByIdWithSizesAsync(int id);
    Task<List<OfferModel>> GetPagedAsync(int page, int pageSize, int? storeId, bool? inStock);
    Task<int> CountAsync(int? storeId, bool? inStock);
    Task<List<OfferModel>> GetByStoreIdAsync(int storeId);
    Task UpsertAsync(int productId, int storeId, decimal price, string url, bool inStock, List<(string Size, bool Available)> sizes);
    Task<OfferModel?> CreateAsync(int productId, int storeId, decimal price, string url, bool inStock, List<(string Size, bool Available)> sizes);
    Task<OfferModel?> UpdateAsync(int id, decimal? price, string? url, bool? inStock, List<(string Size, bool Available)>? sizes);
    Task<bool> DeleteAsync(int id);
    Task<Dictionary<int, List<string>>> GetAvailableSizesByProductAsync();
}
