using FluentValidation;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Commands.CreateMerchantOrder;

public class CheckoutOrderItemInputDto
{
    public Guid? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string Variant { get; set; } = "Tiêu chuẩn";
    public long UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
}

public class CreateMerchantOrderCommand : IRequest<List<MerchantOrderDto>>
{
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string ReceiverProvince { get; set; } = "Hà Nội";
    public string ReceiverDistrict { get; set; } = "Quận Cầu Giấy";
    public string ReceiverAddress { get; set; } = string.Empty;
    public string? DeliveryNote { get; set; }

    public bool HasGiftWrap { get; set; }
    public string? GiftMessage { get; set; }
    public bool HidePriceOnPackage { get; set; }

    public string ShippingMethod { get; set; } = "Standard"; // "Standard" | "Express2H"
    public bool ApplyVoucher { get; set; }

    public List<CheckoutOrderItemInputDto> Items { get; set; } = new();
}

public class CreateMerchantOrderCommandValidator : AbstractValidator<CreateMerchantOrderCommand>
{
    public CreateMerchantOrderCommandValidator()
    {
        RuleFor(x => x.ReceiverName)
            .NotEmpty().WithMessage("Vui lòng nhập họ tên người nhận quà.")
            .MaximumLength(150);

        RuleFor(x => x.ReceiverPhone)
            .NotEmpty().WithMessage("Vui lòng nhập số điện thoại người nhận quà.")
            .Matches(@"^[0-9+\s\-\.]{9,15}$").WithMessage("Số điện thoại người nhận không hợp lệ.");

        RuleFor(x => x.ReceiverProvince)
            .NotEmpty().WithMessage("Vui lòng chọn Tỉnh/Thành phố nhận quà.");

        RuleFor(x => x.ReceiverAddress)
            .NotEmpty().WithMessage("Vui lòng nhập địa chỉ chi tiết nhận quà.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Giỏ hàng của bạn đang trống.");
    }
}

public class CreateMerchantOrderCommandHandler : IRequestHandler<CreateMerchantOrderCommand, List<MerchantOrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateMerchantOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private static string NormalizeProvince(string? province)
    {
        if (string.IsNullOrWhiteSpace(province)) return "hà nội";
        var p = province.Trim().ToLowerInvariant()
            .Replace("tp.", "")
            .Replace("thành phố", "")
            .Replace("tỉnh", "")
            .Trim();
        if (p.Contains("hồ chí minh") || p.Contains("hcm") || p.Contains("sài gòn"))
            return "hồ chí minh";
        if (p.Contains("hà nội") || p.Contains("ha noi"))
            return "hà nội";
        if (p.Contains("đà nẵng") || p.Contains("da nang"))
            return "đà nẵng";
        return p;
    }

    public async Task<List<MerchantOrderDto>> Handle(CreateMerchantOrderCommand request, CancellationToken cancellationToken)
    {
        var buyerUserId = _currentUserService.UserId;

        var productIds = request.Items
            .Where(i => i.ProductId.HasValue && i.ProductId.Value != Guid.Empty)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        var dbProducts = await _context.Products
            .Include(p => p.Merchant)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Fallback gian hàng mặc định (ví dụ Tiệm Gốm Mộc) nếu sản phẩm demo chưa gán MerchantId
        var defaultMerchant = await _context.Merchants
            .OrderByDescending(m => m.IsVerified)
            .ThenBy(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultMerchant == null)
        {
            throw new InvalidOperationException("Hiện chưa có gian hàng đối tác nào hoạt động trên hệ thống.");
        }

        // Nhóm các sản phẩm theo từng gian hàng (MerchantId)
        var groupedByMerchant = new Dictionary<Guid, (Merchant Shop, List<(CheckoutOrderItemInputDto Input, Product? DbProd)> Lines)>();

        foreach (var item in request.Items)
        {
            Product? dbProd = null;
            if (item.ProductId.HasValue && dbProducts.TryGetValue(item.ProductId.Value, out var found))
            {
                dbProd = found;
            }
            else if (!string.IsNullOrWhiteSpace(item.ProductName))
            {
                dbProd = await _context.Products
                    .Include(p => p.Merchant)
                    .FirstOrDefaultAsync(p => p.Source == ProductSource.DirectMerchant && p.Name.ToLower() == item.ProductName.Trim().ToLower(), cancellationToken);
            }

            var targetMerchant = dbProd?.Merchant ?? defaultMerchant;
            if (!groupedByMerchant.TryGetValue(targetMerchant.Id, out var group))
            {
                group = (targetMerchant, new List<(CheckoutOrderItemInputDto, Product?)>());
                groupedByMerchant[targetMerchant.Id] = group;
            }
            group.Lines.Add((item, dbProd));
        }

        var isExpressRequested = string.Equals(request.ShippingMethod, "Express2H", StringComparison.OrdinalIgnoreCase);
        var receiverProvNorm = NormalizeProvince(request.ReceiverProvince);

        var createdOrders = new List<MerchantOrder>();
        var resultDtos = new List<MerchantOrderDto>();

        foreach (var kvp in groupedByMerchant)
        {
            var shop = kvp.Value.Shop;
            var lines = kvp.Value.Lines;

            var shopProvNorm = NormalizeProvince(shop.PickupProvince);
            var isSameProvince = string.Equals(shopProvNorm, receiverProvNorm, StringComparison.OrdinalIgnoreCase);

            if (isExpressRequested && !isSameProvince)
            {
                throw new InvalidOperationException(
                    $"Gian hàng '{shop.ShopName}' có kho tại {shop.PickupProvince}, không hỗ trợ Giao Hỏa Tốc 2h đến {request.ReceiverProvince}. Vui lòng chọn Giao Tiêu Chuẩn Liên Tỉnh.");
            }

            long subtotal = 0;
            foreach (var (input, dbProd) in lines)
            {
                var qty = Math.Max(1, input.Quantity);
                var unitPrice = dbProd?.Price ?? Math.Max(1000, input.UnitPrice);

                if (dbProd != null)
                {
                    if (dbProd.StockQuantity < qty)
                    {
                        throw new InvalidOperationException(
                            $"Sản phẩm '{dbProd.Name}' chỉ còn {dbProd.StockQuantity} món trong kho của shop {shop.ShopName}.");
                    }
                    dbProd.DeductStock(qty);
                    dbProd.IncreasePopularityScore(15);
                }

                subtotal += unitPrice * qty;
            }

            var shippingMethodEnum = isExpressRequested && isSameProvince
                ? ShippingMethodType.Express2H
                : ShippingMethodType.Standard;

            // Tính phí vận chuyển chuẩn thực tế:
            // - Hỏa tốc 2h nội thành (cùng tỉnh): 50.000đ
            // - Tiêu chuẩn nội tỉnh (cùng tỉnh): 25.000đ (Miễn phí cho đơn từ 500.000đ)
            // - Tiêu chuẩn liên tỉnh (khác tỉnh): 35.000đ (Miễn phí cho đơn từ 500.000đ)
            long shippingFee;
            if (shippingMethodEnum == ShippingMethodType.Express2H)
            {
                shippingFee = 50000;
            }
            else
            {
                shippingFee = subtotal >= 500000 ? 0 : (isSameProvince ? 25000 : 35000);
            }

            long giftWrapFee = request.HasGiftWrap ? 30000 : 0;
            long discountAmount = request.ApplyVoucher ? 50000 : 0;

            var orderCode = $"GF-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}";

            var order = new MerchantOrder(
                orderCode: orderCode,
                merchantId: shop.Id,
                buyerUserId: buyerUserId,
                receiverName: request.ReceiverName,
                receiverPhone: request.ReceiverPhone,
                receiverProvince: request.ReceiverProvince,
                receiverDistrict: request.ReceiverDistrict,
                receiverAddress: request.ReceiverAddress,
                deliveryNote: request.DeliveryNote,
                hasGiftWrap: request.HasGiftWrap,
                giftMessage: request.GiftMessage,
                hidePriceOnPackage: request.HidePriceOnPackage,
                subtotal: subtotal,
                shippingFee: shippingFee,
                giftWrapFee: giftWrapFee,
                discountAmount: discountAmount,
                shippingMethod: shippingMethodEnum
            );

            foreach (var (input, dbProd) in lines)
            {
                var prodId = dbProd?.Id ?? defaultMerchant.Id;
                // Nếu prodId không nằm trong bảng Products, lấy sản phẩm đầu tiên của shop để đảm bảo FK
                if (dbProd == null)
                {
                    var fallbackProd = await _context.Products
                        .FirstOrDefaultAsync(p => p.MerchantId == shop.Id, cancellationToken)
                        ?? await _context.Products.FirstAsync(cancellationToken);
                    prodId = fallbackProd.Id;
                }

                order.AddItem(
                    productId: prodId,
                    productName: dbProd?.Name ?? input.ProductName,
                    productImageUrl: dbProd?.ImageUrl ?? input.ProductImageUrl,
                    unitPrice: dbProd?.Price ?? input.UnitPrice,
                    quantity: Math.Max(1, input.Quantity),
                    variant: input.Variant
                );
            }

            _context.MerchantOrders.Add(order);
            createdOrders.Add(order);
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var o in createdOrders)
        {
            var shop = groupedByMerchant[o.MerchantId].Shop;
            resultDtos.Add(new MerchantOrderDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                MerchantId = o.MerchantId,
                ShopName = shop.ShopName,
                ShopPickupProvince = shop.PickupProvince,
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
            });
        }

        return resultDtos;
    }
}

