using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Queries.GetMerchantOrders;

public class GetMerchantOrdersQuery : IRequest<List<MerchantOrderDto>>
{
    public MerchantOrderStatus? Status { get; set; }
}

public class GetMerchantOrdersQueryHandler : IRequestHandler<GetMerchantOrdersQuery, List<MerchantOrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMerchantOrdersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<MerchantOrderDto>> Handle(GetMerchantOrdersQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem danh sách đơn hàng.");
        }

        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin gian hàng.");
        }

        var query = _context.MerchantOrders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.MerchantId == merchant.Id);

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(o => new MerchantOrderDto
        {
            Id = o.Id,
            OrderCode = o.OrderCode,
            MerchantId = o.MerchantId,
            ShopName = merchant.ShopName,
            ShopPickupProvince = merchant.PickupProvince,
            ReceiverName = o.ReceiverName,
            ReceiverPhone = o.ReceiverPhone,
            ReceiverProvince = o.ReceiverProvince,
            ReceiverDistrict = o.ReceiverDistrict,
            ReceiverAddress = o.ReceiverAddress,
            DeliveryNote = o.DeliveryNote,
            HasGiftWrap = o.HasGiftWrap,
            GiftMessage = o.GiftMessage,
            HidePriceOnPackage = o.HidePriceOnPackage,
            Subtotal = o.Subtotal,
            ShippingFee = o.ShippingFee,
            GiftWrapFee = o.GiftWrapFee,
            DiscountAmount = o.DiscountAmount,
            TotalAmount = o.TotalAmount,
            PaymentMethod = o.PaymentMethod,
            ShippingMethod = o.ShippingMethod.ToString(),
            ShippingCarrier = o.ShippingCarrier,
            TrackingCode = o.TrackingCode,
            Status = o.Status.ToString(),
            CreatedAt = o.CreatedAt,
            PackedAt = o.PackedAt,
            ShippedAt = o.ShippedAt,
            CompletedAt = o.CompletedAt,
            CancelReason = o.CancelReason,
            Items = o.Items.Select(i => new MerchantOrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductImageUrl = i.ProductImageUrl,
                Variant = i.Variant,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal
            }).ToList()
        }).ToList();
    }
}

