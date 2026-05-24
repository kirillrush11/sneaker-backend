using System.ComponentModel.DataAnnotations;

namespace SneakerAgregator.Controllers.Models;

public record ProductSummaryDto(int Id, string Brand, string Model, string GlobalArticle, string ImageUrl, string Gender, decimal MinPrice, decimal MaxPrice, int StoreCount);

public record ProductDetailDto(int Id, string Brand, string Model, string GlobalArticle, string ImageUrl, string Gender, List<OfferDto> Offers);

public record OfferDto(int Id, string StoreName, string StoreUrl, string StoreLogo, decimal Price, bool InStock, string ProductUrl, bool IsBestPrice, List<SizeDto> Sizes);

public record SizeDto(string Size, bool Available);

public record CreateProductRequest(
    [Required][MaxLength(64)]  string GlobalArticle,
    [Required][MaxLength(64)]  string Brand,
    [Required][MaxLength(128)] string Model,
    [MaxLength(16)]  string Gender   = "",
    [MaxLength(512)] string ImageUrl = ""
);

public record UpdateProductRequest(
    [MaxLength(64)]  string? Brand,
    [MaxLength(128)] string? Model,
    [MaxLength(16)]  string? Gender,
    [MaxLength(512)] string? ImageUrl
);
