using Microsoft.EntityFrameworkCore;
using SneakerAgregator.Services.Models;
using SneakerAgregator.DataBase.Models;
using SneakerAgregator.Converters;

namespace SneakerAgregator.DataBase.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<bool> ExistsByEmailAsync(string email) =>
        db.Users.AnyAsync(u => u.Email == email);

    public async Task<UserModel?> GetByEmailAsync(string email)
    {
        var entity = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        return entity == null ? null : FromEfToServiceConverter.ToModel(entity);
    }

    public async Task<UserModel> CreateAsync(UserModel user)
    {
        var entity = new User
        {
            Username     = user.Username,
            Email        = user.Email,
            PasswordHash = user.PasswordHash
        };
        db.Users.Add(entity);
        await db.SaveChangesAsync();
        return FromEfToServiceConverter.ToModel(entity);
    }
}
