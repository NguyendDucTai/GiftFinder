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
    public double Rating { get; private set; }
    public int TotalReviews { get; private set; }

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

    public void Approve()
    {
        if (_productTags.Count == 0)
            throw new InvalidOperationException(
                "Vui lòng chọn ít nhất một sở thích và một dịp tặng phù hợp cho sản phẩm trước khi duyệt.");

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

    public void ActivateFeatured(FeaturedPlanType planType, DateTime from)
    {
        if (Status != ProductStatus.Approved)
            throw new InvalidOperationException(
                "Sản phẩm chưa được duyệt, vui lòng chờ Admin xét duyệt trước khi mua gói hiển thị nổi bật.");

        IsFeatured = true;
        FeaturedUntil = planType == FeaturedPlanType.Weekly
            ? from.AddDays(7)
            : from.AddMonths(1);
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
}