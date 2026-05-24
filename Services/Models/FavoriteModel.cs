namespace SneakerAgregator.Services.Models;

public class FavoriteModel
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public int ProductId { get; init; }
    public DateTime AddedAt { get; init; }
    public ProductModel Product { get; init; } = new();
}
