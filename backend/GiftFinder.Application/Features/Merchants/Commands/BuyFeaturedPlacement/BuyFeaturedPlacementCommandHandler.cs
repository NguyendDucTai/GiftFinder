using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Commands.BuyFeaturedPlacement;

public class BuyFeaturedPlacementCommandHandler : IRequestHandler<BuyFeaturedPlacementCommand, BuyFeaturedPlacementResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IVnPayService _vnPayService;

    public BuyFeaturedPlacementCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IVnPayService vnPayService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _vnPayService = vnPayService;
    }

    public async Task<BuyFeaturedPlacementResponseDto> Handle(BuyFeaturedPlacementCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để mua gói hiển thị nổi bật.");
        }

        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin gian hàng.");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin sản phẩm.");
        }

        if (product.MerchantId != merchant.Id)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên sản phẩm này.");
        }

        // BR-24: Chỉ sản phẩm đã ở trạng thái "Đã duyệt" mới được mua gói hiển thị nổi bật
        if (product.Status != ProductStatus.Approved)
        {
            throw new InvalidOperationException("Sản phẩm chưa được duyệt, vui lòng chờ Admin xét duyệt trước khi mua gói hiển thị nổi bật.");
        }

        // BR-23: Biểu phí gói Featured Placement (Weekly: 49.000đ, Monthly: 199.000đ)
        var amount = request.PlanType == FeaturedPlanType.Weekly ? 49_000L : 199_000L;
        var planTitle = request.PlanType == FeaturedPlanType.Weekly ? "Gói Tuần (7 ngày)" : "Gói Tháng (30 ngày)";
        var description = $"Mua gói Featured ({planTitle}) cho sản phẩm '{product.Name}'";

        // Khởi tạo Transaction trạng thái Pending với phương thức thanh toán VNPay
        var transaction = Transaction.CreateFeaturedPlacement(
            userId: currentUserId.Value,
            productId: product.Id,
            planType: request.PlanType,
            amount: amount,
            description: description,
            paymentMethod: "VNPay"
        );

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        // Sinh link thanh toán Cổng VNPAY Sandbox chuẩn HMAC-SHA512
        var orderInfo = $"GiftFinder - Featured {request.PlanType} ({product.Name})";
        if (orderInfo.Length > 90) orderInfo = orderInfo[..87] + "...";
        var paymentUrl = _vnPayService.CreatePaymentUrl(transaction.Id, amount, orderInfo, request.IpAddress ?? "127.0.0.1");

        return new BuyFeaturedPlacementResponseDto
        {
            TransactionId = transaction.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            PlanType = request.PlanType.ToString(),
            Amount = amount,
            PaymentMethod = "VNPay",
            PaymentUrl = paymentUrl,
            IsFeatured = product.IsFeatured,
            FeaturedUntil = product.FeaturedUntil,
            Message = $"Khởi tạo giao dịch mua gói đẩy Top ({planTitle}) thành công ({amount:N0} VNĐ). Vui lòng chuyển hướng sang cổng thanh toán VNPAY để kích hoạt dịch vụ."
        };
    }
}
