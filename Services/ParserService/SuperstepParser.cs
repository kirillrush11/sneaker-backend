using System.Text.Json;
using System.Text.RegularExpressions;
using SneakerAgregator.DataBase.Repositories;

namespace SneakerAgregator.Services.ParserService;

public class SuperstepParser(
    IProductRepository productRepo,
    IOfferRepository offerRepo,
    IStoreRepository storeRepo,
    ILogger<SuperstepParser> logger)
{
    private const string CatalogBaseUrl = "https://superstep.ru/catalog/obuv/krossovki/";
    private const int MaxPages = 40;

    private int _storeId;

    public async Task ParseAsync()
    {
        logger.LogInformation("=== Superstep: парсинг запущен ===");

        var store = await storeRepo.GetByNameAsync("Superstep");
        if (store == null)
        {
            logger.LogError("Магазин 'Superstep' не найден в products.db.");
            return;
        }
        _storeId = store.Id;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9");

        var urls = await CollectProductUrlsAsync(http);
        logger.LogInformation("Найдено товаров: {Count}", urls.Count);

        if (urls.Count == 0)
        {
            logger.LogWarning("Товары не найдены — возможно изменилась структура сайта.");
            return;
        }

        int parsed = 0;
        foreach (var url in urls)
        {
            try
            {
                await ParseProductAsync(http, url);
                parsed++;
                await Task.Delay(Random.Shared.Next(300, 700));
            }
            catch (Exception ex)
            {
                logger.LogWarning("Ошибка при парсинге {Url}: {Error}", url, ex.Message);
            }

            if (parsed % 50 == 0)
                logger.LogInformation("Обработано: {Count}/{Total}", parsed, urls.Count);
        }

        logger.LogInformation("=== Superstep: завершён. Обработано: {Count} ===", parsed);
    }

    private async Task<List<string>> CollectProductUrlsAsync(HttpClient http)
    {
        var urls = new HashSet<string>();

        for (int page = 1; page <= MaxPages; page++)
        {
            var pageUrl = page == 1 ? CatalogBaseUrl : $"{CatalogBaseUrl}?page={page}";

            string html;
            try { html = await http.GetStringAsync(pageUrl); }
            catch (Exception ex) { logger.LogWarning("Ошибка загрузки страницы {Page}: {Error}", page, ex.Message); break; }

            var matches = Regex.Matches(html, @"href=""(/product/[^""]+)""");
            if (matches.Count == 0) break;

            int before = urls.Count;
            foreach (Match m in matches)
                urls.Add("https://superstep.ru" + m.Groups[1].Value);

            logger.LogInformation("Страница {Page}: +{New} товаров (всего {Total})", page, urls.Count - before, urls.Count);

            if (!Regex.IsMatch(html, $@"page={page + 1}")) break;

            await Task.Delay(300);
        }

        return urls.ToList();
    }

    private async Task ParseProductAsync(HttpClient http, string url)
    {
        var html = await http.GetStringAsync(url);

        // Find the Product JSON-LD (there may be multiple ld+json blocks — BreadcrumbList + Product)
        var ldMatches = Regex.Matches(html, @"application/ld\+json"">(.*?)</script>", RegexOptions.Singleline);
        JsonElement? productLd = null;

        foreach (Match m in ldMatches)
        {
            try
            {
                var doc = JsonDocument.Parse(m.Groups[1].Value);
                if (doc.RootElement.TryGetProperty("@type", out var typeEl) &&
                    typeEl.GetString() == "Product")
                {
                    productLd = doc.RootElement;
                    break;
                }
            }
            catch { /* skip malformed */ }
        }

        if (productLd == null) return;
        var ld = productLd.Value;

        if (!ld.TryGetProperty("sku", out var skuEl)) return;
        var article = skuEl.GetString() ?? "";
        if (string.IsNullOrEmpty(article)) return;

        var fullName  = ld.TryGetProperty("name",  out var nameEl) ? nameEl.GetString() ?? "" : "";
        var imageUrl  = ld.TryGetProperty("image", out var imgEl)  ? imgEl.GetString()  ?? "" : "";
        var brand     = ld.TryGetProperty("brand", out var brandEl) && brandEl.TryGetProperty("name", out var bnEl)
                        ? bnEl.GetString() ?? "" : "";

        decimal price = 0;
        if (ld.TryGetProperty("offers", out var offersEl) && offersEl.TryGetProperty("price", out var priceEl))
            price = priceEl.GetDecimal();

        var gender = DetectGender(fullName, url);
        var model  = StripGenderPrefix(fullName);
        var sizes  = ExtractSizes(html);

        logger.LogInformation("Товар: {Model} | {Article} | {Price}₽ | Размеров: {Count}",
            model, article, price, sizes.Count);

        var product = await productRepo.UpsertAsync(article, brand, model, gender, imageUrl);
        await offerRepo.UpsertAsync(product.Id, _storeId, price, url, sizes.Any(s => s.Available), sizes);
    }

    private static string DetectGender(string name, string url)
    {
        var t = (name + " " + url).ToLowerInvariant();
        if (t.Contains("женск") || t.Contains("woman") || t.Contains("women")) return "Женские";
        if (t.Contains("мужск") || t.Contains("/man/") || t.Contains("men"))   return "Мужские";
        if (t.Contains("детск") || t.Contains("kid"))                           return "Детские";
        return "Унисекс";
    }

    private static string StripGenderPrefix(string name)
    {
        return Regex.Replace(name,
            @"^(Женские|Мужские|Детские|Унисекс)\s+кроссовки\s+",
            "", RegexOptions.IgnoreCase).Trim();
    }

    private static List<(string Size, bool Available)> ExtractSizes(string html)
    {
        var result = new List<(string, bool)>();

        var idx = html.IndexOf("product-sizes", StringComparison.Ordinal);
        if (idx < 0) return result;

        var chunk = html[idx..Math.Min(idx + 8000, html.Length)];

        // Match size buttons: <button class="...min-w..." [disabled]>...SIZE...</button>
        var buttons = Regex.Matches(chunk,
            @"<button([^>]*min-w[^>]*)>(.*?)</button>",
            RegexOptions.Singleline);

        foreach (Match m in buttons)
        {
            var attrs = m.Groups[1].Value;
            var inner = m.Groups[2].Value;

            // Strip Nuxt SSR comments and tags to get plain size text
            var sizeText = Regex.Replace(inner, @"<!--.*?-->|<[^>]+>", "", RegexOptions.Singleline).Trim();

            if (string.IsNullOrWhiteSpace(sizeText) || !char.IsDigit(sizeText[0])) continue;

            var isDisabled = attrs.Contains("disabled");
            result.Add(($"{sizeText} EU", !isDisabled));
        }

        return result;
    }
}
