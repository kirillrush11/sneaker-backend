using System.ComponentModel.DataAnnotations;

namespace SneakerAgregator.DataBase.Models;

public class Product
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string GlobalArticle { get; set; } = "";

    [MaxLength(64)]
    public string Brand { get; set; } = "";

    [MaxLength(128)]
    public string Model { get; set; } = "";

    [MaxLength(512)]
    public string ImageUrl { get; set; } = "";

    [MaxLength(16)]
    public string Gender { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Favorite> Favorites { get; set; } = [];
}
