using Microsoft.EntityFrameworkCore;

namespace SneakerAgregator.DataBase;

public static class Extensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=sneakers.db"));
        services.AddDbContext<ProductsDbContext>(opt => opt.UseSqlite("Data Source=products.db"));
        return services;
    }
}
