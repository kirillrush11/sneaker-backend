using SneakerAgregator.Services.Models;
using SneakerAgregator.Controllers.Models;

namespace SneakerAgregator.Converters;

public static class FromViewModelToServiceConverter
{
    public static UserModel ToModel(RegisterRequest r, string passwordHash) => new()
    {
        Username     = r.Username,
        Email        = r.Email,
        PasswordHash = passwordHash
    };
}
