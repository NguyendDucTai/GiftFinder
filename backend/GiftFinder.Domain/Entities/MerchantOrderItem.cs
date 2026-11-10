using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class MerchantOrderItem : BaseAuditableEntity
{
    public Guid MerchantOrderId { get; private set; }
    public MerchantOrder MerchantOrder { get; private set; } = default!;

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = default!;

    public string ProductName { get; private set; } = default!;
    public string? ProductImageUrl { get; private set; }
    public string Variant { get; private set; } = "Tiêu chuẩn";
    public long UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public long LineTotal { get; private set; }

    protected MerchantOrderItem() { }

    public MerchantOrderItem(
        Guid merchantOrderId,
        Guid productId,
        string productName,
        string? productImageUrl,
        long unitPrice,
        int quantity,
        string? variant = null)
    {
        MerchantOrderId = merchantOrderId;
        ProductId = productId;
        ProductName = productName.Trim();
        ProductImageUrl = productImageUrl?.Trim();
        UnitPrice = unitPrice;
        Quantity = Math.Max(1, quantity);
        Variant = string.IsNullOrWhiteSpace(variant) ? "Tiêu chuẩn" : variant.Trim();
        LineTotal = UnitPrice * Quantity;
    }
}

