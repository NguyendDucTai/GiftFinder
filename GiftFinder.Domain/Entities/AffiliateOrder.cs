using GiftFinder.Domain.Common;
using GiftFinder.Domain.Enums;

namespace GiftFinder.Domain.Entities;

public class AffiliateOrder : BaseAuditableEntity
{
    public ProductSource Source { get; private set; } = ProductSource.Shopee;
    public string ExternalOrderId { get; private set; } = default!;

    public Guid? ClickTrackingId { get; private set; }
    public ClickTracking? ClickTracking { get; private set; }

    public long OrderAmount { get; private set; }
    public long CommissionAmount { get; private set; }
    public AffiliateOrderStatus Status { get; private set; } = AffiliateOrderStatus.Pending;
    public DateTime OrderedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? CommissionPaidAt { get; private set; }

    // Dữ liệu đối soát hoa hồng hàng tuần (UC-11 - BR-17)
    public bool IsReconciled { get; private set; }
    public DateTime? ReconciledAt { get; private set; }
    public string? ReconciliationBatch { get; private set; }
    public string? ReconciliationNote { get; private set; }

    protected AffiliateOrder() { }

    public AffiliateOrder(
        ProductSource source,
        string externalOrderId,
        long orderAmount,
        long commissionAmount,
        Guid? clickTrackingId = null,
        DateTime? orderedAt = null)
    {
        if (string.IsNullOrWhiteSpace(externalOrderId))
            throw new ArgumentException("Mã đơn hàng ngoại vi (ExternalOrderId) không được để trống.");

        Source = source;
        ExternalOrderId = externalOrderId.Trim();
        OrderAmount = orderAmount;
        CommissionAmount = commissionAmount;
        ClickTrackingId = clickTrackingId;
        OrderedAt = orderedAt ?? DateTime.UtcNow;
        Status = AffiliateOrderStatus.Pending;
        IsReconciled = false;
    }

    public void Approve(DateTime approvedAt)
    {
        Status = AffiliateOrderStatus.Approved;
        ApprovedAt = approvedAt;
    }

    public void Reject()
    {
        Status = AffiliateOrderStatus.Rejected;
    }

    public void Cancel()
    {
        Status = AffiliateOrderStatus.Cancelled;
        CommissionAmount = 0;
    }

    public void MarkPaid(DateTime paidAt)
    {
        CommissionPaidAt = paidAt;
    }

    public void Reconcile(string batchCode, string? note = null)
    {
        IsReconciled = true;
        ReconciledAt = DateTime.UtcNow;
        ReconciliationBatch = batchCode.Trim();
        ReconciliationNote = note?.Trim();
    }
}