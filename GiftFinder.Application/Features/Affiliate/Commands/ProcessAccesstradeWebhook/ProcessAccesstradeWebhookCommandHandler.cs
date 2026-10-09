using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Application.Features.Affiliate.Commands.ProcessAccesstradeWebhook;

public class ProcessAccesstradeWebhookCommandHandler : IRequestHandler<ProcessAccesstradeWebhookCommand, WebhookProcessResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ProcessAccesstradeWebhookCommandHandler> _logger;

    public ProcessAccesstradeWebhookCommandHandler(
        IApplicationDbContext context, 
        ILogger<ProcessAccesstradeWebhookCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WebhookProcessResultDto> Handle(ProcessAccesstradeWebhookCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Payload;
        if (string.IsNullOrWhiteSpace(payload.OrderId))
        {
            return new WebhookProcessResultDto
            {
                Success = false,
                Message = "OrderId không được để trống."
            };
        }

        var orderId = payload.OrderId.Trim();

        // 1. Kiểm tra đơn hàng đã tồn tại trong DB chưa (Đảm bảo tính Idempotent)
        var existingOrder = await _context.AffiliateOrders
            .FirstOrDefaultAsync(o => o.ExternalOrderId == orderId, cancellationToken);

        if (existingOrder != null)
        {
            // Cập nhật trạng thái đơn hàng (sàn chuyển từ Pending sang Approved hoặc Cancelled)
            UpdateOrderStatus(existingOrder, payload.Status);
            await _context.SaveChangesAsync(cancellationToken);

            return new WebhookProcessResultDto
            {
                Success = true,
                OrderId = orderId,
                Status = existingOrder.Status.ToString(),
                CommissionAmount = existingOrder.CommissionAmount,
                MatchedClickTracking = existingOrder.ClickTrackingId.HasValue,
                Message = $"Đơn hàng {orderId} đã tồn tại. Cập nhật trạng thái thành: {existingOrder.Status}."
            };
        }

        // 2. Đối soát tìm ClickTracking tương ứng qua SubId
        ClickTracking? matchedTracking = null;
        if (!string.IsNullOrWhiteSpace(payload.SubId))
        {
            var subId = payload.SubId.Trim();
            matchedTracking = await _context.ClickTrackings
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.SubId == subId, cancellationToken);

            if (matchedTracking == null && Guid.TryParse(subId, out var trackingGuid))
            {
                matchedTracking = await _context.ClickTrackings
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.Id == trackingGuid, cancellationToken);
            }
        }

        // Nếu tìm thấy click tracking: đánh dấu đã chuyển đổi & cộng điểm nổi bật cho sản phẩm
        if (matchedTracking != null)
        {
            matchedTracking.MarkConverted();
            if (matchedTracking.Product != null)
            {
                matchedTracking.Product.IncreasePopularityScore(15);
            }
        }

        // 3. Xác định nguồn sàn TMĐT (Shopee hay TikTok Shop)
        var source = ProductSource.Shopee;
        var platformText = (payload.Platform ?? payload.CampaignName ?? "").ToLowerInvariant();
        if (platformText.Contains("tiktok"))
        {
            source = ProductSource.TikTokShop;
        }
        else if (platformText.Contains("lazada"))
        {
            source = ProductSource.Lazada;
        }
        else if (platformText.Contains("tiki"))
        {
            source = ProductSource.Tiki;
        }

        // 4. Tính toán hoa hồng (mặc định 8% nếu sàn chưa tính sẵn)
        var orderAmount = payload.OrderAmount > 0 ? payload.OrderAmount : 250000;
        var commissionAmount = payload.CommissionAmount > 0 
            ? payload.CommissionAmount 
            : (long)(orderAmount * 0.08);

        // 5. Khởi tạo đơn hàng Affiliate mới
        var newOrder = new AffiliateOrder(
            source,
            orderId,
            orderAmount,
            commissionAmount,
            matchedTracking?.Id,
            payload.OrderedAt ?? DateTime.UtcNow);

        UpdateOrderStatus(newOrder, payload.Status);

        _context.AffiliateOrders.Add(newOrder);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Ghi nhận thành công đơn hàng Affiliate {OrderId} từ sàn {Source}. Giá trị: {OrderAmount:N0}đ, Hoa hồng: {Commission:N0}đ. Matched SubId: {Matched}",
            orderId, source, orderAmount, commissionAmount, matchedTracking != null);

        return new WebhookProcessResultDto
        {
            Success = true,
            OrderId = orderId,
            Status = newOrder.Status.ToString(),
            CommissionAmount = commissionAmount,
            MatchedClickTracking = matchedTracking != null,
            Message = $"Ghi nhận thành công đơn hàng {orderId} từ sàn {source} với hoa hồng {commissionAmount:N0} VNĐ."
        };
    }

    private static void UpdateOrderStatus(AffiliateOrder order, string status)
    {
        var lower = (status ?? "").Trim().ToLowerInvariant();
        switch (lower)
        {
            case "approved":
            case "success":
                order.Approve(DateTime.UtcNow);
                break;
            case "rejected":
                order.Reject();
                break;
            case "cancelled":
            case "canceled":
                order.Cancel();
                break;
            default:
                // Giữ Pending
                break;
        }
    }
}
