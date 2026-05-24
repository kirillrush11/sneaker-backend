using Microsoft.EntityFrameworkCore;
using SneakerAgregator.DataBase.Configurations;
using SneakerAgregator.DataBase.Models;

namespace SneakerAgregator.DataBase;

// sneakers.db — модели кроссовок, пользователи, избранное
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new FavoriteConfiguration());
    }
}
