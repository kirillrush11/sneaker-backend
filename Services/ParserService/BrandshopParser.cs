using System.Text.RegularExpressions;
using Microsoft.Playwright;
using SneakerAgregator.DataBase.Repositories;

namespace SneakerAgregator.Services.ParserService;

public class BrandshopParser(
    IProductRepository productRepo,
    IOfferRepository offerRepo,
    IStoreRepository storeRepo,
    ILogger<BrandshopParser> logger)
{
    private const string CatalogBaseUrl = "https://brandshop.ru/new/obuv/krossovki/";
    private const int MaxPages = 20;

    private int _storeId;

    public async Task ParseAsync()
    {
        logger.LogInformation("=== Brandshop: парсинг запущен ===");

        var store = await storeRepo.GetByNameAsync("Brandshop");
        if (store == null)
        {
            logger.LogError("Магазин 'Brandshop' не найден в products.db.");
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

        logger.LogInformation("=== Brandshop: завершён. Обработано: {Count} ===", parsed);
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
            await Task.Delay(2000);

            var cards = await page.QuerySelectorAllAsync("a.product-card__link");
            if (cards.Count == 0)
            {
                logger.LogInformation("Страница {Page}: нет карточек, завершаем", pageNum);
                break;
            }

            logger.LogInformation("Страница {Page}: найдено {Count} карточек", pageNum, cards.Count);

            foreach (var card in cards)
            {
                var href = await card.GetAttributeAsync("href");
                if (string.IsNullOrEmpty(href)) continue;
                urls.Add(href.StartsWith("http") ? href : $"https://brandshop.ru{href}");
            }

            var hasNext = await page.EvaluateAsync<bool>(
                "() => !!document.querySelector('li.pagination__item_arrow:not(.disabled) a[href*=\"page=\"]')");
            if (!hasNext)
            {
                logger.LogInformation("Нет следующей страницы, завершаем на странице {Page}", pageNum);
                break;
            }

            await Task.Delay(500);
        }

        return urls.Distinct().ToList();
    }

    private async Task ParseProductPageAsync(IPage page, string url)
    {
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await Task.Delay(2000);

        var article = await page.EvaluateAsync<string?>(@"() => {
            for (const item of document.querySelectorAll('.product-data__item')) {
                const type = item.querySelector('.product-data__type');
                if (type?.textContent?.trim() === 'Артикул') {
                    return item.querySelector('.product-data__name')?.textContent?.trim() ?? null;
                }
            }
            return null;
        }");

        if (string.IsNullOrEmpty(article))
        {
            logger.LogWarning("Артикул не найден: {Url}", url);
            return;
        }

        var brand = await page.EvaluateAsync<string?>(
            "() => document.querySelector('.product-page__header span')?.textContent?.trim() ?? null") ?? "";

        var modelSuffix = await page.EvaluateAsync<string?>(@"() => {
            const els = document.querySelectorAll('.product-page__subheader');
            return els[els.length - 1]?.textContent?.trim() ?? null;
        }") ?? "";

        var genderMatch = Regex.Match(modelSuffix,
            @"^(Мужские|Мужской|Женские|Женский|Детские|Унисекс)",
            RegexOptions.IgnoreCase);
        var gender = genderMatch.Success ? NormalizeGender(genderMatch.Value) : "Унисекс";

        modelSuffix = Regex.Replace(modelSuffix,
            @"^(Мужские|Женские|Детские|Унисекс|Мужской|Женский)\s+\S+\s*",
            "", RegexOptions.IgnoreCase).Trim();

        var fullModel = $"{brand} {modelSuffix}".Trim();

        var priceStr = await page.EvaluateAsync<string?>(
            "() => document.querySelector('meta[itemprop=\"price\"]')?.content ?? null");
        decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var price);

        var imageUrl = await page.EvaluateAsync<string?>(
            "() => document.querySelector('meta[property=\"og:image\"]')?.content ?? null") ?? "";

        var sizes = await ParseSizesAsync(page);

        logger.LogInformation("Товар: {Model} | {Article} | {Price}₽ | Размеров: {Sizes}", fullModel, article, price, sizes.Count);

        var product = await productRepo.UpsertAsync(article, brand, fullModel, gender, imageUrl);
        await offerRepo.UpsertAsync(product.Id, _storeId, price, url, sizes.Any(s => s.Available), sizes);
    }

    private static async Task<List<(string Size, bool Available)>> ParseSizesAsync(IPage page)
    {
        var raw = await page.EvaluateAsync<string[][]?>(@"() => {
            const result = [];
            for (const item of document.querySelectorAll('.product-plate__item')) {
                const clone = item.cloneNode(true);
                clone.querySelector?.('.tooltip__wrapper')?.remove();
                const sizeText = clone.textContent?.trim();
                if (!sizeText) continue;
                const tooltip = item.querySelector('.tooltip');
                const available = (tooltip?.textContent ?? '').includes('В наличии') ? 'true' : 'false';
                result.push([sizeText, available]);
            }
            return result;
        }") ?? [];

        return raw.Select(r => (r[0], r[1] == "true")).ToList();
    }

    private static string NormalizeGender(string raw) =>
        raw.ToLowerInvariant() switch {
            var s when s.StartsWith("муж") => "Мужские",
            var s when s.StartsWith("жен") => "Женские",
            var s when s.StartsWith("дет") => "Детские",
            _ => "Унисекс"
        };
}
