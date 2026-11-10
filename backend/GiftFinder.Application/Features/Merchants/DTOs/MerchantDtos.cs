namespace GiftFinder.Application.Features.Merchants.DTOs;

public class MerchantProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? BusinessLicense { get; set; }
    public string PickupProvince { get; set; } = "Hà Nội";
    public string? PickupAddress { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Token { get; set; }
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
    public int StockQuantity { get; set; } = 50;
    public bool IsFeatured { get; set; }
    public DateTime? FeaturedUntil { get; set; }
    public List<string> SuitableZodiacs { get; set; } = new();
    public List<Guid> TagIds { get; set; } = new();
    public List<string> TagNames { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string? Token { get; set; }
}

public class MerchantProductListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long Price { get; set; }
    public long? OriginalPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectReason { get; set; }
    public int StockQuantity { get; set; } = 50;
    public bool IsFeatured { get; set; }
    public DateTime? FeaturedUntil { get; set; }
    public int ClickCount { get; set; }
    public int SoldCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Guid> TagIds { get; set; } = new();
    public List<string> TagNames { get; set; } = new();
    public List<string> SuitableZodiacs { get; set; } = new();
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

public class MerchantDailyChartPointDto
{
    public string DateLabel { get; set; } = string.Empty; // dd/MM
    public string FullDate { get; set; } = string.Empty;  // yyyy-MM-dd
    public int Clicks { get; set; }
    public int Orders { get; set; }
    public long Revenue { get; set; }
}

public class MerchantDashboardStatsDto
{
    public Guid MerchantId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopSlug { get; set; } = string.Empty;
    public string PickupProvince { get; set; } = "Hà Nội";
    public string? PickupAddress { get; set; }
    public bool IsVerified { get; set; }
    public int TotalProducts { get; set; }
    public int ApprovedProducts { get; set; }
    public int PendingProducts { get; set; }
    public int RejectedProducts { get; set; }
    public int FeaturedProducts { get; set; }
    public int TotalClicks { get; set; }

    // Thống kê Đơn hàng & Doanh thu COD
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ShippingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public long CompletedRevenue { get; set; }
    public long PendingRevenue { get; set; }
    public long AverageOrderValue { get; set; }
    public double ConversionRate { get; set; } // % (Orders / Clicks)

    public List<MerchantDailyChartPointDto> DailyChart { get; set; } = new();
    public List<MerchantOrderDto> RecentOrders { get; set; } = new();
    public List<MerchantTopProductDto> TopProducts { get; set; } = new();
}

public class MerchantTopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public long Price { get; set; }
    public int ClickCount { get; set; }
    public int SoldCount { get; set; }
    public long Revenue { get; set; }
    public bool IsFeatured { get; set; }
}

public class MerchantOrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string Variant { get; set; } = "Tiêu chuẩn";
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long LineTotal { get; set; }
}

public class MerchantOrderDto
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid MerchantId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopPickupProvince { get; set; } = "Hà Nội";

    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string ReceiverProvince { get; set; } = string.Empty;
    public string ReceiverDistrict { get; set; } = string.Empty;
    public string ReceiverAddress { get; set; } = string.Empty;
    public string? DeliveryNote { get; set; }

    public bool HasGiftWrap { get; set; }
    public string? GiftMessage { get; set; }
    public bool HidePriceOnPackage { get; set; }

    public long Subtotal { get; set; }
    public long ShippingFee { get; set; }
    public long GiftWrapFee { get; set; }
    public long DiscountAmount { get; set; }
    public long TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = "COD";
    public string ShippingMethod { get; set; } = "Standard";
    public string? ShippingCarrier { get; set; }
    public string? TrackingCode { get; set; }

    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? PackedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CancelReason { get; set; }

    public List<MerchantOrderItemDto> Items { get; set; } = new();
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
