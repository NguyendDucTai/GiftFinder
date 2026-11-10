using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Commands.UpdateMerchantOrderStatus;

public class UpdateMerchantOrderStatusCommand : IRequest<MerchantOrderDto>
{
    public Guid OrderId { get; set; }
    public string TargetStatus { get; set; } = string.Empty; // "Packing" | "Shipping" | "Completed" | "Cancelled"
    public string? ShippingCarrier { get; set; }
    public string? TrackingCode { get; set; }
    public string? CancelReason { get; set; }
}

public class UpdateMerchantOrderStatusCommandHandler : IRequestHandler<UpdateMerchantOrderStatusCommand, MerchantOrderDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMerchantOrderStatusCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MerchantOrderDto> Handle(UpdateMerchantOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để cập nhật trạng thái đơn hàng.");
        }

        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin gian hàng.");
        }

        var order = await _context.MerchantOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.MerchantId == merchant.Id, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException("Không tìm thấy đơn hàng trong gian hàng của bạn.");
        }

        var target = request.TargetStatus.Trim();

        if (string.Equals(target, "Packing", StringComparison.OrdinalIgnoreCase))
        {
            order.ConfirmAndPack();
        }
        else if (string.Equals(target, "Shipping", StringComparison.OrdinalIgnoreCase))
        {
            order.MarkAsShipping(request.ShippingCarrier ?? "Giao Hàng Nhanh (GHN)", request.TrackingCode ?? string.Empty);
        }
        else if (string.Equals(target, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            order.MarkAsCompleted();
        }
        else if (string.Equals(target, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            if (order.Status != MerchantOrderStatus.Cancelled)
            {
                // Hoàn lại số lượng tồn kho khi hủy đơn
                var prodIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => prodIds.Contains(p.Id))
                    .ToListAsync(cancellationToken);

                foreach (var item in order.Items)
                {
                    var prod = products.FirstOrDefault(p => p.Id == item.ProductId);
                    prod?.RestoreStock(item.Quantity);
                }
            }
            order.Cancel(request.CancelReason);
        }
        else
        {
            throw new ArgumentException($"Trạng thái đơn hàng '{request.TargetStatus}' không hợp lệ.");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new MerchantOrderDto
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            MerchantId = order.MerchantId,
            ShopName = merchant.ShopName,
            ShopPickupProvince = merchant.PickupProvince,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = order.ReceiverPhone,
            ReceiverProvince = order.ReceiverProvince,
            ReceiverDistrict = order.ReceiverDistrict,
            ReceiverAddress = order.ReceiverAddress,
            DeliveryNote = order.DeliveryNote,
            HasGiftWrap = order.HasGiftWrap,
            GiftMessage = order.GiftMessage,
            HidePriceOnPackage = order.HidePriceOnPackage,
            Subtotal = order.Subtotal,
            ShippingFee = order.ShippingFee,
            GiftWrapFee = order.GiftWrapFee,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            PaymentMethod = order.PaymentMethod,
            ShippingMethod = order.ShippingMethod.ToString(),
            ShippingCarrier = order.ShippingCarrier,
            TrackingCode = order.TrackingCode,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            PackedAt = order.PackedAt,
            ShippedAt = order.ShippedAt,
            CompletedAt = order.CompletedAt,
            CancelReason = order.CancelReason,
            Items = order.Items.Select(i => new MerchantOrderItemDto
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
        };
    }
}

