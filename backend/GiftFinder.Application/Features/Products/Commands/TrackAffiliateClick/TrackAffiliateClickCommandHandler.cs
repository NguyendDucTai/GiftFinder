using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Products.Commands.TrackAffiliateClick;

public class TrackAffiliateClickCommandHandler : IRequestHandler<TrackAffiliateClickCommand, string>
{
    private readonly IApplicationDbContext _context;

    public TrackAffiliateClickCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(TrackAffiliateClickCommand request, CancellationToken cancellationToken)
    {
        // 1. Tìm Product
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product == null)
        {
            throw new ArgumentException("Sản phẩm không tồn tại.");
        }

        // Lấy link để chuyển hướng (Ưu tiên AffiliateUrl, nếu chưa cấu hình thì dùng OriginalUrl)
        string targetUrl = !string.IsNullOrWhiteSpace(product.AffiliateUrl) ? product.AffiliateUrl : product.OriginalUrl;

        // 2. Kiểm tra spam (Rate Limiting 24h)
        bool isSpam = false;
        if (!string.IsNullOrWhiteSpace(request.IpAddress))
        {
            var timeLimit = DateTime.UtcNow.AddHours(-24);
            isSpam = await _context.ClickTrackings
                .AnyAsync(c => c.ProductId == request.ProductId 
                            && c.IpAddress == request.IpAddress 
                            && c.ClickedAt >= timeLimit, 
                          cancellationToken);
        }

        // 3. Xử lý ghi nhận điểm và log
        if (!isSpam)
        {
            // Cộng điểm ngầm cho sản phẩm
            product.IncreasePopularityScore(10);

            // Sinh SubId (Dùng để gửi sang Shopee/TikTok tracking hoa hồng)
            string subId = Guid.NewGuid().ToString("N");

            // Lưu log click
            var clickTracking = new ClickTracking(
                productId: request.ProductId,
                subId: subId,
                trackingUrl: targetUrl,
                userId: request.UserId,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent
            );

            _context.ClickTrackings.Add(clickTracking);
            
            // Lưu xuống DB
            await _context.SaveChangesAsync(cancellationToken);
            
            // Chú ý: Ở hệ thống thật, targetUrl có thể cần gắn thêm tham số ?subId=... để truyền qua Shopee.
            // Nhưng hiện tại trả về link gốc/link Affiliate cơ bản để user chuyển hướng trước.
        }

        // Dù spam hay không, vẫn luôn trả về Link để người dùng đi mua hàng (Tuyệt đối không chặn luồng mua sắm)
        return targetUrl;
    }
}
