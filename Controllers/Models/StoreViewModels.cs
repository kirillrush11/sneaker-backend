using System.ComponentModel.DataAnnotations;

namespace SneakerAgregator.Controllers.Models;

public record StoreDto(int Id, string Name, string BaseUrl, string LogoUrl);

public record CreateStoreRequest(
    [Required] string Name,
    [Required] string BaseUrl,
    string LogoUrl = ""
);

public record UpdateStoreRequest(
    string? Name,
    string? BaseUrl,
    string? LogoUrl
);
