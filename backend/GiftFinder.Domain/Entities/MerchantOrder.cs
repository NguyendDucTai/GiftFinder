using GiftFinder.Domain.Common;
using GiftFinder.Domain.Enums;

namespace GiftFinder.Domain.Entities;

public class MerchantOrder : BaseAuditableEntity
{
    public string OrderCode { get; private set; } = default!;
    public Guid MerchantId { get; private set; }
    public Merchant Merchant { get; private set; } = default!;

    public Guid? BuyerUserId { get; private set; }
    public User? BuyerUser { get; private set; }

    // Thông tin người nhận quà
    public string ReceiverName { get; private set; } = default!;
    public string ReceiverPhone { get; private set; } = default!;
    public string ReceiverProvince { get; private set; } = default!;
    public string ReceiverDistrict { get; private set; } = default!;
    public string ReceiverAddress { get; private set; } = default!;
    public string? DeliveryNote { get; private set; }

    // Dịch vụ quà tặng đặc thù
    public bool HasGiftWrap { get; private set; }
    public string? GiftMessage { get; private set; }
    public bool HidePriceOnPackage { get; private set; }

    // Chi phí đơn hàng (VNĐ)
    public long Subtotal { get; private set; }
    public long ShippingFee { get; private set; }
    public long GiftWrapFee { get; private set; }
    public long DiscountAmount { get; private set; }
    public long TotalAmount { get; private set; }

    // Thanh toán & Vận chuyển (100% COD)
    public string PaymentMethod { get; private set; } = "COD";
    public ShippingMethodType ShippingMethod { get; private set; } = ShippingMethodType.Standard;
    public string? ShippingCarrier { get; private set; }
    public string? TrackingCode { get; private set; }

    public MerchantOrderStatus Status { get; private set; } = MerchantOrderStatus.Pending;
    public DateTime? PackedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? CancelReason { get; private set; }

    private readonly List<MerchantOrderItem> _items = new();
    public IReadOnlyCollection<MerchantOrderItem> Items => _items.AsReadOnly();

    protected MerchantOrder() { }

    public MerchantOrder(
        string orderCode,
        Guid merchantId,
        Guid? buyerUserId,
        string receiverName,
        string receiverPhone,
        string receiverProvince,
        string receiverDistrict,
        string receiverAddress,
        string? deliveryNote,
        bool hasGiftWrap,
        string? giftMessage,
        bool hidePriceOnPackage,
        long subtotal,
        long shippingFee,
        long giftWrapFee,
        long discountAmount,
        ShippingMethodType shippingMethod)
    {
        if (string.IsNullOrWhiteSpace(receiverName))
            throw new ArgumentException("Họ tên người nhận không được để trống.");
        if (string.IsNullOrWhiteSpace(receiverPhone))
            throw new ArgumentException("Số điện thoại người nhận không được để trống.");
        if (string.IsNullOrWhiteSpace(receiverAddress))
            throw new ArgumentException("Địa chỉ nhận quà không được để trống.");

        OrderCode = orderCode.Trim();
        MerchantId = merchantId;
        BuyerUserId = buyerUserId;
        ReceiverName = receiverName.Trim();
        ReceiverPhone = receiverPhone.Trim();
        ReceiverProvince = string.IsNullOrWhiteSpace(receiverProvince) ? "TP. Hồ Chí Minh" : receiverProvince.Trim();
        ReceiverDistrict = string.IsNullOrWhiteSpace(receiverDistrict) ? "Quận 1" : receiverDistrict.Trim();
        ReceiverAddress = receiverAddress.Trim();
        DeliveryNote = deliveryNote?.Trim();
        HasGiftWrap = hasGiftWrap;
        GiftMessage = giftMessage?.Trim();
        HidePriceOnPackage = hidePriceOnPackage;
        Subtotal = subtotal;
        ShippingFee = shippingFee;
        GiftWrapFee = giftWrapFee;
        DiscountAmount = discountAmount;
        TotalAmount = Math.Max(0, subtotal + shippingFee + giftWrapFee - discountAmount);
        PaymentMethod = "COD";
        ShippingMethod = shippingMethod;
        Status = MerchantOrderStatus.Pending;
    }

    public void AddItem(Guid productId, string productName, string? productImageUrl, long unitPrice, int quantity, string? variant = null)
    {
        _items.Add(new MerchantOrderItem(Id, productId, productName, productImageUrl, unitPrice, quantity, variant));
    }

    public void ConfirmAndPack()
    {
        Status = MerchantOrderStatus.Packing;
        PackedAt = DateTime.UtcNow;
    }

    public void MarkAsShipping(string shippingCarrier, string trackingCode)
    {
        Status = MerchantOrderStatus.Shipping;
        ShippingCarrier = string.IsNullOrWhiteSpace(shippingCarrier) ? "Giao Hàng Nhanh (GHN)" : shippingCarrier.Trim();
        TrackingCode = string.IsNullOrWhiteSpace(trackingCode)
            ? $"GHN-{DateTime.UtcNow:MMdd}-{Random.Shared.Next(1000, 9999)}"
            : trackingCode.Trim();
        ShippedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted(DateTime? completedAt = null)
    {
        Status = MerchantOrderStatus.Completed;
        CompletedAt = completedAt ?? DateTime.UtcNow;
        if (!ShippedAt.HasValue)
        {
            ShippedAt = CompletedAt.Value.AddHours(-6);
        }
    }

    public void Cancel(string? reason = null)
    {
        Status = MerchantOrderStatus.Cancelled;
        CancelReason = string.IsNullOrWhiteSpace(reason) ? "Hủy theo yêu cầu" : reason.Trim();
    }
}

