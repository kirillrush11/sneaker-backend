namespace SneakerAgregator.DataBase.Models;

public class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string LogoUrl { get; set; } = "";

    public List<Offer> Offers { get; set; } = [];
}
