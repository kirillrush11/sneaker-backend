using System.ComponentModel.DataAnnotations;

namespace SneakerAgregator.Controllers.Models;

public record RegisterRequest(
    [Required][MinLength(2)] string Username,
    [Required][EmailAddress]  string Email,
    [Required][MinLength(6)]  string Password
);

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required]               string Password
);

public record AuthResponse(string Token, string Username, string Email);
