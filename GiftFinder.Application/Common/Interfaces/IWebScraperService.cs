namespace GiftFinder.Application.Common.Interfaces;

/// <summary>
/// Dữ liệu sản phẩm bóc tách thực tế từ trình duyệt ngầm Playwright
/// </summary>
public class ScrapedProductData
{
    public string Title { get; set; } = string.Empty;
    public long Price { get; set; }
    public long? OriginalPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public string? ExternalId { get; set; }
    public string? FinalUrl { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Dịch vụ cào dữ liệu ngầm thời gian thực bằng Playwright headless browser
/// </summary>
public interface IWebScraperService
{
    /// <summary>
    /// Bóc tách thông tin tên thật, giá thật, ảnh gốc từ URL sản phẩm
    /// </summary>
    Task<ScrapedProductData> ScrapeProductAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra xem URL có được hỗ trợ cào bởi dịch vụ này hay không
    /// </summary>
    bool SupportsUrl(string url);
}
