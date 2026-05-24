using System.ComponentModel.DataAnnotations;

namespace SneakerAgregator.DataBase.Models;

public class Offer
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public decimal Price { get; set; }

    [MaxLength(512)]
    public string Url { get; set; } = "";
    public bool InStock { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public Store Store { get; set; } = null!;
    public List<SizeAvailability> Sizes { get; set; } = [];
}
