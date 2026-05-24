using SneakerAgregator.Services.Models;
using SneakerAgregator.DataBase.Models;

namespace SneakerAgregator.Converters;

public static class FromServiceToEfConverter
{
    public static User ToEntity(UserModel m) => new()
    {
        Id           = m.Id,
        Username     = m.Username,
        Email        = m.Email,
        PasswordHash = m.PasswordHash,
        CreatedAt    = m.CreatedAt
    };
}
