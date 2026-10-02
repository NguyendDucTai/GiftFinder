using GiftFinder.Domain.Common;
using GiftFinder.Domain.Enums;

namespace GiftFinder.Domain.Entities;

public class Transaction : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    public TransactionType Type { get; private set; } = TransactionType.PremiumSubscription;
    public long Amount { get; private set; }
    public string PaymentMethod { get; private set; } = "VNPay";
    public string? GatewayTransactionId { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string Description { get; private set; } = default!;

    // Dành cho giao dịch mua gói hiển thị nổi bật Featured Placement (UC-17, UC-25)
    public Guid? ProductId { get; private set; }
    public Product? Product { get; private set; }
    public FeaturedPlanType? FeaturedPlan { get; private set; }
    public DateTime? FeaturedStartDate { get; private set; }
    public DateTime? FeaturedEndDate { get; private set; }

    protected Transaction() { }

    // Constructor cho thanh toán gói Premium cá nhân (UC-24)
    public static Transaction CreatePremiumSubscription(Guid userId, long amount, string description, string paymentMethod = "VNPay")
    {
        return new Transaction
        {
            UserId = userId,
            Type = TransactionType.PremiumSubscription,
            Amount = amount,
            Description = description,
            PaymentMethod = paymentMethod,
            Status = PaymentStatus.Pending
        };
    }

    // Constructor cho Merchant mua gói Featured Placement (UC-17, UC-25)
    public static Transaction CreateFeaturedPlacement(
        Guid userId,
        Guid productId,
        FeaturedPlanType planType,
        long amount,
        string description,
        string paymentMethod = "VNPay")
    {
        var start = DateTime.UtcNow;
        var end = planType == FeaturedPlanType.Weekly ? start.AddDays(7) : start.AddMonths(1);

        return new Transaction
        {
            UserId = userId,
            Type = TransactionType.FeaturedPlacement,
            ProductId = productId,
            FeaturedPlan = planType,
            FeaturedStartDate = start,
            FeaturedEndDate = end,
            Amount = amount,
            Description = description,
            PaymentMethod = paymentMethod,
            Status = PaymentStatus.Pending
        };
    }

    public void Complete(string gatewayTransactionId)
    {
        GatewayTransactionId = gatewayTransactionId;
        Status = PaymentStatus.Completed;
    }

    public void Fail()
    {
        Status = PaymentStatus.Failed;
    }
}