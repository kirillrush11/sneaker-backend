using Microsoft.EntityFrameworkCore;

namespace SneakerAgregator.DataBase;

public static class Extensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? ".";
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite($"Data Source={Path.Combine(dbPath, "sneakers.db")}"));
        services.AddDbContext<ProductsDbContext>(opt =>
            opt.UseSqlite($"Data Source={Path.Combine(dbPath, "products.db")}"));
        return services;
    }
}
