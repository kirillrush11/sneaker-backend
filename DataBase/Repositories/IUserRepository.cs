using SneakerAgregator.Services.Models;

namespace SneakerAgregator.DataBase.Repositories;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email);
    Task<UserModel?> GetByEmailAsync(string email);
    Task<UserModel> CreateAsync(UserModel user);
}
