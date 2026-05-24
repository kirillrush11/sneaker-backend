using System.Text.Json;
using Microsoft.Playwright;
using SneakerAgregator.DataBase.Repositories;

namespace SneakerAgregator.Services.ParserService;

public class StreetBeatParser(
    IProductRepository productRepo,
    IOfferRepository offerRepo,
    IStoreRepository storeRepo,
    ILogger<StreetBeatParser> logger)
{
    private const string CatalogBaseUrl = "https://street-beat.ru/cat/obuv/krossovki/";
    private const int MaxPages = 30;

    private int _storeId;

    public async Task ParseAsync()
    {
        logger.LogInformation("=== Street Beat: парсинг запущен ===");

        var store = await storeRepo.GetByNameAsync("Street Beat");
        if (store == null)
        {
            logger.LogError("Магазин 'Street Beat' не найден в products.db.");
            return;
        }
        _storeId = store.Id;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = [
                "--disable-blink-features=AutomationControlled",
                "--disable-dev-shm-usage",
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-infobars",
                "--window-size=1920,1080"
            ]
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            Locale = "ru-RU",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Accept-Language"] = "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7",
                ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8"
            }
        });

        await context.AddInitScriptAsync(@"
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
            Object.defineProperty(navigator, 'languages', { get: () => ['ru-RU', 'ru', 'en-US', 'en'] });
            window.chrome = { runtime: {} };
            const originalQuery = window.navigator.permissions.query;
            window.navigator.permissions.query = (parameters) =>
                parameters.name === 'notifications'
                    ? Promise.resolve({ state: Notification.permission })
                    : originalQuery(parameters);
        ");

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        page.SetDefaultNavigationTimeout(90_000);

        var productUrls = await CollectProductUrlsAsync(page);
        logger.LogInformation("Найдено товаров: {Count}", productUrls.Count);

        if (productUrls.Count == 0)
        {
            logger.LogWarning("Товары не найдены — возможно изменилась структура сайта.");
            return;
        }

        int parsed = 0;
        foreach (var url in productUrls)
        {
            try
            {
                await ParseProductPageAsync(page, url);
                parsed++;
                await Task.Delay(Random.Shared.Next(800, 1500));
            }
            catch (Exception ex)
            {
                logger.LogWarning("Ошибка при парсинге {Url}: {Error}", url, ex.Message);
            }

            if (parsed % 10 == 0)
                logger.LogInformation("Обработано: {Count}/{Total}", parsed, productUrls.Count);
        }

        logger.LogInformation("=== Street Beat: завершён. Обработано: {Count} ===", parsed);
    }

    private async Task<List<string>> CollectProductUrlsAsync(IPage page)
    {
        var urls = new List<string>();

        for (int pageNum = 1; pageNum <= MaxPages; pageNum++)
        {
            var url = pageNum == 1
                ? CatalogBaseUrl
                : $"{CatalogBaseUrl}?page={pageNum}";

            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await Task.Delay(1500);

            var items = await page.EvaluateAsync<string[]?>(
                "() => (window.digitalData?.listing?.items ?? []).map(i => i.url).filter(Boolean)");

            if (items == null || items.Length == 0)
            {
                logger.LogInformation("Страница {Page}: нет товаров, завершаем", pageNum);
                break;
            }

            logger.LogInformation("Страница {Page}: найдено {Count} товаров", pageNum, items.Length);
            urls.AddRange(items);

            var hasNext = await page.EvaluateAsync<bool>(
                "() => !!document.querySelector('link[rel=\"next\"]')");
            if (!hasNext) break;

            await Task.Delay(500);
        }

        return urls.Distinct().ToList();
    }

    private async Task ParseProductPageAsync(IPage page, string url)
    {
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await Task.Delay(1500);

        var detail = await page.EvaluateAsync<JsonElement?>("() => window.SB_PARAMS?.detailPage ?? null");

        if (detail == null || detail.Value.ValueKind == JsonValueKind.Null)
        {
            logger.LogWarning("detailPage не найден: {Url}", url);
            return;
        }

        if (!detail.Value.TryGetProperty("product", out var productEl))
        {
            logger.LogWarning("product отсутствует в detailPage: {Url}", url);
            return;
        }

        var article = GetString(productEl, "vendorCode");
        if (string.IsNullOrEmpty(article))
        {
            logger.LogWarning("vendorCode не найден: {Url}", url);
            return;
        }

        var brand = GetString(productEl, "brand");
        var model = GetString(productEl, "title");

        var rawGender = await page.EvaluateAsync<string?>(@"() => {
            const detail = window.SB_PARAMS?.detailPage;

            const detect = t => {
                t = (t ?? '').toLowerCase();
                if (t.includes('мужск')) return 'male';
                if (t.includes('женск')) return 'female';
                if (t.includes('детск')) return 'kids';
                if (t.includes('унисекс') || t.includes('unisex')) return 'unisex';
                return null;
            };

            if (detail?.product?.sex) return detect(detail.product.sex) ?? detail.product.sex;

            for (const c of detail?.breadcrumbs ?? detail?.breadcrumb ?? []) {
                const r = detect(c.name ?? c.title ?? c.text ?? '')
                       || detect(c.url ?? c.link ?? '');
                if (r) return r;
            }

            for (const s of detail?.product?.sections ?? []) {
                const r = detect(s.name ?? s.title ?? '');
                if (r) return r;
            }

            for (const el of document.querySelectorAll(
                    'nav a, [class*=""breadcrumb""] a, [class*=""crumb""] a')) {
                const r = detect(el.textContent);
                if (r) return r;
            }

            for (const el of document.querySelectorAll(
                    'h2, h3, [class*=""category""], [class*=""section""], [class*=""label""], [class*=""tag""]')) {
                if (el.textContent.trim().length < 60) {
                    const r = detect(el.textContent);
                    if (r) return r;
                }
            }

            return detect(window.digitalData?.product?.[0]?.category ?? '');
        }") ?? "";

        if (string.IsNullOrEmpty(rawGender))
        {
            var lower = url.ToLowerInvariant();
            rawGender = lower.Contains("muzhsk") ? "male"
                      : lower.Contains("zhensk") ? "female"
                      : lower.Contains("detsk")  ? "kids"
                      : lower.Contains("unisex") ? "unisex"
                      : "";
        }

        var gender = rawGender switch {
            "male"   or "man"  or "men"                        => "Мужские",
            "female" or "woman" or "women" or "girl" or "lady" => "Женские",
            "kids"   or "children" or "child"                  => "Детские",
            _                                                   => "Унисекс"
        };

        detail.Value.TryGetProperty("price", out var priceEl);
        var price = priceEl.ValueKind == JsonValueKind.Object
            ? (priceEl.TryGetProperty("current", out var cur) ? cur.GetDecimal() : 0)
            : 0;

        var imageUrl = "";
        if (detail.Value.TryGetProperty("images", out var imagesEl) && imagesEl.GetArrayLength() > 0)
        {
            var idx = imagesEl.GetArrayLength() > 1 ? 1 : 0;
            imageUrl = GetString(imagesEl[idx], "big");
        }

        var sizes = ParseSizes(productEl);

        logger.LogInformation("Товар: {Model} | {Article} | {Price}₽ | Размеров: {Sizes}", model, article, price, sizes.Count);

        var product = await productRepo.UpsertAsync(article, brand, model, gender, imageUrl);
        await offerRepo.UpsertAsync(product.Id, _storeId, price, url, sizes.Any(s => s.Available), sizes);
    }

    private static List<(string Size, bool Available)> ParseSizes(JsonElement productEl)
    {
        var result = new List<(string, bool)>();
        if (!productEl.TryGetProperty("sizes", out var sizesEl)) return result;

        foreach (var sizeEl in sizesEl.EnumerateArray())
        {
            if (!sizeEl.TryGetProperty("grid", out var grid) || grid.ValueKind != JsonValueKind.Object)
                continue;

            var euSize = GetString(grid, "eu");
            if (string.IsNullOrEmpty(euSize)) continue;

            var unavailable = false;
            if (sizeEl.TryGetProperty("status", out var statusEl))
                foreach (var s in statusEl.EnumerateArray())
                    if (s.GetString() == "unavailable") { unavailable = true; break; }

            result.Add(($"{euSize} EU", !unavailable));
        }

        return result;
    }

    private static string GetString(JsonElement el, string key) =>
        el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";
}
