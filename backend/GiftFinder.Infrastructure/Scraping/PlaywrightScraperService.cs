using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using GiftFinder.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GiftFinder.Infrastructure.Scraping;

/// <summary>
/// Dịch vụ cào Tên thật, Giá thật và Ảnh gốc của Shopee & TikTok Shop.
/// Kết hợp:
/// 1. Tối ưu siêu tốc cho Shopee: Trích xuất trực tiếp schema.org/Product JSON-LD (phản hồi ~300ms, không tốn RAM).
/// 2. Hỗ trợ Playwright ConnectOverCDP: Kết nối trực tiếp vào Microsoft Edge thật của người dùng (Port 9222)
///    để vượt qua 100% Shopee Shield (WAF 90309999) và TikTok Captcha.
/// 3. Fallback an toàn về Playwright Headless Browser ngầm độc lập khi Edge không bật.
/// </summary>
public class PlaywrightScraperService : IWebScraperService, IAsyncDisposable
{
    private readonly ILogger<PlaywrightScraperService> _logger;
    private readonly IConfiguration? _configuration;
    private readonly HttpClient _httpClient;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _isConnectedOverCdp;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _concurrencyLock = new(3, 3);
    private bool _disposed;

    public PlaywrightScraperService(ILogger<PlaywrightScraperService> logger, IConfiguration? configuration = null)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.Timeout = TimeSpan.FromSeconds(7);
    }

    public bool SupportsUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var lower = url.ToLowerInvariant();
        return lower.Contains("shopee.vn") || 
               lower.Contains("s.shopee.vn") || 
               lower.Contains("tiktok.com") || 
               lower.Contains("vt.tiktok.com") ||
               lower.Contains("lazada.vn");
    }

    public async Task<ScrapedProductData> ScrapeProductAsync(string url, CancellationToken cancellationToken = default)
    {
        var result = new ScrapedProductData();
        if (string.IsNullOrWhiteSpace(url))
        {
            result.ErrorMessage = "URL không hợp lệ.";
            return result;
        }

        var lowerUrl = url.ToLowerInvariant();

        // 1. CHIẾN LƯỢC NHANH QUA SCHEMA JSON-LD CHO SHOPEE & LAZADA
        if (lowerUrl.Contains("shopee.vn") || lowerUrl.Contains("s.shopee.vn") || lowerUrl.Contains("lazada.vn"))
        {
            try
            {
                var fastResult = await TryScrapeFastAsync(url, cancellationToken);
                if (fastResult.IsSuccess && fastResult.Price > 0)
                {
                    _logger.LogInformation("Cào thành công qua Fast JSON-LD: Tên='{Title}', Giá={Price:N0}đ", 
                        fastResult.Title, fastResult.Price);
                    return fastResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Fast JSON-LD scrape không thành công, chuyển sang Playwright: {Msg}", ex.Message);
            }
        }

        // 2. CHIẾN LƯỢC PLAYWRIGHT CHO TIKTOK SHOP, LAZADA VÀ CÁC TRƯỜNG HỢP CÒN LẠI
        await _concurrencyLock.WaitAsync(cancellationToken);
        IBrowserContext? context = null;
        IPage? page = null;
        bool isCreatedContext = false;

        try
        {
            await EnsureBrowserInitializedAsync(cancellationToken);
            if (_browser == null)
            {
                result.ErrorMessage = "Không thể khởi động trình duyệt Playwright.";
                return result;
            }

            if (_isConnectedOverCdp)
            {
                // Khi kết nối qua CDP: Dùng Context của Edge thật (đã có cookies, sensor, session người dùng)
                context = _browser.Contexts.FirstOrDefault();
                if (context == null)
                {
                    context = await _browser.NewContextAsync();
                    isCreatedContext = true;
                }
                page = await context.NewPageAsync();
            }
            else
            {
                // Tạo Context với Stealth Mode và nạp phiên đăng nhập Shopee nếu có cho Chromium headless
                var contextOptions = new BrowserNewContextOptions
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                    ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
                    Locale = "vi-VN",
                    TimezoneId = "Asia/Ho_Chi_Minh"
                };

                var statePath = Path.Combine(Directory.GetCurrentDirectory(), "shopee_state.json");
                if (!File.Exists(statePath))
                {
                    statePath = Path.Combine(AppContext.BaseDirectory, "shopee_state.json");
                }
                if (File.Exists(statePath))
                {
                    contextOptions.StorageStatePath = statePath;
                }

                context = await _browser.NewContextAsync(contextOptions);
                isCreatedContext = true;

                // Ẩn hoàn toàn cờ tự động hóa (webdriver)
                await context.AddInitScriptAsync(@"
                    Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                    window.chrome = { runtime: {} };
                ");

                page = await context.NewPageAsync();

                // Route Aborting: Chặn tải hình ảnh, font, media để tiết kiệm 80% RAM và tăng tốc tối đa
                await page.RouteAsync("**/*", async route =>
                {
                    var resourceType = route.Request.ResourceType;
                    var reqUrl = route.Request.Url;

                    if (resourceType == "image" || 
                        resourceType == "media" || 
                        resourceType == "font" ||
                        reqUrl.Contains("google-analytics") || 
                        reqUrl.Contains("facebook.net") || 
                        reqUrl.Contains("analytics") || 
                        reqUrl.Contains("tracker"))
                    {
                        await route.AbortAsync();
                    }
                    else
                    {
                        await route.ContinueAsync();
                    }
                });
            }

            if (lowerUrl.Contains("tiktok.com") || lowerUrl.Contains("vt.tiktok.com"))
            {
                result = await ScrapeTikTokShopAsync(page, url, cancellationToken);
            }
            else if (lowerUrl.Contains("lazada.vn"))
            {
                result = await ScrapeLazadaAsync(page, url, cancellationToken);
            }
            else
            {
                result = await ScrapeShopeeBrowserAsync(page, url, cancellationToken);
            }

            result.FinalUrl = page.Url;
            if (result.IsSuccess && result.Price > 0)
            {
                _logger.LogInformation("Playwright cào dữ liệu thành công từ {Url}: Tên='{Title}', Giá={Price:N0}đ", 
                    url, result.Title, result.Price);
            }
            else
            {
                _logger.LogWarning("Playwright không lấy được giá từ {Url} (Trang bị chặn hoặc cần Captcha). Tên='{Title}', Giá={Price:N0}đ", 
                    url, result.Title, result.Price);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi hoặc timeout khi cào URL {Url} bằng Playwright: {Message}", url, ex.Message);
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            if (page != null)
            {
                try
                {
                    // Nếu là Edge CDP và chỉ còn 1 tab duy nhất, không đóng tab vì sẽ tắt luôn Edge!
                    if (_isConnectedOverCdp && context != null && context.Pages.Count <= 1)
                    {
                        await page.GotoAsync("about:blank", new PageGotoOptions { Timeout = 3000 }).WaitAsync(TimeSpan.FromSeconds(4));
                    }
                    else
                    {
                        await page.CloseAsync(new PageCloseOptions { RunBeforeUnload = false }).WaitAsync(TimeSpan.FromSeconds(4));
                    }
                }
                catch { }
            }
            if (context != null && isCreatedContext && !_isConnectedOverCdp)
            {
                try { await context.CloseAsync().WaitAsync(TimeSpan.FromSeconds(4)); } catch { }
            }

            // Nghỉ sau khi đóng tab để sàn không kích hoạt rate limit (traffic error)
            try
            {
                var delayMs = lowerUrl.Contains("tiktok")
                    ? Random.Shared.Next(3000, 4200)
                    : Random.Shared.Next(4000, 5500);
                await Task.Delay(delayMs, cancellationToken);
            }
            catch { }

            _concurrencyLock.Release();
        }

        return result;
    }

    private async Task<ScrapedProductData> TryScrapeFastAsync(string url, CancellationToken cancellationToken)
    {
        var result = new ScrapedProductData();
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return result;

        result.FinalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(html)) return result;

        var matches = Regex.Matches(html, @"<script[^>]*>(.*?schema\.org.*?)</script>", RegexOptions.Singleline);
        foreach (Match m in matches)
        {
            var text = m.Groups[1].Value.Trim();
            if (text.Contains("\"@type\":\"Product\"") || text.Contains("\"Product\""))
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.TryGetProperty("name", out var nameP))
                {
                    result.Title = CleanTitle(nameP.GetString() ?? "");
                }
                if (root.TryGetProperty("image", out var imgP))
                {
                    if (imgP.ValueKind == JsonValueKind.Array)
                    {
                        var first = imgP.EnumerateArray().FirstOrDefault();
                        result.ImageUrl = first.GetString();
                    }
                    else
                    {
                        result.ImageUrl = imgP.GetString();
                    }
                }
                if (root.TryGetProperty("description", out var descP))
                {
                    result.Description = descP.GetString();
                }
                if (root.TryGetProperty("productID", out var idP))
                {
                    result.ExternalId = idP.GetString();
                }
                if (root.TryGetProperty("offers", out var offers))
                {
                    JsonElement priceEl = default;
                    bool hasPrice = false;

                    if (offers.ValueKind == JsonValueKind.Array)
                    {
                        var firstOffer = offers.EnumerateArray().FirstOrDefault();
                        if (firstOffer.TryGetProperty("price", out var pArrayEl))
                        {
                            priceEl = pArrayEl;
                            hasPrice = true;
                        }
                    }
                    else if (offers.TryGetProperty("price", out var pObjEl))
                    {
                        priceEl = pObjEl;
                        hasPrice = true;
                    }

                    if (hasPrice)
                    {
                        var rawP = priceEl.GetString() ?? priceEl.GetRawText();
                        var num = Regex.Replace(rawP, @"[^\d]", "");
                        if (long.TryParse(num, out var p))
                        {
                            if (rawP.Contains(".00") && num.EndsWith("00")) p /= 100;
                            result.Price = p;
                            result.OriginalPrice = (long)(p * 1.25);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(result.Title) && result.Price > 0)
                {
                    result.IsSuccess = true;
                    return result;
                }
            }
        }

        // Bổ sung đọc OpenGraph nếu trang HTML tĩnh có đủ og:title, og:image và giá
        var ogTitleMatch = Regex.Match(html, @"<meta\s+[^>]*property=[""']og:title[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        var ogImageMatch = Regex.Match(html, @"<meta\s+[^>]*property=[""']og:image[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        var ogPriceMatch = Regex.Match(html, @"property=[""']product:price:amount[""'][^>]*content=[""'](\d+)", RegexOptions.IgnoreCase);
        if (!ogPriceMatch.Success)
            ogPriceMatch = Regex.Match(html, @"[""']sale_price[""']\s*:\s*[""']?(\d+)", RegexOptions.IgnoreCase);

        if (ogTitleMatch.Success && ogPriceMatch.Success && long.TryParse(ogPriceMatch.Groups[1].Value, out var ogP) && ogP > 1000)
        {
            result.Title = CleanTitle(WebUtility.HtmlDecode(ogTitleMatch.Groups[1].Value));
            if (ogImageMatch.Success) result.ImageUrl = WebUtility.HtmlDecode(ogImageMatch.Groups[1].Value);
            result.Price = ogP;
            result.OriginalPrice = (long)(ogP * 1.25);
            result.IsSuccess = true;
            return result;
        }

        return result;
    }

    private async Task<ScrapedProductData> ScrapeLazadaAsync(IPage page, string url, CancellationToken cancellationToken)
    {
        var result = new ScrapedProductData();
        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 25000
        });
        
        try
        {
            await page.WaitForSelectorAsync(".pdp-price, .pdp-price_color_orange, [class*='pdp-price'], span[class*='price']", new PageWaitForSelectorOptions
            {
                Timeout = 5000
            });
        }
        catch { }

        // Kiểm tra xem có bị Captcha / Verify của Lazada không
        var lazTitle = "";
        try { lazTitle = await page.TitleAsync(); } catch { }
        bool isLazCaptcha = page.Url.Contains("punish") ||
                            page.Url.Contains("captcha") ||
                            lazTitle.Contains("Security", StringComparison.OrdinalIgnoreCase) ||
                            lazTitle.Contains("Verification", StringComparison.OrdinalIgnoreCase);

        if (isLazCaptcha && _isConnectedOverCdp)
        {
            if (OperatingSystem.IsWindows())
            {
                try { Console.Beep(1000, 500); } catch { }
            }
            Console.WriteLine("\n=========================================================================================");
            Console.WriteLine($"🔔 [CẦN XÁC MINH CAPTCHA LAZADA: \"{lazTitle}\"]");
            Console.WriteLine("👉 Vui lòng nhìn vào cửa sổ Microsoft Edge và KÉO XÁC MINH TRÊN MÀN HÌNH!");
            Console.WriteLine("⏳ Hệ thống đang TẠM DỪNG chờ bạn hoàn tất xác minh (tối đa 90 giây)...");
            Console.WriteLine("=========================================================================================\n");

            var startWait = DateTime.UtcNow;
            while ((DateTime.UtcNow - startWait).TotalSeconds < 90 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1500, cancellationToken);
                try
                {
                    var curTitle = await page.TitleAsync();
                    if (!page.Url.Contains("punish") && !curTitle.Contains("Verification"))
                    {
                        var pEl = await page.WaitForSelectorAsync(".pdp-price, [class*='pdp-price']", new PageWaitForSelectorOptions { Timeout = 5000 });
                        if (pEl != null)
                        {
                            Console.WriteLine("🎉 [XÁC MINH THÀNH CÔNG] Đã vượt qua Captcha Lazada! Tiếp tục lấy dữ liệu...");
                            break;
                        }
                    }
                }
                catch { }
            }
        }

        var jsonStr = await page.EvaluateAsync<string>(@"() => {
            const titleEl = document.querySelector('meta[property=""og:title""]') || document.querySelector('h1.pdp-mod-product-badge-title') || document.querySelector('h1') || document.querySelector('title');
            let title = titleEl ? (titleEl.content || titleEl.innerText || '') : '';
            title = title.replace(/\s*\|\s*Lazada.*$/i, '').trim();

            const imgEl = document.querySelector('meta[property=""og:image""]') || document.querySelector('img.pdp-mod-common-image') || document.querySelector('.gallery-preview-panel__image');
            const img = imgEl ? (imgEl.content || imgEl.src || '') : '';

            let priceText = '';
            const priceEl = document.querySelector('.pdp-price_color_orange') ||
                            document.querySelector('.pdp-price') || 
                            document.querySelector('.pdp-vsl-price') || 
                            document.querySelector('[class*=""pdp-price""]') ||
                            document.querySelector('span[class*=""price""]');
            if (priceEl) priceText = priceEl.innerText || '';

            let origPriceText = '';
            const origEl = document.querySelector('.pdp-price_type_deleted') || 
                           document.querySelector('[class*=""origin-price""]') ||
                           document.querySelector('span[class*=""deleted""]');
            if (origEl) origPriceText = origEl.innerText || '';

            const matches = [];
            if (!priceText) {
                const elements = document.querySelectorAll('span, div, p');
                for (const el of elements) {
                    if (el.children.length === 0) {
                        const txt = (el.innerText || '').trim();
                        if (/\d{1,3}(\.\d{3})+/.test(txt)) {
                            const isIgnored = el.closest('[class*=""voucher""], [class*=""shipping""], [class*=""delivery""], [class*=""badge""], [class*=""popover""]');
                            if (!isIgnored && !txt.includes('đánh giá') && !txt.includes('bán') && !txt.includes('Phí') && !txt.includes('VND')) {
                                matches.push(txt);
                            }
                        }
                    }
                }
            }

            return JSON.stringify({
                title: title,
                imageUrl: img,
                price: priceText,
                origPrice: origPriceText,
                matches: matches
            });
        }");

        if (!string.IsNullOrWhiteSpace(jsonStr))
        {
            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;
            result.Title = CleanTitle(root.GetProperty("title").GetString() ?? "");
            result.ImageUrl = root.GetProperty("imageUrl").GetString();

            var rawP = root.GetProperty("price").GetString() ?? "";
            var numP = Regex.Replace(rawP, @"[^\d]", "");
            if (long.TryParse(numP, out var p) && p >= 1000 && p < 100_000_000)
            {
                result.Price = p;
            }
            else if (root.TryGetProperty("matches", out var mArr) && mArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in mArr.EnumerateArray())
                {
                    var rawM = item.GetString() ?? "";
                    var numM = Regex.Replace(rawM, @"[^\d]", "");
                    if (long.TryParse(numM, out var mp) && mp >= 10000 && mp < 100_000_000)
                    {
                        if (result.Price == 0 || mp < result.Price)
                        {
                            result.Price = mp;
                        }
                    }
                }
            }

            var rawOp = root.GetProperty("origPrice").GetString() ?? "";
            var numOp = Regex.Replace(rawOp, @"[^\d]", "");
            if (long.TryParse(numOp, out var op) && op > 1000)
            {
                result.OriginalPrice = op;
            }
            else if (result.Price > 0)
            {
                result.OriginalPrice = (long)(result.Price * 1.25);
            }
        }

        result.IsSuccess = !string.IsNullOrWhiteSpace(result.Title) && result.Price > 0;
        return result;
    }

    private async Task<ScrapedProductData> ScrapeTikTokShopAsync(IPage page, string url, CancellationToken cancellationToken)
    {
        var result = new ScrapedProductData();

        // 1. Mở trang TikTok Shop (tự động theo dõi chuyển hướng link rút gọn vt.tiktok.com)
        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 15000
        });

        // Đợi linh hoạt khi phần tử giá xuất hiện (trả về ngay khi React mount giá xong, không phải chờ cứng 2.5s)
        try
        {
            await page.WaitForSelectorAsync("span[style*='font-size:36px'], span[style*='font-size: 36px'], div[class*='items-baseline'], [class*='line-through']", new PageWaitForSelectorOptions
            {
                Timeout = 3000
            });
        }
        catch
        {
            // Fallback ngắn nếu trang tải chậm hoặc gặp layout biến thể
            await page.WaitForTimeoutAsync(800);
        }

        // Kiểm tra xem có bị Security Check / Captcha không
        var checkTitle = "";
        try { checkTitle = await page.TitleAsync(); } catch { }
        bool isSecurityCheck = page.Url.Contains("verify") ||
                               page.Url.Contains("security") ||
                               checkTitle.Contains("Security Check", StringComparison.OrdinalIgnoreCase) ||
                               checkTitle.Contains("Verify", StringComparison.OrdinalIgnoreCase);

        if (isSecurityCheck && _isConnectedOverCdp)
        {
            if (OperatingSystem.IsWindows())
            {
                try { Console.Beep(1000, 500); } catch { }
            }
            Console.WriteLine("\n=========================================================================================");
            Console.WriteLine($"🔔 [CẦN XÁC MINH SECURITY CHECK TIKTOK SHOP]");
            Console.WriteLine("👉 Vui lòng nhìn vào cửa sổ Microsoft Edge và KÉO XÁC MINH TRÊN MÀN HÌNH!");
            Console.WriteLine("⏳ Hệ thống đang TẠM DỪNG chờ bạn hoàn tất xác minh (tối đa 90 giây)...");
            Console.WriteLine("=========================================================================================\n");

            var startWait = DateTime.UtcNow;
            while ((DateTime.UtcNow - startWait).TotalSeconds < 90 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1500, cancellationToken);
                try
                {
                    var curTitle = await page.TitleAsync();
                    if (!curTitle.Contains("Security Check", StringComparison.OrdinalIgnoreCase) && 
                        !curTitle.Contains("Verify", StringComparison.OrdinalIgnoreCase) &&
                        !page.Url.Contains("verify") && 
                        !page.Url.Contains("security"))
                    {
                        Console.WriteLine("🎉 [XÁC MINH THÀNH CÔNG] Đã vượt qua Security Check trên Edge! Tiếp tục lấy dữ liệu...");
                        try
                        {
                            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 10000 });
                        }
                        catch { }
                        await page.WaitForTimeoutAsync(2500);
                        break;
                    }
                }
                catch { }
            }
        }

        // 2. Trích xuất dữ liệu giá và thông tin sản phẩm từ DOM
        string? jsonStr = null;
        for (int retryEval = 0; retryEval < 3; retryEval++)
        {
            try
            {
                jsonStr = await page.EvaluateAsync<string>(@"() => {
                    const titleEl = document.querySelector('meta[property=""og:title""]') || document.querySelector('title');
                    let title = titleEl ? (titleEl.content || titleEl.innerText || '') : '';
                    title = title.replace(/\s*-\s*TikTok.*$/i, '').trim();

                    const imgEl = document.querySelector('meta[property=""og:image""]');
                    const img = imgEl ? imgEl.content : '';

                    let salePriceText = '';
                    
                    // 1. Ưu tiên tìm trong container giá chính (nơi chứa line-through hoặc thẻ giá to cạnh ₫)
                    const origEl = document.querySelector('[class*=""line-through""]');
                    if (origEl) {
                        const row = origEl.closest('div[class*=""items-baseline""]') || origEl.parentElement;
                        if (row) {
                            const spans = row.querySelectorAll('span');
                            for (const s of spans) {
                                const t = (s.innerText || '').trim();
                                if (s !== origEl && /\d{1,3}(\.\d{3})+/.test(t) && !s.className.includes('line-through') && !t.includes('%')) {
                                    salePriceText = t;
                                    break;
                                }
                            }
                        }
                    }

                    // 2. Thẻ giá bán chính hiển thị cỡ lớn (font-size:36px hoặc Headline-Semibold cạnh ký hiệu ₫)
                    if (!salePriceText) {
                        const largePrice = document.querySelector('span[style*=""font-size:36px""], span[style*=""font-size: 36px""], span[style*=""font-size:32px""], span[style*=""font-size: 32px""]') ||
                                           document.querySelector('div[class*=""items-baseline""] span[class*=""Headline-Semibold""]');
                        if (largePrice) {
                            salePriceText = (largePrice.innerText || '').trim();
                        }
                    }

                    // 3. Fallback: Quét các thẻ số, loại trừ triệt để phí ship (shipping/phí), voucher, review, v.v.
                    if (!salePriceText) {
                        const elements = document.querySelectorAll('span, div, p');
                        for (const el of elements) {
                            if (el.children.length === 0) {
                                const txt = (el.innerText || '').trim();
                                if (/\d{1,3}(\.\d{3})+/.test(txt)) {
                                    const ignored = el.closest('[class*=""shipping""], [class*=""voucher""], [class*=""recommend""], [class*=""delivery""], [class*=""review""], [class*=""badge""], [class*=""popover""]');
                                    const lower = txt.toLowerCase();
                                    if (!ignored && 
                                        !lower.includes('shipping') && 
                                        !lower.includes('phí') && 
                                        !lower.includes('đánh giá') && 
                                        !lower.includes('bán') && 
                                        !lower.includes('vnd') && 
                                        !lower.includes('đơn') && 
                                        !lower.includes('giảm') && 
                                        !txt.includes('%')) {
                                        salePriceText = txt;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    return JSON.stringify({
                        title: title,
                        imageUrl: img,
                        salePriceText: salePriceText
                    });
                }");
                break;
            }
            catch (Exception ex) when (ex.Message.Contains("Execution context was destroyed") || ex.Message.Contains("navigation"))
            {
                if (retryEval == 2) throw;
                await Task.Delay(2500, cancellationToken);
                try { await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 5000 }); } catch { }
            }
        }

        if (!string.IsNullOrWhiteSpace(jsonStr))
        {
            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;
            result.Title = CleanTitle(root.GetProperty("title").GetString() ?? "");
            result.ImageUrl = root.GetProperty("imageUrl").GetString();

            var rawPrice = root.GetProperty("salePriceText").GetString() ?? "";
            var numPrice = Regex.Replace(rawPrice, @"[^\d]", "");
            if (long.TryParse(numPrice, out var sp) && sp >= 1000 && sp < 100_000_000)
            {
                result.Price = sp;
            }

            // Người dùng yêu cầu: Chỉ lấy giá bán hiển thị trực tiếp trên màn hình, không lấy giá gốc ảo bị gạch ngang quá cao
            result.OriginalPrice = null;
        }

        if (result.Title.Contains("Security Check", StringComparison.OrdinalIgnoreCase) || result.Title.Contains("Verify", StringComparison.OrdinalIgnoreCase))
        {
            result.IsSuccess = false;
            result.ErrorMessage = "TikTok Shop yêu cầu Security Check / Captcha.";
            return result;
        }

        result.IsSuccess = !string.IsNullOrWhiteSpace(result.Title) && result.Price > 0;
        return result;
    }

    private async Task<ScrapedProductData> ScrapeShopeeBrowserAsync(IPage page, string url, CancellationToken cancellationToken)
    {
        var result = new ScrapedProductData();
        var targetUrl = url;
        var idMatch = Regex.Match(url, @"(?:-i\.|/product/|/[^/]+/)(?<shopId>\d+)[/.](?<itemId>\d+)");
        if (idMatch.Success && url.Contains("__mobile__"))
        {
            targetUrl = $"https://shopee.vn/product/{idMatch.Groups["shopId"].Value}/{idMatch.Groups["itemId"].Value}";
        }

        await page.GotoAsync(targetUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 25000
        });

        // Đợi phần tử giá xuất hiện (trả về ngay khi React mount giá xong, khoảng 2.7s)
        try
        {
            await page.WaitForSelectorAsync(".pyzxvq, .pw3J3G, .PcnXj1, .pmmxKx, .G27ShZ, div[class*='items-center'] > div[class*='text-']", new PageWaitForSelectorOptions
            {
                Timeout = 7500
            });
        }
        catch { }

        // Kiểm tra xem có gặp Captcha / Trang xác minh của Shopee không
        var pageTitle = "";
        try { pageTitle = await page.TitleAsync(); } catch { }

        bool isCaptcha = page.Url.Contains("/verify") ||
                         page.Url.Contains("captcha") ||
                         pageTitle.Contains("Verify", StringComparison.OrdinalIgnoreCase) ||
                         pageTitle.Contains("Xác minh", StringComparison.OrdinalIgnoreCase) ||
                         pageTitle.Contains("Robot", StringComparison.OrdinalIgnoreCase) ||
                         pageTitle.Contains("Please Try Again", StringComparison.OrdinalIgnoreCase);

        if (!isCaptcha)
        {
            try
            {
                var captchaEl = await page.QuerySelectorAsync("iframe[src*='captcha'], div[class*='captcha'], div[class*='verify']");
                if (captchaEl != null) isCaptcha = true;
            }
            catch { }
        }

        if (isCaptcha && _isConnectedOverCdp)
        {
            if (OperatingSystem.IsWindows())
            {
                try { Console.Beep(1000, 500); } catch { }
            }
            Console.WriteLine("\n=========================================================================================");
            Console.WriteLine($"🔔 [CẦN XÁC MINH CAPTCHA SHOPEE: \"{pageTitle}\"]");
            Console.WriteLine("👉 Vui lòng nhìn vào cửa sổ Microsoft Edge và KÉO MẢNH GHÉP / XÁC MINH TRÊN MÀN HÌNH!");
            Console.WriteLine("⏳ Hệ thống đang TẠM DỪNG chờ bạn hoàn tất xác minh (tối đa 90 giây)...");
            Console.WriteLine("=========================================================================================\n");

            _logger.LogWarning("🔔 [CẦN XÁC MINH CAPTCHA TRÊN EDGE] Shopee yêu cầu xác minh ('{Title}')! Đang tạm dừng chờ bạn thao tác trên Edge tối đa 90 giây...", pageTitle);

            var startWait = DateTime.UtcNow;
            while ((DateTime.UtcNow - startWait).TotalSeconds < 90 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1500, cancellationToken);

                try
                {
                    var currentTitle = await page.TitleAsync();
                    var currentUrl = page.Url;
                    if (!currentUrl.Contains("/verify") && 
                        !currentUrl.Contains("captcha") && 
                        !currentTitle.Contains("Verify", StringComparison.OrdinalIgnoreCase) &&
                        !currentTitle.Contains("Xác minh", StringComparison.OrdinalIgnoreCase) &&
                        !currentTitle.Contains("Robot", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("🎉 [XÁC MINH THÀNH CÔNG] Đã phát hiện trang Shopee vượt qua Captcha! Tiếp tục cào dữ liệu...");
                        _logger.LogInformation("🎉 [XÁC MINH THÀNH CÔNG] Đã vượt qua Captcha trên Edge! Tiếp tục bóc tách dữ liệu...");
                        await page.WaitForTimeoutAsync(2000);
                        break;
                    }
                }
                catch { }
            }
        }

        if (page.Url.Contains("verify/traffic/error"))
        {
            result.IsSuccess = false;
            result.ErrorMessage = "Shopee Shield chặn truy cập (Traffic Error).";
            return result;
        }

        var jsonStr = await page.EvaluateAsync<string?>(@"() => {
            const h1 = document.querySelector('h1') || document.querySelector('div[class*=""product-briefing""] span') || document.querySelector('meta[property=""og:title""]');
            let title = h1 ? (h1.content || h1.innerText || '').trim() : '';

            const imgEl = document.querySelector('meta[property=""og:image""]') || 
                          document.querySelector('img[src*=""susercontent.com""]') ||
                          document.querySelector('div[class*=""product-briefing""] img');
            const img = imgEl ? (imgEl.content || imgEl.src || '') : '';

            let priceText = '';
            // Các class giá hiển thị chính thức của Shopee
            const priceEl = document.querySelector('.pyzxvq') || 
                            document.querySelector('.pw3J3G') || 
                            document.querySelector('.PcnXj1') || 
                            document.querySelector('.pmmxKx') || 
                            document.querySelector('.G27ShZ') || 
                            document.querySelector('div[class*=""items-center""] > div[class*=""text-""]');
            if (priceEl) {
                const t = (priceEl.innerText || '').trim();
                if (/\d/.test(t)) {
                    priceText = t;
                }
            }

            const matches = [];
            if (!priceText) {
                const elements = document.querySelectorAll('div, span');
                for (const el of elements) {
                    if (el.children.length === 0) {
                        const txt = (el.innerText || '').trim();
                        if (/\d{1,3}(\.\d{3})+/.test(txt)) {
                            const isIgnored = el.closest('[class*=""voucher""], [class*=""shipping""], [class*=""delivery""], [class*=""badge""], [class*=""popover""], .BudQxb, .EBSK_9');
                            if (!isIgnored && !txt.includes('đánh giá') && !txt.includes('bán') && !txt.includes('Phí') && !txt.includes('VND')) {
                                matches.push(txt);
                            }
                        }
                    }
                }
            }

            const isNotExists = document.body ? (document.body.innerText.includes('Sản phẩm này không tồn tại') || document.body.innerText.includes('Sản phẩm không tồn tại')) : false;
            return JSON.stringify({ title, img, priceText, matches, isNotExists, pageTitle: document.title });
        }");

        _logger.LogInformation("Shopee Extracted JSON: {Json}", jsonStr);

        if (!string.IsNullOrWhiteSpace(jsonStr))
        {
            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;
            if (root.TryGetProperty("isNotExists", out var notExistsProp) && notExistsProp.GetBoolean())
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Sản phẩm không tồn tại hoặc đã bị người bán xóa trên Shopee.";
                return result;
            }

            var t = root.GetProperty("title").GetString() ?? "";
            if (t.Contains("Shopee Việt Nam | Mua và Bán") || string.IsNullOrWhiteSpace(t))
            {
                var parsedMeta = Affiliate.ShopeeAffiliateHandler.ParseShopeeMetadata(url);
                result.Title = parsedMeta.Title;
            }
            else
            {
                result.Title = CleanTitle(t);
            }

            result.ImageUrl = root.GetProperty("img").GetString();

            long foundPrice = 0;
            var pStr = root.GetProperty("priceText").GetString() ?? "";
            // Nếu Shopee hiển thị khoảng giá biến thể (VD: "229.000₫ - 1.000.000₫"), lấy giá nhỏ nhất ở vế đầu
            if (pStr.Contains("-"))
            {
                pStr = pStr.Split('-')[0];
            }
            var pNum = Regex.Replace(pStr, @"[^\d]", "");
            if (long.TryParse(pNum, out var p) && p >= 1000 && p < 100_000_000)
            {
                foundPrice = p;
            }
            else if (root.TryGetProperty("matches", out var matchesArr) && matchesArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in matchesArr.EnumerateArray())
                {
                    var rawMatch = item.GetString() ?? "";
                    if (rawMatch.Contains("-"))
                    {
                        rawMatch = rawMatch.Split('-')[0];
                    }
                    var matchNum = Regex.Replace(rawMatch, @"[^\d]", "");
                    // Bỏ qua giá nhỏ hơn 25.000đ để tránh ăn phải voucher / phí ship 15k
                    if (long.TryParse(matchNum, out var sp) && sp >= 25000 && sp < 100_000_000)
                    {
                        if (foundPrice == 0 || sp < foundPrice)
                        {
                            foundPrice = sp;
                        }
                    }
                }
            }

            if (foundPrice > 0)
            {
                result.Price = foundPrice;
                result.OriginalPrice = (long)(foundPrice * 1.25);
                result.IsSuccess = true;
            }
        }

        return result;
    }

    private static string CleanTitle(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var clean = Regex.Replace(raw, @"\s+", " ").Trim();
        clean = Regex.Replace(clean, @"\s*\|\s*Shopee.*$", "", RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"\s*-\s*TikTok.*$", "", RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"\s*\|\s*Lazada.*$", "", RegexOptions.IgnoreCase);
        return clean.Trim();
    }

    private async Task<bool> IsCdpRunningAsync(string endpoint)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2000));
            var url = endpoint.TrimEnd('/') + "/json/version";
            using var resp = await _httpClient.GetAsync(url, cts.Token);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            if (endpoint.Contains("localhost"))
            {
                try
                {
                    using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));
                    var fallbackUrl = endpoint.Replace("localhost", "127.0.0.1").TrimEnd('/') + "/json/version";
                    using var resp2 = await _httpClient.GetAsync(fallbackUrl, cts2.Token);
                    return resp2.IsSuccessStatusCode;
                }
                catch { }
            }
            return false;
        }
    }

    private async Task EnsureBrowserInitializedAsync(CancellationToken cancellationToken)
    {
        if (_browser != null && _browser.IsConnected) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser != null && _browser.IsConnected) return;

            _playwright ??= await Playwright.CreateAsync();

            // 1. Thử kết nối tới Microsoft Edge thật qua CDP (Port 9222) nếu đang bật
            var cdpUrl = _configuration?["Scraper:CdpEndpoint"] ?? "http://127.0.0.1:9222";
            if (await IsCdpRunningAsync(cdpUrl))
            {
                try
                {
                    _browser = await _playwright.Chromium.ConnectOverCDPAsync(cdpUrl);
                    _isConnectedOverCdp = true;
                    _logger.LogInformation("🚀 [Playwright] Đã kết nối thành công vào Microsoft Edge thật qua CDP tại {Url}!", cdpUrl);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Không thể kết nối qua CDP ({Msg}), chuyển sang trình duyệt headless.", ex.Message);
                    _isConnectedOverCdp = false;
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Không phát hiện Microsoft Edge Debug đang mở tại {Url}. Hệ thống chuyển sang Chromium headless dự phòng.", cdpUrl);
            }

            // 2. Fallback về Chromium Headless ngầm
            _isConnectedOverCdp = false;
            _logger.LogInformation("Khởi tạo tiến trình Chromium headless cho Playwright...");
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-accelerated-2d-canvas",
                    "--disable-gpu",
                    "--disable-blink-features=AutomationControlled",
                    "--blink-settings=imagesEnabled=false"
                }
            });
            _logger.LogInformation("Khởi tạo Chromium headless thành công.");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_browser != null)
        {
            if (!_isConnectedOverCdp)
            {
                try { await _browser.CloseAsync(); } catch { }
            }
            try { await _browser.DisposeAsync(); } catch { }
        }

        _playwright?.Dispose();
        _httpClient.Dispose();
        _initLock.Dispose();
        _concurrencyLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
