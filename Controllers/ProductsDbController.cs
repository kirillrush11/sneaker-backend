using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SneakerAgregator.Controllers.Models;
using SneakerAgregator.DataBase.Repositories;

namespace SneakerAgregator.Controllers;

[ApiController]
[Route("api/productsdb")]
public class ProductsDbController(
    IProductRepository productRepo,
    IOfferRepository offerRepo,
    IStoreRepository storeRepo) : ControllerBase
{
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var products    = await productRepo.GetFilteredAsync(null, null);
        var stores      = await storeRepo.GetAllAsync();
        var totalOffers = await offerRepo.CountAsync(null, null);
        var inStock     = await offerRepo.CountAsync(null, true);

        return Ok(new
        {
            ProductsInSneakersDb = products.Count,
            Stores     = stores.Count,
            Offers     = totalOffers,
            InStock    = inStock,
            OutOfStock = totalOffers - inStock
        });
    }

    [HttpGet("offers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOffers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? storeId = null,
        [FromQuery] bool? inStock = null)
    {
        var total  = await offerRepo.CountAsync(storeId, inStock);
        var offers = await offerRepo.GetPagedAsync(page, pageSize, storeId, inStock);

        var productIds = offers.Select(o => o.ProductId).Distinct().ToList();
        var products   = (await productRepo.GetFilteredAsync(null, null))
            .Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        var result = offers.Select(o =>
        {
            products.TryGetValue(o.ProductId, out var p);
            return new
            {
                o.Id, o.ProductId,
                Brand         = p?.Brand ?? "",
                Model         = p?.Model ?? "(товар не найден)",
                GlobalArticle = p?.GlobalArticle ?? "",
                Store         = o.Store.Name,
                o.Price, o.InStock, o.Url, o.LastUpdated
            };
        });

        return Ok(new { Total = total, Page = page, PageSize = pageSize, Items = result });
    }

    [HttpGet("offers/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOffer(int id)
    {
        var offer = await offerRepo.GetByIdWithSizesAsync(id);
        if (offer == null) return NotFound(new { message = "Оффер не найден" });

        var product = await productRepo.GetByIdAsync(offer.ProductId);

        return Ok(new
        {
            offer.Id, offer.ProductId,
            Brand         = product?.Brand ?? "",
            Model         = product?.Model ?? "(товар не найден)",
            GlobalArticle = product?.GlobalArticle ?? "",
            Store         = offer.Store.Name,
            offer.Price, offer.InStock, offer.Url, offer.LastUpdated,
            Sizes = offer.Sizes.OrderBy(s => s.Size).Select(s => new { s.Size, s.Available })
        });
    }

    [HttpGet("offers/by-product/{productId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOffersByProduct(int productId)
    {
        var product = await productRepo.GetByIdAsync(productId);
        if (product == null) return NotFound(new { message = "Товар не найден в sneakers.db" });

        var offers = await offerRepo.GetByProductIdWithSizesAsync(productId);

        return Ok(new
        {
            product.Id, product.Brand, product.Model, product.GlobalArticle,
            Offers = offers.Select(o => new
            {
                o.Id, Store = o.Store.Name, o.Price, o.InStock, o.Url, o.LastUpdated,
                Sizes = o.Sizes.OrderBy(s => s.Size).Select(s => new { s.Size, s.Available })
            })
        });
    }

    [HttpGet("offers/by-article/{article}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOffersByArticle(string article)
    {
        var product = await productRepo.GetByArticleAsync(article);
        if (product == null) return NotFound(new { message = $"Артикул '{article}' не найден" });
        return await GetOffersByProduct(product.Id);
    }

    [HttpGet("compare/{productId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ComparePrice(int productId)
    {
        var product = await productRepo.GetByIdAsync(productId);
        if (product == null) return NotFound(new { message = "Товар не найден" });

        var offers  = await offerRepo.GetByProductIdWithSizesAsync(productId);
        var inStock = offers.Where(o => o.InStock).OrderBy(o => o.Price).ToList();

        if (inStock.Count == 0)
            return Ok(new { product.Brand, product.Model, message = "Нет предложений в наличии", Offers = Array.Empty<object>() });

        var best = inStock.First();
        return Ok(new
        {
            product.Brand, product.Model, product.GlobalArticle,
            BestPrice = best.Price,
            BestStore = best.Store.Name,
            Savings   = inStock.Last().Price - best.Price,
            Offers    = inStock.Select(o => new
            {
                Store          = o.Store.Name,
                o.Price, o.Url,
                AvailableSizes = o.Sizes.Where(s => s.Available).Select(s => s.Size).OrderBy(s => s)
            })
        });
    }

    [HttpPost("offers")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOffer([FromBody] CreateOfferRequest request)
    {
        if (!await storeRepo.ExistsAsync(request.StoreId))
            return NotFound(new { message = "Магазин не найден" });

        if (await productRepo.GetByIdAsync(request.ProductId) == null)
            return NotFound(new { message = "Товар не найден" });

        var sizes = request.Sizes?.Select(s => (s.Size, s.Available)).ToList() ?? [];
        var offer = await offerRepo.CreateAsync(request.ProductId, request.StoreId, request.Price, request.Url, request.InStock, sizes);

        if (offer == null)
            return Conflict(new { message = "Оффер для этого товара в данном магазине уже существует" });

        return CreatedAtAction(nameof(GetOffer), new { id = offer.Id }, new { offer.Id, offer.ProductId, offer.StoreId, offer.Price, offer.InStock, offer.Url });
    }

    [HttpPatch("offers/{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOffer(int id, [FromBody] UpdateOfferRequest request)
    {
        var sizes = request.Sizes?.Select(s => (s.Size, s.Available)).ToList();
        var offer = await offerRepo.UpdateAsync(id, request.Price, request.Url, request.InStock, sizes);

        if (offer == null) return NotFound(new { message = "Оффер не найден" });

        return Ok(new { offer.Id, offer.ProductId, offer.StoreId, offer.Price, offer.InStock, offer.Url, offer.LastUpdated,
            Sizes = offer.Sizes.Select(s => new { s.Size, s.Available }) });
    }

    [HttpDelete("offers/{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOffer(int id)
    {
        var deleted = await offerRepo.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = "Оффер не найден" });
        return NoContent();
    }

    [HttpGet("stores/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStoresSummary()
    {
        var stores    = await storeRepo.GetAllAsync();
        var allOffers = await offerRepo.GetPagedAsync(1, int.MaxValue, null, null);

        var result = stores.Select(s =>
        {
            var storeOffers   = allOffers.Where(o => o.StoreId == s.Id).ToList();
            var inStockOffers = storeOffers.Where(o => o.InStock).ToList();
            return new
            {
                s.Id, s.Name, s.BaseUrl,
                TotalOffers = storeOffers.Count,
                InStock     = inStockOffers.Count,
                AvgPrice    = inStockOffers.Count > 0 ? Math.Round(inStockOffers.Average(o => (double)o.Price), 0) : 0,
                MinPrice    = inStockOffers.Count > 0 ? inStockOffers.Min(o => o.Price) : 0,
                MaxPrice    = inStockOffers.Count > 0 ? inStockOffers.Max(o => o.Price) : 0
            };
        });

        return Ok(result);
    }
}
