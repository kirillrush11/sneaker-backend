using Microsoft.EntityFrameworkCore;
using SneakerAgregator.DataBase.Configurations;
using SneakerAgregator.DataBase.Models;

namespace SneakerAgregator.DataBase;

// products.db — магазины, цены, размеры
public class ProductsDbContext(DbContextOptions<ProductsDbContext> options) : DbContext(options)
{
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<SizeAvailability> Sizes => Set<SizeAvailability>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OfferConfiguration());
        modelBuilder.ApplyConfiguration(new SizeAvailabilityConfiguration());
        modelBuilder.ApplyConfiguration(new StoreConfiguration());
    }
}
