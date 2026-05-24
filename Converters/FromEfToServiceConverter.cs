using SneakerAgregator.Services.Models;
using SneakerAgregator.DataBase.Models;

namespace SneakerAgregator.Converters;

public static class FromEfToServiceConverter
{
    public static ProductModel ToModel(Product e) => new()
    {
        Id            = e.Id,
        GlobalArticle = e.GlobalArticle,
        Brand         = e.Brand,
        Model         = e.Model,
        ImageUrl      = e.ImageUrl,
        Gender        = e.Gender,
        CreatedAt     = e.CreatedAt
    };

    public static UserModel ToModel(User e) => new()
    {
        Id           = e.Id,
        Username     = e.Username,
        Email        = e.Email,
        PasswordHash = e.PasswordHash,
        CreatedAt    = e.CreatedAt
    };

    public static StoreModel ToModel(Store e) => new()
    {
        Id      = e.Id,
        Name    = e.Name,
        BaseUrl = e.BaseUrl,
        LogoUrl = e.LogoUrl
    };

    public static SizeModel ToModel(SizeAvailability e) => new()
    {
        Size      = e.Size,
        Available = e.Available
    };

    public static OfferModel ToModel(Offer e) => new()
    {
        Id          = e.Id,
        ProductId   = e.ProductId,
        StoreId     = e.StoreId,
        Price       = e.Price,
        Url         = e.Url,
        InStock     = e.InStock,
        LastUpdated = e.LastUpdated,
        Store       = e.Store != null ? ToModel(e.Store) : new StoreModel(),
        Sizes       = e.Sizes?.Select(ToModel).ToList() ?? []
    };

    public static FavoriteModel ToModel(Favorite e) => new()
    {
        Id        = e.Id,
        UserId    = e.UserId,
        ProductId = e.ProductId,
        AddedAt   = e.AddedAt,
        Product   = e.Product != null ? ToModel(e.Product) : new ProductModel()
    };
}
