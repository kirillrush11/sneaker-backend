namespace SneakerAgregator.DataBase.Models;

public class SizeAvailability
{
    public int Id { get; set; }
    public int OfferId { get; set; }
    public string Size { get; set; } = "";
    public bool Available { get; set; }

    public Offer Offer { get; set; } = null!;
}
