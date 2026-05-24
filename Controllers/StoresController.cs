using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SneakerAgregator.Converters;
using SneakerAgregator.DataBase.Repositories;
using SneakerAgregator.Controllers.Models;

namespace SneakerAgregator.Controllers;

[ApiController]
[Route("api/stores")]
public class StoresController(IStoreRepository storeRepo, IOfferRepository offerRepo, IProductRepository productRepo) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStores()
    {
        var stores = await storeRepo.GetAllAsync();
        return Ok(stores.Select(FromServiceToViewModelConverter.ToDto));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStore(int id)
    {
        var store = await storeRepo.GetByIdAsync(id);
        if (store == null) return NotFound(new { message = "Магазин не найден" });
        return Ok(FromServiceToViewModelConverter.ToDto(store));
    }

    [HttpGet("{id:int}/products")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStoreProducts(int id)
    {
        if (!await storeRepo.ExistsAsync(id)) return NotFound(new { message = "Магазин не найден" });

        var offers     = await offerRepo.GetByStoreIdAsync(id);
        var productIds = offers.Select(o => o.ProductId).Distinct().ToList();
        var products   = (await productRepo.GetFilteredAsync(null, null))
            .Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        var result = offers
            .Where(o => products.ContainsKey(o.ProductId))
            .Select(o => new
            {
                products[o.ProductId].Id,
                products[o.ProductId].Brand,
                products[o.ProductId].Model,
                products[o.ProductId].GlobalArticle,
                products[o.ProductId].ImageUrl,
                o.Price,
                o.Url
            });

        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequest request)
    {
        if (await storeRepo.GetByNameAsync(request.Name) != null)
            return Conflict(new { message = $"Магазин '{request.Name}' уже существует" });
        var store = await storeRepo.CreateAsync(request.Name, request.BaseUrl, request.LogoUrl);
        return CreatedAtAction(nameof(GetStore), new { id = store.Id }, FromServiceToViewModelConverter.ToDto(store));
    }

    [HttpPatch("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStore(int id, [FromBody] UpdateStoreRequest request)
    {
        var store = await storeRepo.UpdateAsync(id, request.Name, request.BaseUrl, request.LogoUrl);
        if (store == null) return NotFound(new { message = "Магазин не найден" });
        return Ok(FromServiceToViewModelConverter.ToDto(store));
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStore(int id)
    {
        var deleted = await storeRepo.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = "Магазин не найден" });
        return NoContent();
    }
}
