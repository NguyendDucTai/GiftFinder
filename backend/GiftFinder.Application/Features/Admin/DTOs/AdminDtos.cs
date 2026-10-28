namespace GiftFinder.Application.Features.Admin.DTOs;

public class PendingProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long Price { get; set; }
    public long? OriginalPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Thông tin Merchant gửi duyệt
    public Guid? MerchantId { get; set; }
    public string? ShopName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }

    // Danh sách Tags
    public List<string> TagNames { get; set; } = new();
    public List<string> SuitableZodiacs { get; set; } = new();
}

public class PagedAdminResult<T>
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = new();
}
