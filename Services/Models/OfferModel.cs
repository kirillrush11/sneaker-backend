namespace SneakerAgregator.Services.Models;

public class OfferModel
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public int StoreId { get; init; }
    public decimal Price { get; init; }
    public string Url { get; init; } = "";
    public bool InStock { get; init; }
    public DateTime LastUpdated { get; init; }
    public StoreModel Store { get; init; } = new();
    public List<SizeModel> Sizes { get; init; } = [];
}
