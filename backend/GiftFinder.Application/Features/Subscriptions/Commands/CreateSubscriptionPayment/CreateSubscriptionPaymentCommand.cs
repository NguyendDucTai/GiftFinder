using FluentValidation;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Subscriptions.DTOs;
using GiftFinder.Domain.Constants;
using GiftFinder.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Subscriptions.Commands.CreateSubscriptionPayment;

public class CreateSubscriptionPaymentCommand : IRequest<CreateSubscriptionPaymentResponseDto>
{
    public int Months { get; set; } = 1; // 1, 3 hoặc 12 tháng
    public bool AutoRenew { get; set; } = true;
    public string? IpAddress { get; set; }
}

public class CreateSubscriptionPaymentCommandValidator : AbstractValidator<CreateSubscriptionPaymentCommand>
{
    public CreateSubscriptionPaymentCommandValidator()
    {
        RuleFor(x => x.Months)
            .Must(m => m == 1 || m == 3 || m == 12)
            .WithMessage("Kỳ hạn gói Premium phải là 1 tháng, 3 tháng hoặc 12 tháng.");
    }
}

public class CreateSubscriptionPaymentCommandHandler : IRequestHandler<CreateSubscriptionPaymentCommand, CreateSubscriptionPaymentResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IVnPayService _vnPayService;

    public CreateSubscriptionPaymentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IVnPayService vnPayService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _vnPayService = vnPayService;
    }

    public async Task<CreateSubscriptionPaymentResponseDto> Handle(CreateSubscriptionPaymentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập để nâng cấp gói thành viên Premium.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin tài khoản người dùng.");
        }

        // Tính toán số tiền theo kỳ hạn (BR-19: 15.000đ/tháng)
        var amount = SubscriptionPricing.GetPrice(request.Months);
        var planName = SubscriptionPricing.GetPlanName(request.Months);
        var description = $"Nâng cấp {planName} ({request.Months} tháng) cho {user.Email}";

        // Tạo bản ghi giao dịch chờ thanh toán (Pending)
        var transaction = Transaction.CreatePremiumSubscription(
            userId: user.Id,
            amount: amount,
            description: description,
            paymentMethod: "VNPay"
        );

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        // Sinh link thanh toán Cổng VNPAY Sandbox chuẩn HMAC-SHA512
        var orderInfo = $"GiftFinder - Nang cap {planName} ({request.Months} thang)";
        var paymentUrl = _vnPayService.CreatePaymentUrl(transaction.Id, amount, orderInfo, request.IpAddress ?? "127.0.0.1");

        return new CreateSubscriptionPaymentResponseDto
        {
            TransactionId = transaction.Id,
            PlanName = planName,
            Months = request.Months,
            Amount = amount,
            PaymentMethod = "VNPay",
            PaymentUrl = paymentUrl,
            CreatedAt = transaction.CreatedAt,
            Message = $"Khởi tạo giao dịch nâng cấp gói {planName} thành công ({amount:N0} VNĐ). Vui lòng chuyển hướng sang cổng thanh toán VNPAY để hoàn tất giao dịch."
        };
    }
}
