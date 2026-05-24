using Microsoft.IdentityModel.Tokens;
using SneakerAgregator.Services.Models;
using SneakerAgregator.Converters;
using SneakerAgregator.DataBase.Repositories;
using SneakerAgregator.Controllers.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SneakerAgregator.Services;

public class AuthService(IUserRepository userRepository, IConfiguration config) : IAuthService
{
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email))
            return null;

        var user    = FromViewModelToServiceConverter.ToModel(request, BCrypt.Net.BCrypt.HashPassword(request.Password));
        var created = await userRepository.CreateAsync(user);
        return new AuthResponse(GenerateToken(created), created.Username, created.Email);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return new AuthResponse(GenerateToken(user), user.Username, user.Email);
    }

    private string GenerateToken(UserModel user)
    {
        var key         = config["Jwt:Key"] ?? "super-secret-key-minimum-32-characters!";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var token = new JwtSecurityToken(
            claims:             claims,
            expires:            DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
