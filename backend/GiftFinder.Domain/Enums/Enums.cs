namespace GiftFinder.Domain.Enums;

public enum UserRole
{
    Admin = 0,
    Member = 1,
    Merchant = 2
}

public enum MembershipTier
{
    Free = 0,
    Premium = 1
}

public enum ProductStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Inactive = 3
}

public enum ProductSource
{
    Shopee = 0,
    TikTokShop = 1,
    Lazada = 2,
    Tiki = 3,
    DirectMerchant = 4
}

public enum FeaturedPlanType
{
    Weekly = 0,   // 49.000đ/tuần (UC-17)
    Monthly = 1   // 199.000đ/tháng (UC-17)
}

public enum TagType
{
    Recipient = 0,  // Bạn gái, Bạn trai, Mẹ, Bố, Sếp... (UC-01)
    Occasion = 1,   // Sinh nhật, 20/10, Valentine, Giáng sinh... (UC-01, UC-07)
    Hobby = 2,      // Công nghệ, Decor, Thời trang, Nấu ăn... (UC-01)
    Category = 3,   // Đồ gia dụng, Phụ kiện, Mỹ phẩm... (UC-07)
    Budget = 4      // Dưới 200k, 200k-500k, 500k-1tr, Trên 1tr... (UC-01)
}

public enum ClickStatus
{
    Clicked = 0,
    Converted = 1,
    Dropped = 2
}

public enum AffiliateOrderStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

public enum TransactionType
{
    PremiumSubscription = 0,  // Thanh toán gói Premium 29k/tháng (UC-24)
    FeaturedPlacement = 1     // Thanh toán gói hiển thị nổi bật 49k/199k (UC-25)
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}

public enum NotificationType
{
    System = 0,
    Reminder = 1,       // Nhắc lịch kỷ niệm/sinh nhật (UC-21)
    PriceDrop = 2,      // Thông báo giảm giá món trong Wishlist (UC-22)
    RenewalAlert = 3    // Thông báo gia hạn Premium/Featured (UC-23)
}

public enum MerchantOrderStatus
{
    Pending = 0,        // Chờ Shop xác nhận
    Packing = 1,        // Đang gói quà & viết thiệp
    Shipping = 2,       // Đang giao hàng (Shipper đang giao)
    Completed = 3,      // Đã giao thành công
    Cancelled = 4       // Đã hủy
}

public enum ShippingMethodType
{
    Standard = 0,       // Giao tiêu chuẩn (Nội tỉnh 25k / Liên tỉnh 35k)
    Express2H = 1       // Giao hỏa tốc nội thành 2h (50k - Chỉ mở khi cùng Tỉnh/Thành)
}
