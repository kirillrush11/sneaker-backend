namespace SneakerAgregator.Services.Models;

public class UserModel
{
    public int Id { get; init; }
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}
