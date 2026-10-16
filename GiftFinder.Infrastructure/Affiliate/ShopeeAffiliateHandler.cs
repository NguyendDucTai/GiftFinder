using System.Net;
using System.Text.RegularExpressions;

namespace GiftFinder.Infrastructure.Affiliate;

/// <summary>
/// Trình xử lý chuyên biệt độc lập cho sàn Shopee (Shopee Affiliate Direct).
/// Xử lý bóc tách slug tiếng Việt, bóc tách ShopId/ItemId và tự động gắn mã đối tác Shopee chính thức.
/// </summary>
public static class ShopeeAffiliateHandler
{
    public const string DefaultAffiliateId = "an_17374170692";

    /// <summary>
    /// Kiểm tra xem URL có thuộc sàn Shopee hay không (bao gồm link web và link rút gọn app s.shopee.vn)
    /// </summary>
    public static bool IsShopeeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return url.Contains("shopee.vn", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("s.shopee.vn", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sinh link Shopee Affiliate chính thức gắn trực tiếp mã đối tác (không qua trung gian Accesstrade)
    /// </summary>
    public static string GenerateDeeplink(string url, string subId, string? affiliateId = null)
    {
        var affId = !string.IsNullOrWhiteSpace(affiliateId) ? affiliateId.Trim() : DefaultAffiliateId;
        var cleanUrl = SanitizeShopeeUrl(url);

        // 1. Nếu là link rút gọn từ app (s.shopee.vn), giữ nguyên link rút gọn
        if (cleanUrl.Contains("s.shopee.vn", StringComparison.OrdinalIgnoreCase))
        {
            return cleanUrl;
        }

        // 2. Nếu là link web Shopee, gắn các tham số tracking Shopee Affiliate chính thức
        var separator = cleanUrl.Contains('?') ? "&" : "?";
        var trackingParams = $"utm_source={affId}&utm_medium=affiliates&mmp_pid={affId}";
        if (!string.IsNullOrWhiteSpace(subId))
        {
            trackingParams += $"&sub_id={Uri.EscapeDataString(subId)}";
        }

        return $"{cleanUrl}{separator}{trackingParams}";
    }

    /// <summary>
    /// Làm sạch URL Shopee, loại bỏ các tham số tracking thừa
    /// </summary>
    public static string SanitizeShopeeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        var clean = url.Trim();

        try
        {
            // Cắt bỏ query param thừa phía sau mã sản phẩm -i.shopId.itemId
            if (clean.Contains("shopee.vn", StringComparison.OrdinalIgnoreCase) && clean.Contains("-i."))
            {
                var qIndex = clean.IndexOf('?');
                if (qIndex > 0)
                {
                    return clean[..qIndex];
                }
            }

            // Với link dạng product/shopId/itemId
            if (clean.Contains("shopee.vn/product/", StringComparison.OrdinalIgnoreCase))
            {
                var qIndex = clean.IndexOf('?');
                if (qIndex > 0)
                {
                    return clean[..qIndex];
                }
            }
        }
        catch { }

        return clean;
    }

    /// <summary>
    /// Bóc tách tiêu đề chuẩn tiếng Việt và mã Shop/Item từ đường link Shopee
    /// </summary>
    public static (string Title, string ExternalId, string CleanUrl) ParseShopeeMetadata(string url)
    {
        var cleanUrl = SanitizeShopeeUrl(url);
        string title = "Set Quà Tặng Shopee Tinh Tế Tuyển Chọn";
        string externalId = $"SP_{Guid.NewGuid():N}"[..14];

        try
        {
            var uri = new Uri(cleanUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length > 0)
            {
                var rawSlug = segments[0];

                // Kiểm tra mã shopid và itemid dạng -i.{shopId}.{itemId}
                var idMatch = Regex.Match(rawSlug, @"-i\.(\d+)\.(\d+)", RegexOptions.IgnoreCase);
                if (idMatch.Success)
                {
                    var shopId = idMatch.Groups[1].Value;
                    var itemId = idMatch.Groups[2].Value;
                    externalId = $"SP_{shopId}_{itemId}";
                }
                else if (segments.Length >= 3 && segments[0].Equals("product", StringComparison.OrdinalIgnoreCase))
                {
                    // Dạng /product/{shopId}/{itemId}
                    externalId = $"SP_{segments[1]}_{segments[2]}";
                }

                // Giải mã URL tiếng Việt có dấu
                var decodedSlug = WebUtility.UrlDecode(rawSlug);

                // Loại bỏ phần đuôi -i.shopid.itemid
                var cleanSlug = Regex.Replace(decodedSlug, @"-i\.\d+\.\d+.*$", "", RegexOptions.IgnoreCase);
                cleanSlug = cleanSlug.Replace("-", " ").Replace("_", " ").Trim();
                cleanSlug = Regex.Replace(cleanSlug, @"\s+", " ");

                if (cleanSlug.Length > 90)
                {
                    var cutoff = cleanSlug.LastIndexOf(' ', 90);
                    cleanSlug = cutoff > 40 ? cleanSlug[..cutoff] : cleanSlug[..90];
                }

                if (!string.IsNullOrWhiteSpace(cleanSlug) && 
                    !cleanSlug.Equals("product", StringComparison.OrdinalIgnoreCase) &&
                    !cleanSlug.StartsWith("opaanlp", StringComparison.OrdinalIgnoreCase))
                {
                    title = char.ToUpper(cleanSlug[0]) + cleanSlug[1..];
                }
            }
        }
        catch { }

        return (title, externalId, cleanUrl);
    }
}
