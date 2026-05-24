using SneakerAgregator.Services.Models;
using SneakerAgregator.Controllers.Models;

namespace SneakerAgregator.Converters;

public static class FromServiceToViewModelConverter
{
    public static ProductSummaryDto ToSummary(ProductModel p, List<OfferModel> offers)
    {
        var prices = offers.Where(o => o.InStock).Select(o => o.Price).ToList();
        return new ProductSummaryDto(
            p.Id, p.Brand, p.Model, p.GlobalArticle, p.ImageUrl, p.Gender,
            MinPrice:   prices.Count > 0 ? prices.Min() : 0,
            MaxPrice:   prices.Count > 0 ? prices.Max() : 0,
            StoreCount: offers.Count
        );
    }

    public static ProductDetailDto ToDetail(ProductModel p, List<OfferModel> offers)
    {
        var minPrice  = offers.Where(o => o.InStock).Select(o => o.Price).DefaultIfEmpty(0).Min();
        var offerDtos = offers.OrderBy(o => o.Price).Select(o => ToDto(o, minPrice)).ToList();
        return new ProductDetailDto(p.Id, p.Brand, p.Model, p.GlobalArticle, p.ImageUrl, p.Gender, offerDtos);
    }

    public static StoreDto ToDto(StoreModel s) =>
        new(s.Id, s.Name, s.BaseUrl, s.LogoUrl);

    public static FavoriteDto ToDto(FavoriteModel f, decimal minPrice) =>
        new(f.ProductId, f.Product.Brand, f.Product.Model, f.Product.ImageUrl, minPrice);

    private static OfferDto ToDto(OfferModel o, decimal minPrice) => new(
        o.Id,
        o.Store.Name,
        o.Store.BaseUrl,
        o.Store.LogoUrl,
        o.Price,
        o.InStock,
        o.Url,
        IsBestPrice: o.InStock && o.Price == minPrice,
        Sizes: o.Sizes.Select(s => new SizeDto(s.Size, s.Available)).ToList()
    );
}
