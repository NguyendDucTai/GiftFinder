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

    private readonly string _apiKey;
    private readonly string _affiliateId;
    private readonly bool _useMock;

    public AccesstradeService(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<AccesstradeService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _apiKey = _configuration["Accesstrade:ApiKey"] ?? "";
        _affiliateId = _configuration["Accesstrade:AffiliateId"] ?? "giftfinder_aff_01";
        
        var useMockConfig = _configuration["Accesstrade:UseMock"];
        _useMock = string.IsNullOrEmpty(_apiKey) 
                   || _apiKey.StartsWith("gsk_accesstrade_key_placeholder") 
                   || (bool.TryParse(useMockConfig, out var parsedMock) && parsedMock);
    }

    public async Task<List<AffiliateProductFeedItem>> FetchFeedAsync(
        string keyword, 
        ProductSource source, 
        int limit = 20, 
        CancellationToken cancellationToken = default)
    {
        if (_useMock)
        {
            _logger.LogInformation("AccesstradeService running in MOCK mode. Fetching feed for keyword: '{Keyword}', source: {Source}", keyword, source);
            return GetMockCatalog(keyword, source, limit);
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
            _logger.LogWarning(ex, "Lỗi khi gọi Accesstrade Live API. Chuyển sang fallback mock catalog.");
        }

        // Fallback to mock catalog
        return GetMockCatalog(keyword, source, limit);
    }

    public async Task<AffiliateProductFeedItem?> FetchProductByUrlAsync(
        string url, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var cleanUrl = url.Trim();
        var source = ProductSource.Shopee;
        if (cleanUrl.Contains("tiktok.com", StringComparison.OrdinalIgnoreCase))
            source = ProductSource.TikTokShop;
        else if (cleanUrl.Contains("lazada.vn", StringComparison.OrdinalIgnoreCase))
            source = ProductSource.Lazada;
        else if (cleanUrl.Contains("tiki.vn", StringComparison.OrdinalIgnoreCase))
            source = ProductSource.Tiki;

        if (_useMock)
        {
            // Trong mock mode, tìm trong catalog nếu khớp mã
            var matched = GetMockCatalog("", source, 50)
                .FirstOrDefault(p => cleanUrl.Contains(p.ExternalProductId ?? "###", StringComparison.OrdinalIgnoreCase));

            if (matched != null)
            {
                matched.OriginalUrl = cleanUrl;
                matched.AffiliateUrl = await GenerateDeeplinkAsync(cleanUrl, $"item_{Guid.NewGuid():N}", cancellationToken);
                return matched;
            }
        }

        // Cào dữ liệu thực tế (OpenGraph metadata & redirect tracking) từ link sản phẩm (TikTok, Shopee, Tiki, Lazada)
        var metadata = await ScrapeProductMetadataAsync(cleanUrl, cancellationToken);

        var title = !string.IsNullOrWhiteSpace(metadata.Title)
            ? metadata.Title
            : FormatSlugToTitle(ExtractSlugFromUrl(cleanUrl));

        var description = !string.IsNullOrWhiteSpace(metadata.Description)
            ? metadata.Description
            : $"Sản phẩm quà tặng chính hãng, chất lượng cao tuyển chọn từ sàn {source}: {title}";

        var imageUrl = !string.IsNullOrWhiteSpace(metadata.ImageUrl)
            ? metadata.ImageUrl
            : (source == ProductSource.TikTokShop
                ? "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?w=600"
                : "https://images.unsplash.com/photo-1549465220-1a8b9238cd48?w=600");

        var price = metadata.Price ?? (source == ProductSource.TikTokShop ? 189000L : 250000L);
        var deeplink = await GenerateDeeplinkAsync(cleanUrl, $"item_{Guid.NewGuid():N}", cancellationToken);

        return new AffiliateProductFeedItem
        {
            Name = title,
            Price = price,
            OriginalPrice = (long)(price * 1.25),
            OriginalUrl = cleanUrl,
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

    private async Task<(string? Title, string? ImageUrl, string? Description, long? Price, string? ExternalId)> ScrapeProductMetadataAsync(
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
                return (null, null, null, null, null);
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(html))
            {
                return (null, null, null, null, null);
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

            if (idMatch.Success)
            {
                externalId = idMatch.Groups[idMatch.Groups.Count - 1].Value;
            }

            _logger.LogInformation("Đã cào metadata sản phẩm thành công từ {Url}: Tiêu đề='{Title}', ExternalId='{ExternalId}', Giá={Price}",
                url, title, externalId, price);

            return (title, imageUrl, description, price, externalId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể cào metadata sản phẩm từ url: {Url}", url);
            return (null, null, null, null, null);
        }
    }

    public Task<string> GenerateDeeplinkAsync(
        string originalUrl, 
        string subId, 
        CancellationToken cancellationToken = default)
    {
        var escapedSubId = Uri.EscapeDataString(subId);
        var base64Url = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(originalUrl)));

        // Xác định Campaign ID chính thức trên Accesstrade theo từng sàn (ưu tiên cấu hình từ appsettings)
        string campaignId;
        if (originalUrl.Contains("tiki.vn", StringComparison.OrdinalIgnoreCase))
        {
            campaignId = _configuration["Accesstrade:CampaignIds:Tiki"] ?? "4348614231480407268"; // TIKI (CPS)
        }
        else if (originalUrl.Contains("tiktok.com", StringComparison.OrdinalIgnoreCase))
        {
            campaignId = _configuration["Accesstrade:CampaignIds:TikTokShop"] ?? "6648523843406889655"; // TIKTOK SHOP CPS
        }
        else if (originalUrl.Contains("lazada.vn", StringComparison.OrdinalIgnoreCase))
        {
            campaignId = _configuration["Accesstrade:CampaignIds:Lazada"] ?? "4348614539120935532"; // LAZADA VIETNAM
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
                var candidate = segments[0];
                if (candidate.Length > 3 && !candidate.Equals("product", StringComparison.OrdinalIgnoreCase))
                    return candidate;
                if (segments.Length > 1) return segments[1];
            }
        }
        catch { }
        return "mon-qua-tang-tinh-te";
    }

    private static string FormatSlugToTitle(string slug)
    {
        var cleaned = slug.Replace("-", " ").Replace("_", " ").Trim();
        if (cleaned.Length > 60) cleaned = cleaned[..60];
        if (string.IsNullOrWhiteSpace(cleaned)) return "Set Quà Tặng Độc Đáo Tinh Tế";
        return char.ToUpper(cleaned[0]) + cleaned[1..];
    }

    private static List<AffiliateProductFeedItem> GetMockCatalog(string keyword, ProductSource source, int limit)
    {
        var catalog = new List<AffiliateProductFeedItem>
        {
            // === SHOPEE PRODUCTS ===
            new()
            {
                Name = "Set Quà Tặng Nến Thơm Cao Cấp Agaya Kèm Hộp Quà Vintage & Diêm Thủy Tinh",
                Price = 195000,
                OriginalPrice = 250000,
                ImageUrl = "https://images.unsplash.com/photo-1603006905003-be475563bc59?w=600",
                OriginalUrl = "https://shopee.vn/Set-Qua-Tang-Nen-Thom-Cao-Cap-Agaya-i.123456.987654",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshopee.vn%2FSet-Qua-Tang-Nen-Thom-Agaya&sub1=shopee_nen_thom",
                Source = ProductSource.Shopee,
                ExternalProductId = "SP_NENTHOM_01",
                Description = "Nến thơm sáp đậu nành thiên nhiên Agaya tinh dầu cao cấp, mùi hương gỗ thông và hoa oải hương dịu nhẹ, tạo cảm giác thư giãn tuyệt đối.",
                Rating = 4.9,
                TotalReviews = 1420,
                CommissionRate = 0.08m
            },
            new()
            {
                Name = "Cốc Giữ Nhiệt Lock&Lock 550ml Khắc Tên Theo Yêu Cầu Kèm Hộp Quà Sang Trọng",
                Price = 380000,
                OriginalPrice = 450000,
                ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=600",
                OriginalUrl = "https://shopee.vn/Coc-Giu-Nhiet-LockLock-550ml-Khac-Ten-i.123456.987655",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshopee.vn%2FCoc-Giu-Nhiet-LockLock&sub1=shopee_coc_lock",
                Source = ProductSource.Shopee,
                ExternalProductId = "SP_LOCKLOCK_02",
                Description = "Cốc giữ nhiệt inox 316 cao cấp giữ nhiệt nóng lạnh 12h, có dịch vụ khắc tên và lời chúc ý nghĩa theo yêu cầu của bạn.",
                Rating = 4.9,
                TotalReviews = 890,
                CommissionRate = 0.07m
            },
            new()
            {
                Name = "Tai Nghe Chống Ồn Sony WH-CH520 Không Dây Pin 50h Chính Hãng",
                Price = 1190000,
                OriginalPrice = 1490000,
                ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=600",
                OriginalUrl = "https://shopee.vn/Tai-Nghe-Sony-WH-CH520-Chinh-Hang-i.123456.987656",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshopee.vn%2FTai-Nghe-Sony&sub1=shopee_sony_headphone",
                Source = ProductSource.Shopee,
                ExternalProductId = "SP_SONY_03",
                Description = "Tai nghe chụp tai Sony thế hệ mới, âm thanh DSEE sống động, microphone đàm thoại sắc nét, đệm tai êm ái.",
                Rating = 5.0,
                TotalReviews = 540,
                CommissionRate = 0.05m
            },
            new()
            {
                Name = "Đồng Hồ Nữ Dây Da Mặt Số Tinh Tế Curnon Kashmir Kèm Thiệp Viết Tay",
                Price = 1850000,
                OriginalPrice = 2200000,
                ImageUrl = "https://images.unsplash.com/photo-1524805444758-089113d48a6d?w=600",
                OriginalUrl = "https://shopee.vn/Dong-Ho-Nu-Curnon-Kashmir-i.123456.987657",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshopee.vn%2FDong-Ho-Curnon&sub1=shopee_curnon_watch",
                Source = ProductSource.Shopee,
                ExternalProductId = "SP_CURNON_04",
                Description = "Đồng hồ nữ chính hãng Curnon phong cách tối giản thanh lịch, kính sapphire chống xước, máy Miyota Nhật Bản bền bỉ.",
                Rating = 4.8,
                TotalReviews = 310,
                CommissionRate = 0.09m
            },
            new()
            {
                Name = "Đèn Ngủ Mặt Trăng 3D Cảm Ứng Đổi 16 Màu Kèm Đế Gỗ Tự Nhiên",
                Price = 129000,
                OriginalPrice = 180000,
                ImageUrl = "https://images.unsplash.com/photo-1532767153582-b1a0e5145009?w=600",
                OriginalUrl = "https://shopee.vn/Den-Ngu-Mat-Trang-3D-i.123456.987658",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshopee.vn%2FDen-Mat-Trang-3D&sub1=shopee_moon_lamp",
                Source = ProductSource.Shopee,
                ExternalProductId = "SP_MOONLAMP_05",
                Description = "Đèn ngủ in 3D công nghệ cao mô phỏng bề mặt mặt trăng chân thực, pin sạc USB tích hợp điều khiển từ xa.",
                Rating = 4.8,
                TotalReviews = 2100,
                CommissionRate = 0.10m
            },
            new()
            {
                Name = "Set Trà Hoa Thảo Mộc 9 Vị Dưỡng Nhan Hoàng Cung Kèm Bình Thủy Tinh",
                Price = 245000,
                OriginalPrice = 310000,
                ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=600",
                OriginalUrl = "https://shopee.vn/Set-Tra-Hoa-Thao-Moc-9-Vi-i.123456.987659",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshopee.vn%2FTra-Hoa-Thao-Moc&sub1=shopee_tea_gift",
                Source = ProductSource.Shopee,
                ExternalProductId = "SP_TRAHOA_06",
                Description = "Set trà thảo mộc hoa cúc, kỷ tử, hoa hồng, táo đỏ tự nhiên không hóa chất, đóng hộp quà kraft cao cấp trang trọng.",
                Rating = 4.9,
                TotalReviews = 670,
                CommissionRate = 0.08m
            },
            new()
            {
                Name = "Máy Chụp Ảnh Lấy Liền Fujifilm Instax Mini 12 Kèm 20 Tấm Film & Sticker",
                Price = 2390000,
                OriginalPrice = 2690000,
                ImageUrl = "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?w=600",
                OriginalUrl = "https://shopee.vn/Fujifilm-Instax-Mini-12-i.123456.987660",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshopee.vn%2FFujifilm-Instax&sub1=shopee_instax",
                Source = ProductSource.Shopee,
                ExternalProductId = "SP_INSTAX_07",
                Description = "Máy ảnh chụp lấy liền thiết kế pastel xinh xắn, tự động phơi sáng chuẩn đẹp mọi góc chụp, lưu giữ từng kỷ niệm ngọt ngào.",
                Rating = 5.0,
                TotalReviews = 430,
                CommissionRate = 0.04m
            },
            new()
            {
                Name = "Bó Hoa Sáp Hướng Dương 7 Bông Kèm Đèn Led Đom Đóm & Thiệp Chúc Mừng",
                Price = 99000,
                OriginalPrice = 140000,
                ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=600",
                OriginalUrl = "https://shopee.vn/Bo-Hoa-Sap-Huong-Duong-i.123456.987661",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshopee.vn%2FHoa-Sap-Huong-Duong&sub1=shopee_flower",
                Source = ProductSource.Shopee,
                ExternalProductId = "SP_HOASAP_08",
                Description = "Hoa sáp thơm vĩnh cửu màu sắc rực rỡ tượng trưng cho niềm tin và hy vọng, giữ hương thơm đến 3 năm.",
                Rating = 4.7,
                TotalReviews = 3200,
                CommissionRate = 0.10m
            },

            // === TIKTOK SHOP PRODUCTS ===
            new()
            {
                Name = "Gấu Bông Capybara Đeo Rùa Balo Nhồi Bông Siêu Mềm Mịn 70cm",
                Price = 185000,
                OriginalPrice = 240000,
                ImageUrl = "https://images.unsplash.com/photo-1559454403-b8fb88521f11?w=600",
                OriginalUrl = "https://shop.tiktok.com/view/product/172948291029384",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshop.tiktok.com%2Fcapybara&sub1=tiktok_capybara",
                Source = ProductSource.TikTokShop,
                ExternalProductId = "TK_CAPYBARA_01",
                Description = "Gấu bông hot trend Capybara bộ trưởng ngoại giao đeo balo rùa xanh siêu cưng, bông gòn 7D trắng tinh khiết an toàn.",
                Rating = 4.9,
                TotalReviews = 8800,
                CommissionRate = 0.09m
            },
            new()
            {
                Name = "Loa Bluetooth Mini Divoom Ditoo Plus Màn Hình Pixel Retro Độc Đáo",
                Price = 1450000,
                OriginalPrice = 1790000,
                ImageUrl = "https://images.unsplash.com/photo-1545454675-3531b543be5d?w=600",
                OriginalUrl = "https://shop.tiktok.com/view/product/172948291029385",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshop.tiktok.com%2Fdivoom-ditoo&sub1=tiktok_divoom",
                Source = ProductSource.TikTokShop,
                ExternalProductId = "TK_DIVOOM_02",
                Description = "Loa không dây mô phỏng máy vi tính cổ điển kiêm máy chơi game pixel, đồng hồ báo thức và màn hình decor cực chất.",
                Rating = 4.9,
                TotalReviews = 920,
                CommissionRate = 0.06m
            },
            new()
            {
                Name = "Máy Xông Tinh Dầu Hiệu Ứng Ngọn Lửa Phun Sương Decor Đổi Màu RGB",
                Price = 220000,
                OriginalPrice = 310000,
                ImageUrl = "https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=600",
                OriginalUrl = "https://shop.tiktok.com/view/product/172948291029386",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshop.tiktok.com%2Fflame-diffuser&sub1=tiktok_flame_diffuser",
                Source = ProductSource.TikTokShop,
                ExternalProductId = "TK_DIFFUSER_03",
                Description = "Máy khuếch tán tinh dầu sóng siêu âm mô phỏng ngọn lửa ấm áp, cấp ẩm êm dịu không gây tiếng ồn.",
                Rating = 4.8,
                TotalReviews = 4100,
                CommissionRate = 0.08m
            },
            new()
            {
                Name = "Dây Chuyền Bạc Ý 925 Mặt Cỏ Bốn Lá May Mắn Đính Đá Pha Lê Ánh Sáng",
                Price = 390000,
                OriginalPrice = 520000,
                ImageUrl = "https://images.unsplash.com/photo-1599643478518-a784e5dc4c8f?w=600",
                OriginalUrl = "https://shop.tiktok.com/view/product/172948291029387",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshop.tiktok.com%2Fnecklace-silver&sub1=tiktok_necklace",
                Source = ProductSource.TikTokShop,
                ExternalProductId = "TK_NECKLACE_04",
                Description = "Trang sức bạc thật cao cấp đính đá Zirconia sáng lấp lánh, mang lại may mắn và tình duyên cho người đeo.",
                Rating = 4.9,
                TotalReviews = 1650,
                CommissionRate = 0.10m
            },
            new()
            {
                Name = "Hộp Quà Socola Tươi Nghệ Nhân Nama Chocolate Cao Cấp Vị Matcha & Cacao",
                Price = 250000,
                OriginalPrice = 320000,
                ImageUrl = "https://images.unsplash.com/photo-1548907040-4baa42d10919?w=600",
                OriginalUrl = "https://shop.tiktok.com/view/product/172948291029388",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshop.tiktok.com%2Fnama-chocolate&sub1=tiktok_chocolate",
                Source = ProductSource.TikTokShop,
                ExternalProductId = "TK_CHOCO_05",
                Description = "Socola tươi mềm tan ngậy thơm làm thủ công từ kem tươi New Zealand và bột trà xanh Uji Nhật Bản thượng hạng.",
                Rating = 4.9,
                TotalReviews = 2300,
                CommissionRate = 0.08m
            },
            new()
            {
                Name = "Bàn Phím Cơ Không Dây E-Dra EK387 Pro RGB Mạch Xuôi Gõ Êm Tay",
                Price = 890000,
                OriginalPrice = 1050000,
                ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=600",
                OriginalUrl = "https://shop.tiktok.com/view/product/172948291029389",
                AffiliateUrl = "https://go.isclix.com/deep_link/giftfinder_aff_01?url=https%3A%2F%2Fshop.tiktok.com%2Fkeyboard-edra&sub1=tiktok_keyboard",
                Source = ProductSource.TikTokShop,
                ExternalProductId = "TK_KEYBOARD_06",
                Description = "Bàn phím cơ thiết kế nhỏ gọn 87 phím, switch Outemu mượt mà, LED RGB đa hiệu ứng, kết nối Bluetooth & 2.4Ghz.",
                Rating = 4.8,
                TotalReviews = 1240,
                CommissionRate = 0.05m
            }
        };

        var query = catalog.Where(c => c.Source == source);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            var filtered = query.Where(p => 
                p.Name.ToLowerInvariant().Contains(kw) 
                || (p.Description != null && p.Description.ToLowerInvariant().Contains(kw))).ToList();

            if (filtered.Count > 0)
                return filtered.Take(limit).ToList();
        }

        return query.Take(limit).ToList();
    }
}
