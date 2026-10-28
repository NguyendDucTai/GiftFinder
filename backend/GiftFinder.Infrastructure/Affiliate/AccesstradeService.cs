using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Affiliate.DTOs;
using GiftFinder.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Infrastructure.Affiliate;

public class AccesstradeService : IAffiliateNetworkService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccesstradeService> _logger;
    private readonly IWebScraperService _scraperService;

    private readonly string _apiKey;
    private readonly string _affiliateId;

    public AccesstradeService(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<AccesstradeService> logger,
        IWebScraperService scraperService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _scraperService = scraperService;

        _apiKey = _configuration["Accesstrade:ApiKey"] ?? "";
        _affiliateId = _configuration["Accesstrade:AffiliateId"] ?? "giftfinder_aff_01";
    }

    public async Task<List<AffiliateProductFeedItem>> FetchFeedAsync(
        string keyword, 
        ProductSource source, 
        int limit = 20, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey) || _apiKey.StartsWith("gsk_accesstrade_key_placeholder"))
        {
            _logger.LogWarning("Accesstrade API Key chưa được cấu hình. Bỏ qua lấy feed từ khóa '{Keyword}'.", keyword);
            return new List<AffiliateProductFeedItem>();
        }

        try
        {
            // LIVE MODE: Gọi API Accesstrade Datafeed / Search
            var requestUrl = $"https://api.accesstrade.vn/v1/offers_informations?keyword={Uri.EscapeDataString(keyword)}&limit={limit}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Authorization", $"Token {_apiKey}");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(content);
                var items = new List<AffiliateProductFeedItem>();

                if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataEl.EnumerateArray())
                    {
                        var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                        var price = item.TryGetProperty("price", out var p) && p.TryGetInt64(out var val) ? val : 250000;
                        var url = item.TryGetProperty("aff_link", out var a) ? a.GetString() ?? "" : "";
                        var img = item.TryGetProperty("image", out var imgEl) ? imgEl.GetString() : null;

                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url))
                        {
                            items.Add(new AffiliateProductFeedItem
                            {
                                Name = name,
                                Price = price,
                                OriginalUrl = url,
                                AffiliateUrl = url,
                                ImageUrl = img,
                                Source = source,
                                Rating = 4.8,
                                TotalReviews = 150
                            });
                        }
                    }
                }

                if (items.Count > 0) return items;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gọi Accesstrade Live API cho từ khóa: {Keyword}", keyword);
        }

        return new List<AffiliateProductFeedItem>();
    }

    public async Task<AffiliateProductFeedItem?> FetchProductByUrlAsync(
        string url, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var cleanUrl = SanitizeUrl(url);

        // 1. XỬ LÝ RIÊNG BIỆT CHO SÀN SHOPEE (Shopee Direct Affiliate)
        if (ShopeeAffiliateHandler.IsShopeeUrl(cleanUrl))
        {
            var shopeeInfo = ShopeeAffiliateHandler.ParseShopeeMetadata(cleanUrl);
            var shopeeAffId = _configuration["Shopee:AffiliateId"] ?? ShopeeAffiliateHandler.DefaultAffiliateId;

            // Cào dữ liệu thực tế bằng Playwright
            ScrapedProductData? scraped = null;
            try
            {
                scraped = await _scraperService.ScrapeProductAsync(cleanUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi cào dữ liệu Shopee bằng Playwright: {Message}", ex.Message);
            }

            // Nếu Playwright theo dõi được link chuyển hướng (ví dụ s.shopee.vn -> shopee.vn/product/...), phân tích lại ID
            if (scraped != null && !string.IsNullOrWhiteSpace(scraped.FinalUrl) && !scraped.FinalUrl.Equals(cleanUrl, StringComparison.OrdinalIgnoreCase))
            {
                var redirectedInfo = ShopeeAffiliateHandler.ParseShopeeMetadata(scraped.FinalUrl);
                if (redirectedInfo.ExternalId.StartsWith("SP_") && !redirectedInfo.ExternalId.StartsWith("SP_URL"))
                {
                    shopeeInfo = redirectedInfo;
                }
            }

            if (scraped == null || !scraped.IsSuccess || scraped.Price <= 0)
            {
                _logger.LogWarning("Không thể bóc tách giá thật từ link Shopee {Url}, bỏ qua không tạo sản phẩm lỗi.", cleanUrl);
                return null;
            }

            var shopeeTitle = !string.IsNullOrWhiteSpace(scraped.Title) ? scraped.Title : shopeeInfo.Title;
            var shopeePrice = scraped.Price;
            var shopeeOrigPrice = (scraped.OriginalPrice.HasValue && scraped.OriginalPrice.Value > shopeePrice)
                ? scraped.OriginalPrice.Value
                : (long)(shopeePrice * 1.25);
            var shopeeImage = !string.IsNullOrWhiteSpace(scraped.ImageUrl)
                ? scraped.ImageUrl
                : "https://down-vn.img.susercontent.com/file/sg-11134201-22120-123456789";

            var shopeeDeeplink = ShopeeAffiliateHandler.GenerateDeeplink(cleanUrl, $"item_{Guid.NewGuid():N}", shopeeAffId);

            return new AffiliateProductFeedItem
            {
                Name = shopeeTitle,
                Price = shopeePrice,
                OriginalPrice = shopeeOrigPrice,
                OriginalUrl = shopeeInfo.CleanUrl,
                AffiliateUrl = shopeeDeeplink,
                Source = ProductSource.Shopee,
                ExternalProductId = shopeeInfo.ExternalId,
                ImageUrl = shopeeImage,
                Description = $"Sản phẩm quà tặng chất lượng cao tuyển chọn từ sàn Shopee: {shopeeTitle}",
                Rating = 4.9,
                TotalReviews = 188
            };
        }

        string? customAffiliateUrl = null;
        if (cleanUrl.Contains("goeco.mobi", StringComparison.OrdinalIgnoreCase) ||
            cleanUrl.Contains("passio.eco", StringComparison.OrdinalIgnoreCase))
        {
            customAffiliateUrl = cleanUrl;
        }

        var source = ProductSource.TikTokShop;
        if (cleanUrl.Contains("lazada.vn", StringComparison.OrdinalIgnoreCase))
            source = ProductSource.Lazada;
        else if (cleanUrl.Contains("tiki.vn", StringComparison.OrdinalIgnoreCase))
            source = ProductSource.Tiki;


        // Cào dữ liệu thực tế: Ưu tiên dùng Playwright cho TikTok Shop và các link được hỗ trợ
        ScrapedProductData? playwrightScraped = null;
        if (_scraperService.SupportsUrl(cleanUrl))
        {
            try
            {
                playwrightScraped = await _scraperService.ScrapeProductAsync(cleanUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi cào dữ liệu qua Playwright cho URL {Url}: {Message}", cleanUrl, ex.Message);
            }
        }

        // Cào dữ liệu OpenGraph metadata & redirect tracking qua HttpClient làm bổ trợ
        var metadata = await ScrapeProductMetadataAsync(cleanUrl, cancellationToken);
        var effectiveUrl = SanitizeUrl(playwrightScraped?.FinalUrl ?? metadata.FinalUrl ?? cleanUrl);

        // Tự động nhận diện sàn thực tế từ URL đích (đặc biệt khi dùng link rút gọn Ecomobi goeco.mobi)
        if (effectiveUrl.Contains("tiktok.com", StringComparison.OrdinalIgnoreCase))
            source = ProductSource.TikTokShop;
        else if (effectiveUrl.Contains("lazada.vn", StringComparison.OrdinalIgnoreCase) || cleanUrl.Contains("lazada.vn", StringComparison.OrdinalIgnoreCase))
            source = ProductSource.Lazada;
        else if (effectiveUrl.Contains("tiki.vn", StringComparison.OrdinalIgnoreCase))
            source = ProductSource.Tiki;
        else if (effectiveUrl.Contains("shopee.vn", StringComparison.OrdinalIgnoreCase))
            source = ProductSource.Shopee;

        var title = (playwrightScraped != null && !string.IsNullOrWhiteSpace(playwrightScraped.Title))
            ? playwrightScraped.Title
            : (!string.IsNullOrWhiteSpace(metadata.Title)
                ? metadata.Title
                : FormatSlugToTitle(ExtractSlugFromUrl(effectiveUrl)));

        var description = !string.IsNullOrWhiteSpace(metadata.Description)
            ? metadata.Description
            : $"Sản phẩm quà tặng chính hãng, chất lượng cao tuyển chọn từ sàn {source}: {title}";

        var imageUrl = (playwrightScraped != null && !string.IsNullOrWhiteSpace(playwrightScraped.ImageUrl))
            ? playwrightScraped.ImageUrl
            : (!string.IsNullOrWhiteSpace(metadata.ImageUrl)
                ? metadata.ImageUrl
                : (source == ProductSource.TikTokShop
                    ? "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?w=600"
                    : (source == ProductSource.Lazada
                        ? "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?w=600"
                        : "https://images.unsplash.com/photo-1549465220-1a8b9238cd48?w=600")));

        var price = (playwrightScraped != null && playwrightScraped.Price > 0)
            ? playwrightScraped.Price
            : (metadata.Price ?? (source == ProductSource.TikTokShop ? 189000L : 250000L));

        var originalPrice = (playwrightScraped != null && playwrightScraped.OriginalPrice.HasValue && playwrightScraped.OriginalPrice.Value > price)
            ? playwrightScraped.OriginalPrice.Value
            : (long)(price * 1.25);

        var deeplink = customAffiliateUrl ?? await GenerateDeeplinkAsync(cleanUrl, $"item_{Guid.NewGuid():N}", cancellationToken);

        return new AffiliateProductFeedItem
        {
            Name = title,
            Price = price,
            OriginalPrice = originalPrice,
            OriginalUrl = effectiveUrl,
            AffiliateUrl = deeplink,
            Source = source,
            ExternalProductId = !string.IsNullOrWhiteSpace(metadata.ExternalId)
                ? metadata.ExternalId
                : $"URL_{Guid.NewGuid():N}"[..12],
            ImageUrl = imageUrl,
            Description = description,
            Rating = 4.8,
            TotalReviews = 168
        };
    }

    private async Task<(string? Title, string? ImageUrl, string? Description, long? Price, string? ExternalId, string? FinalUrl)> ScrapeProductMetadataAsync(
        string url, 
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            request.Headers.Add("Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HTTP GET metadata failed for URL {Url} with status code {StatusCode}", url, response.StatusCode);
                return (null, null, null, null, null, null);
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(html))
            {
                return (null, null, null, null, null, null);
            }

            // 1. Trích xuất Tiêu đề (og:title, twitter:title hoặc <title>)
            string? title = null;
            var titleMatch = Regex.Match(html, @"<meta\s+[^>]*property=[""']og:title[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (!titleMatch.Success)
                titleMatch = Regex.Match(html, @"<meta\s+[^>]*content=[""']([^""']+)[""'][^>]*property=[""']og:title[""']", RegexOptions.IgnoreCase);
            if (!titleMatch.Success)
                titleMatch = Regex.Match(html, @"<meta\s+[^>]*property=[""']twitter:title[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (!titleMatch.Success)
                titleMatch = Regex.Match(html, @"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase);

            if (titleMatch.Success)
            {
                title = WebUtility.HtmlDecode(titleMatch.Groups[1].Value);
                title = Regex.Replace(title, @"\s+", " ").Trim();
            }

            // 2. Trích xuất Hình ảnh (og:image, twitter:image)
            string? imageUrl = null;
            var imgMatch = Regex.Match(html, @"<meta\s+[^>]*property=[""']og:image[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (!imgMatch.Success)
                imgMatch = Regex.Match(html, @"<meta\s+[^>]*content=[""']([^""']+)[""'][^>]*property=[""']og:image[""']", RegexOptions.IgnoreCase);
            if (!imgMatch.Success)
                imgMatch = Regex.Match(html, @"<meta\s+[^>]*property=[""']twitter:image[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);

            if (imgMatch.Success)
            {
                imageUrl = WebUtility.HtmlDecode(imgMatch.Groups[1].Value.Trim());
            }

            // 3. Trích xuất Mô tả (og:description, meta name=description, twitter:description)
            string? description = null;
            var descMatch = Regex.Match(html, @"<meta\s+[^>]*property=[""']og:description[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (!descMatch.Success)
                descMatch = Regex.Match(html, @"<meta\s+[^>]*name=[""']description[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (!descMatch.Success)
                descMatch = Regex.Match(html, @"<meta\s+[^>]*content=[""']([^""']+)[""'][^>]*name=[""']description[""']", RegexOptions.IgnoreCase);

            if (descMatch.Success)
            {
                description = WebUtility.HtmlDecode(descMatch.Groups[1].Value);
                description = Regex.Replace(description, @"\s+", " ").Trim();
            }

            // 4. Trích xuất Giá sản phẩm nếu có trong HTML / JSON
            long? price = null;
            var priceMatch = Regex.Match(html, @"[""']sale_price[""']\s*:\s*[""']?(\d+)", RegexOptions.IgnoreCase);
            if (!priceMatch.Success)
                priceMatch = Regex.Match(html, @"[""']price[""']\s*:\s*[""']?(\d+)", RegexOptions.IgnoreCase);
            if (!priceMatch.Success)
                priceMatch = Regex.Match(html, @"property=[""']product:price:amount[""'][^>]*content=[""'](\d+)", RegexOptions.IgnoreCase);

            if (priceMatch.Success && long.TryParse(priceMatch.Groups[1].Value, out var parsedPrice) && parsedPrice > 1000)
            {
                if (parsedPrice < 100_000_000) price = parsedPrice;
            }

            // 5. Trích xuất Mã ID sản phẩm thật từ sàn (TikTok, Tiki, Shopee, Lazada)
            string? externalId = null;
            var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
            var idMatch = Regex.Match(finalUrl, @"/pdp/(\d+)", RegexOptions.IgnoreCase);
            if (!idMatch.Success) idMatch = Regex.Match(html, @"[""']product_id[""']\s*:\s*[""']?(\d{10,25})[""']?", RegexOptions.IgnoreCase);
            if (!idMatch.Success) idMatch = Regex.Match(finalUrl, @"-p(\d+)\.html", RegexOptions.IgnoreCase);
            if (!idMatch.Success) idMatch = Regex.Match(finalUrl, @"-i\.(\d+)\.(\d+)", RegexOptions.IgnoreCase);
            if (!idMatch.Success) idMatch = Regex.Match(finalUrl, @"[/-]i(\d{8,15})", RegexOptions.IgnoreCase);
            if (!idMatch.Success) idMatch = Regex.Match(finalUrl, @"pdp-i(\d+)", RegexOptions.IgnoreCase);

            if (idMatch.Success)
            {
                externalId = idMatch.Groups[idMatch.Groups.Count - 1].Value;
            }

            _logger.LogInformation("Đã cào metadata sản phẩm thành công từ {Url}: Tiêu đề='{Title}', ExternalId='{ExternalId}', Giá={Price}, FinalUrl='{FinalUrl}'",
                url, title, externalId, price, finalUrl);

            return (title, imageUrl, description, price, externalId, finalUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể cào metadata sản phẩm từ url: {Url}", url);
            return (null, null, null, null, null, null);
        }
    }

        public Task<string> GenerateDeeplinkAsync(
            string originalUrl, 
            string subId, 
            CancellationToken cancellationToken = default)
        {
            var escapedSubId = Uri.EscapeDataString(subId);

            // 0. Xử lý riêng biệt cho Shopee: Tự động gắn mã Shopee Affiliate trực tiếp (an_17374170692)
            if (ShopeeAffiliateHandler.IsShopeeUrl(originalUrl))
            {
                var shopeeAffId = _configuration["Shopee:AffiliateId"] ?? ShopeeAffiliateHandler.DefaultAffiliateId;
                return Task.FromResult(ShopeeAffiliateHandler.GenerateDeeplink(originalUrl, subId, shopeeAffId));
            }

            // 1. Nếu link đã là link affiliate của Ecomobi, giữ nguyên và gắn thêm sub1 nếu chưa có
            if (originalUrl.Contains("goeco.mobi", StringComparison.OrdinalIgnoreCase) ||
                originalUrl.Contains("passio.eco", StringComparison.OrdinalIgnoreCase))
            {
                var separator = originalUrl.Contains('?') ? "&" : "?";
                var urlWithSub = originalUrl.Contains("sub1=") ? originalUrl : $"{originalUrl}{separator}sub1={escapedSubId}";
                return Task.FromResult(urlWithSub);
            }

            // 2. Nếu là sản phẩm Lazada, tự động sinh link hoa hồng qua Ecomobi với Token của tài khoản
            if (originalUrl.Contains("lazada.vn", StringComparison.OrdinalIgnoreCase))
            {
                var ecomobiToken = _configuration["Ecomobi:Token"] ?? "WrkhMbQezyWOwnMYpFEiz";
                var encodedUrl = Uri.EscapeDataString(originalUrl);
                var ecomobiDeeplink = $"https://goeco.mobi/?token={ecomobiToken}&url={encodedUrl}&advertiser_id=lazada.vn&global_domain=lazada.vn&sub1={escapedSubId}";
                return Task.FromResult(ecomobiDeeplink);
            }

            // 3. Các sàn TikTok Shop, Tiki, Shopee xử lý qua Accesstrade
            var base64Url = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(originalUrl)));
            string campaignId;
            if (originalUrl.Contains("tiki.vn", StringComparison.OrdinalIgnoreCase))
            {
                campaignId = _configuration["Accesstrade:CampaignIds:Tiki"] ?? "4348614231480407268"; // TIKI (CPS)
            }
            else if (originalUrl.Contains("tiktok.com", StringComparison.OrdinalIgnoreCase))
            {
                campaignId = _configuration["Accesstrade:CampaignIds:TikTokShop"] ?? "6648523843406889655"; // TIKTOK SHOP CPS
            }
            else
            {
                campaignId = _configuration["Accesstrade:CampaignIds:Shopee"] ?? "4751584435713464237"; // SHOPEE VIETNAM
            }

            // Định dạng Deeplink v6 chính thức đã kiểm thử thành công 100% của Accesstrade
        var publisherIdV6 = _configuration["Accesstrade:PublisherIdV6"] ?? "7086451798803801329";
        var deeplink = $"https://go.isclix.com/deep_link/v6/{publisherIdV6}/{campaignId}?url_enc={base64Url}&sub1={escapedSubId}";
        return Task.FromResult(deeplink);
    }

    private static string ExtractSlugFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                var candidate = WebUtility.UrlDecode(segments[0]);
                if (candidate.Length > 3 && !candidate.Equals("product", StringComparison.OrdinalIgnoreCase))
                    return candidate;
                if (segments.Length > 1) return WebUtility.UrlDecode(segments[1]);
            }
        }
        catch { }
        return "mon-qua-tang-tinh-te";
    }

    private static string SanitizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        var clean = url.Trim();

        try
        {
            // Link Lazada: cắt bỏ toàn bộ query param rác sau .html (như ?c=&channelLpJumpArgs=&clickTrackInfo=...)
            if (clean.Contains("lazada.vn/products/", StringComparison.OrdinalIgnoreCase) && clean.Contains(".html", StringComparison.OrdinalIgnoreCase))
            {
                var htmlIndex = clean.IndexOf(".html", StringComparison.OrdinalIgnoreCase);
                if (htmlIndex > 0)
                {
                    return clean[..(htmlIndex + 5)];
                }
            }

            // Link Shopee: cắt bỏ các query param rác tìm kiếm sau mã -i.
            if (clean.Contains("shopee.vn", StringComparison.OrdinalIgnoreCase) && clean.Contains("-i."))
            {
                var qIndex = clean.IndexOf('?');
                if (qIndex > 0)
                {
                    return clean[..qIndex];
                }
            }

            // Link Tiki: cắt bỏ query param rác sau .html
            if (clean.Contains("tiki.vn", StringComparison.OrdinalIgnoreCase) && clean.Contains(".html", StringComparison.OrdinalIgnoreCase))
            {
                var htmlIndex = clean.IndexOf(".html", StringComparison.OrdinalIgnoreCase);
                if (htmlIndex > 0)
                {
                    return clean[..(htmlIndex + 5)];
                }
            }
        }
        catch { }

        return clean;
    }

    private static string FormatSlugToTitle(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return "Set Quà Tặng Độc Đáo Tinh Tế";

        // 1. Giải mã URL encoding (chuyển %E1%BA%AF thành 'ắ', v.v.)
        var decoded = WebUtility.UrlDecode(slug);

        // 2. Loại bỏ phần mã số nhận diện của Shopee (-i.shopid.itemid) hoặc Tiki/Lazada (-p123456.html)
        var cleaned = Regex.Replace(decoded, @"-i\.\d+\.\d+.*$", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"-p\d+\.html.*$", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\.html.*$", "", RegexOptions.IgnoreCase);

        // 3. Thay dấu gạch ngang và gạch dưới bằng khoảng trắng
        cleaned = cleaned.Replace("-", " ").Replace("_", " ").Trim();
        cleaned = Regex.Replace(cleaned, @"\s+", " ");

        // 4. Giới hạn độ dài an toàn không cắt ngang từ
        if (cleaned.Length > 90)
        {
            var cutoff = cleaned.LastIndexOf(' ', 90);
            cleaned = cutoff > 40 ? cleaned[..cutoff] : cleaned[..90];
        }

        if (string.IsNullOrWhiteSpace(cleaned)) return "Set Quà Tặng Độc Đáo Tinh Tế";
        return char.ToUpper(cleaned[0]) + cleaned[1..];
    }


}
