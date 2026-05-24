using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SneakerAgregator.DataBase.Models;

namespace SneakerAgregator.DataBase.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.HasData(
            new Store { Id = 1, Name = "Kixbox",      BaseUrl = "https://kixbox.ru",      LogoUrl = "" },
            new Store { Id = 2, Name = "Street Beat", BaseUrl = "https://street-beat.ru", LogoUrl = "" },
            new Store { Id = 3, Name = "Brandshop",   BaseUrl = "https://brandshop.ru",   LogoUrl = "" }
        );
    }
}
