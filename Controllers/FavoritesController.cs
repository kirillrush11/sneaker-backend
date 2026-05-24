using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SneakerAgregator.Services;
using System.Security.Claims;

namespace SneakerAgregator.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController(IFavoriteService favoriteService) : ControllerBase
{
    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFavorites()
        => Ok(await favoriteService.GetFavoritesAsync(UserId()));

    [HttpPost("{productId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddFavorite(int productId)
    {
        var success = await favoriteService.AddFavoriteAsync(UserId(), productId);
        if (!success) return Conflict(new { message = "Уже в избранном или товар не найден" });
        return Ok(new { message = "Добавлено в избранное" });
    }

    [HttpDelete("{productId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavorite(int productId)
    {
        var success = await favoriteService.RemoveFavoriteAsync(UserId(), productId);
        if (!success) return NotFound(new { message = "Не найдено в избранном" });
        return Ok(new { message = "Удалено из избранного" });
    }
}
