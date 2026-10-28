using GiftFinder.Domain.Common;
using GiftFinder.Domain.Enums;

namespace GiftFinder.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public long Price { get; private set; }
    public long? OriginalPrice { get; private set; }
    public string? ImageUrl { get; private set; }
    public string OriginalUrl { get; private set; } = default!;
    public string? AffiliateUrl { get; private set; }
    public ProductSource Source { get; private set; } = ProductSource.Shopee;
    public string? ExternalProductId { get; private set; }

    public ProductStatus Status { get; private set; } = ProductStatus.Pending;
    public string? RejectReason { get; private set; }

    public Guid? MerchantId { get; private set; }
    public Merchant? Merchant { get; private set; }

    public bool IsFeatured { get; private set; }
    public DateTime? FeaturedUntil { get; private set; }

    // Tính năng mở rộng: Gợi ý theo Cung Hoàng Đạo (Tùy chọn)
    public List<ZodiacSign> SuitableZodiacs { get; private set; } = new();

    public double Rating { get; private set; }
    public int TotalReviews { get; private set; }

    // UC-04: Đánh giá ngầm (Implicit Tracking)
    public int PopularityScore { get; private set; } = 0;

    private readonly List<ProductTag> _productTags = new();
    public IReadOnlyCollection<ProductTag> ProductTags => _productTags.AsReadOnly();

    private readonly List<PriceHistory> _priceHistories = new();
    public IReadOnlyCollection<PriceHistory> PriceHistories => _priceHistories.AsReadOnly();

    protected Product() { }

    public Product(
        string name,
        long price,
        string originalUrl,
        ProductSource source,
        string? externalProductId = null,
        string? imageUrl = null,
        long? originalPrice = null,
        Guid? merchantId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên sản phẩm không được để trống.");
        if (price <= 0)
            throw new ArgumentException("Giá sản phẩm phải là số nguyên dương.");
        if (string.IsNullOrWhiteSpace(originalUrl))
            throw new ArgumentException("Đường dẫn gốc sản phẩm không được để trống.");

        Name = name.Trim();
        Price = price;
        OriginalPrice = originalPrice;
        OriginalUrl = originalUrl.Trim();
        Source = source;
        ExternalProductId = externalProductId?.Trim();
        ImageUrl = imageUrl;
        MerchantId = merchantId;
        Status = ProductStatus.Pending;
        IsFeatured = false;
    }

    public void SetAffiliateUrl(string affiliateUrl)
    {
        if (string.IsNullOrWhiteSpace(affiliateUrl))
            throw new ArgumentException("Affiliate URL không được để trống.");
        AffiliateUrl = affiliateUrl.Trim();
    }

    public void SetDescription(string? description)
    {
        Description = description?.Trim();
    }

    public void SetImageUrl(string? imageUrl)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl))
            ImageUrl = imageUrl.Trim();
    }

    public void SetOriginalPrice(long? originalPrice)
    {
        OriginalPrice = originalPrice;
    }

    public void SetRating(double rating, int totalReviews)
    {
        Rating = Math.Clamp(rating, 0.0, 5.0);
        TotalReviews = Math.Max(0, totalReviews);
    }

    public void SetSuitableZodiacs(IEnumerable<ZodiacSign> zodiacs)
    {
        SuitableZodiacs = zodiacs?.Distinct().ToList() ?? new List<ZodiacSign>();
    }

    public void Approve()
    {
        Status = ProductStatus.Approved;
        RejectReason = null;
    }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Lý do từ chối không được để trống.");

        Status = ProductStatus.Rejected;
        RejectReason = reason.Trim();
    }

    public void UpdateMerchantDetails(
        string name, 
        long price, 
        string originalUrl, 
        string? description = null, 
        string? imageUrl = null, 
        long? originalPrice = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên sản phẩm không được để trống.");
        if (price <= 0)
            throw new ArgumentException("Giá sản phẩm phải là số nguyên dương.");
        if (string.IsNullOrWhiteSpace(originalUrl))
            throw new ArgumentException("Đường dẫn sản phẩm không được để trống.");

        Name = name.Trim();
        Price = price;
        OriginalUrl = originalUrl.Trim();
        Description = description?.Trim();
        ImageUrl = imageUrl?.Trim();
        OriginalPrice = originalPrice;

        // Nếu sản phẩm đã duyệt trước đó, sau khi sửa thông tin sẽ tự động đưa về Pending để Admin duyệt lại (BR-03)
        if (Status == ProductStatus.Approved)
        {
            Status = ProductStatus.Pending;
            RejectReason = null;
        }
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
    }

    public void ActivateFeatured(FeaturedPlanType planType, DateTime from)
    {
        if (Status != ProductStatus.Approved)
            throw new InvalidOperationException(
                "Sản phẩm chưa được duyệt, vui lòng chờ Admin xét duyệt trước khi mua gói hiển thị nổi bật.");

        // AC-4 (UC-17): Cho phĂ©p gia háº¡n ná»‘i tiáº¿p náº¿u gĂ³i hiá»‡n táº¡i cĂ²n háº¡n
        var baseTime = (IsFeatured && FeaturedUntil.HasValue && FeaturedUntil.Value > from)
            ? FeaturedUntil.Value
            : from;

        IsFeatured = true;
        FeaturedUntil = planType == FeaturedPlanType.Weekly
            ? baseTime.AddDays(7)
            : baseTime.AddMonths(1);
    }

    public void DeactivateFeatured()
    {
        IsFeatured = false;
        FeaturedUntil = null;
    }

    public void UpdatePrice(long newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Giá mới phải là số nguyên dương.");

        if (newPrice != Price)
        {
            _priceHistories.Add(new PriceHistory(Id, Price, newPrice));
            Price = newPrice;
        }
    }

    public void AddTag(Guid tagId)
    {
        if (!_productTags.Any(pt => pt.TagId == tagId))
        {
            _productTags.Add(new ProductTag(Id, tagId));
        }
    }

    public void RemoveTag(Guid tagId)
    {
        var item = _productTags.FirstOrDefault(pt => pt.TagId == tagId);
        if (item != null)
        {
            _productTags.Remove(item);
        }
    }

    // UC-04: Cộng điểm tương tác ngầm (vd: Click Affiliate = +10, Thêm Wishlist = +10)
    public void IncreasePopularityScore(int points)
    {
        if (points > 0)
        {
            PopularityScore += points;
        }
    }

    public void DecreasePopularityScore(int points)
    {
        if (points > 0)
        {
            PopularityScore -= points;
            if (PopularityScore < 0) PopularityScore = 0; // Luôn đảm bảo không có điểm âm như đã hứa
        }
    }
}