namespace SneakerAgregator.Controllers.Models;

public record FavoriteDto(int ProductId, string Brand, string Model, string ImageUrl, decimal MinPrice);
