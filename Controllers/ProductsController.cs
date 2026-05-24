using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SneakerAgregator.Services;
using SneakerAgregator.Controllers.Models;

namespace SneakerAgregator.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog([FromQuery] string? brand, [FromQuery] string? gender)
    {
        var products = await productService.GetCatalogAsync(brand, gender);
        return Ok(products);
    }

    [HttpGet("new")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNewArrivals([FromQuery] int count = 10)
        => Ok(await productService.GetNewArrivalsAsync(count));

    [HttpGet("brands")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBrands()
        => Ok(await productService.GetBrandsAsync());

    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Введите название пары" });

        var results = await productService.SearchAsync(q);
        if (!results.Any()) return NotFound(new { message = "Ничего не найдено" });
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await productService.GetProductAsync(id);
        if (product == null) return NotFound(new { message = "Товар не найден" });
        return Ok(product);
    }

    [HttpGet("{id:int}/sizes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSizes(int id)
    {
        var result = await productService.GetSizeAvailabilityAsync(id);
        if (result == null) return NotFound(new { message = "Товар не найден" });
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = await productService.CreateAsync(request);
        if (product == null)
            return Conflict(new { message = $"Артикул '{request.GlobalArticle}' уже существует" });
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPatch("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var product = await productService.UpdateAsync(id, request);
        if (product == null) return NotFound(new { message = "Товар не найден" });
        return Ok(product);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var deleted = await productService.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = "Товар не найден" });
        return NoContent();
    }
}
