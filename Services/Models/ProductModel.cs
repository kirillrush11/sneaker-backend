namespace SneakerAgregator.Services.Models;

public class ProductModel
{
    public int Id { get; init; }
    public string GlobalArticle { get; init; } = "";
    public string Brand { get; init; } = "";
    public string Model { get; init; } = "";
    public string ImageUrl { get; init; } = "";
    public string Gender { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}
