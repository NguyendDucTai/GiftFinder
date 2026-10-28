namespace GiftFinder.Application.Features.Subscriptions.DTOs;

public class SubscriptionStatusDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Tier { get; set; } = "Free";
    public bool IsPremiumActive { get; set; }
    public DateTime? PremiumExpiresAt { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsInGracePeriod { get; set; }
    public DateTime? GracePeriodUntil { get; set; }
    public bool AutoRenew { get; set; }
    public List<string> Benefits { get; set; } = new();
}

public class CreateSubscriptionPaymentResponseDto
{
    public Guid TransactionId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int Months { get; set; }
    public long Amount { get; set; }
    public string PaymentMethod { get; set; } = "VNPay";
    public string PaymentUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PaymentWebhookPayload
{
    public Guid? TransactionId { get; set; }
    public string? OrderCode { get; set; }
    public long Amount { get; set; }
    public string? Status { get; set; } = "PAID";
    public string? GatewayTransactionId { get; set; }
    public string? Signature { get; set; }
    public Dictionary<string, string>? RawParams { get; set; }
}

public class PaymentWebhookResultDto
{
    public bool Success { get; set; }
    public string RspCode { get; set; } = "00";
    public string Message { get; set; } = string.Empty;
    public Guid? TransactionId { get; set; }
    public string? Status { get; set; }
}
