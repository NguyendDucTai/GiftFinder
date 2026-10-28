namespace GiftFinder.Application.Features.Merchants.DTOs;

public class MerchantProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? BusinessLicense { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MerchantProductDto
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
    public string? RejectReason { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? FeaturedUntil { get; set; }
    public List<string> SuitableZodiacs { get; set; } = new();
    public List<Guid> TagIds { get; set; } = new();
    public List<string> TagNames { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class MerchantProductListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; }
    public long? OriginalPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectReason { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> TagNames { get; set; } = new();
}

public class PagedResult<T>
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = new();
}

public class AiProductTagSuggestionDto
{
    public string ProductName { get; set; } = string.Empty;
    public List<string> SuitableZodiacs { get; set; } = new();
    public string ZodiacRationale { get; set; } = string.Empty;
    public List<SuggestedTagItemDto> SuggestedTags { get; set; } = new();
    public List<Guid> SuggestedTagIds { get; set; } = new();
}

public class SuggestedTagItemDto
{
    public Guid TagId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class MerchantDashboardStatsDto
{
    public string ShopName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public int TotalProducts { get; set; }
    public int ApprovedProducts { get; set; }
    public int PendingProducts { get; set; }
    public int RejectedProducts { get; set; }
    public int FeaturedProducts { get; set; }
    public int TotalClicks { get; set; }
    public List<MerchantTopProductDto> TopProducts { get; set; } = new();
}

public class MerchantTopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public long Price { get; set; }
    public int ClickCount { get; set; }
    public bool IsFeatured { get; set; }
}

public class BuyFeaturedPlacementResponseDto
{
    public Guid TransactionId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string PaymentMethod { get; set; } = "VNPay";
    public string PaymentUrl { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public DateTime? FeaturedUntil { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CancelFeaturedPlacementResponseDto
{
    public Guid ProductId { get; set; }
    public bool IsFeatured { get; set; }
    public string Message { get; set; } = string.Empty;
}

