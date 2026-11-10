using FluentValidation;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Commands.RegisterMerchant;

public class RegisterMerchantCommand : IRequest<MerchantProfileDto>
{
    public string ShopName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? BusinessLicense { get; set; }
    public string? PickupProvince { get; set; }
    public string? PickupAddress { get; set; }
}

public class RegisterMerchantCommandValidator : AbstractValidator<RegisterMerchantCommand>
{
    public RegisterMerchantCommandValidator()
    {
        RuleFor(x => x.ShopName)
            .NotEmpty().WithMessage("Tên gian hàng không được để trống.")
            .MaximumLength(100).WithMessage("Tên gian hàng không được vượt quá 100 ký tự.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("Số điện thoại liên hệ không được để trống.")
            .Matches(@"^[0-9+]{9,15}$").WithMessage("Số điện thoại không đúng định dạng.");

        When(x => !string.IsNullOrWhiteSpace(x.ContactEmail), () =>
        {
            RuleFor(x => x.ContactEmail)
                .EmailAddress().WithMessage("Email liên hệ không hợp lệ.");
        });
    }
}

public class RegisterMerchantCommandHandler : IRequestHandler<RegisterMerchantCommand, MerchantProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterMerchantCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<MerchantProfileDto> Handle(RegisterMerchantCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để thực hiện đăng ký gian hàng.");
        }

        var user = await _context.Users
            .Include(u => u.MerchantProfile)
            .FirstOrDefaultAsync(u => u.Id == currentUserId.Value, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin tài khoản người dùng.");
        }

        if (user.MerchantProfile != null)
        {
            throw new InvalidOperationException("Tài khoản của bạn đã có gian hàng trên hệ thống.");
        }

        var normalizedShopName = request.ShopName.Trim();
        var isDuplicateShopName = await _context.Merchants
            .AnyAsync(m => m.ShopName.ToLower() == normalizedShopName.ToLower(), cancellationToken);

        if (isDuplicateShopName)
        {
            throw new InvalidOperationException($"Tên gian hàng '{normalizedShopName}' đã được sử dụng. Vui lòng chọn tên khác.");
        }

        var contactEmail = !string.IsNullOrWhiteSpace(request.ContactEmail)
            ? request.ContactEmail.Trim()
            : user.Email;

        var merchant = new Merchant(
            user.Id,
            normalizedShopName,
            contactEmail,
            request.ContactPhone.Trim(),
            request.BusinessLicense?.Trim(),
            request.PickupProvince?.Trim(),
            request.PickupAddress?.Trim()
        );

        _context.Merchants.Add(merchant);
        user.ChangeRole(UserRole.Merchant);

        await _context.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenGenerator.GenerateToken(
            user.Id.ToString(),
            user.Email,
            user.FullName,
            user.Role.ToString());

        return new MerchantProfileDto
        {
            Id = merchant.Id,
            UserId = merchant.UserId,
            ShopName = merchant.ShopName,
            ContactEmail = merchant.ContactEmail,
            ContactPhone = merchant.ContactPhone,
            BusinessLicense = merchant.BusinessLicense,
            PickupProvince = merchant.PickupProvince,
            PickupAddress = merchant.PickupAddress,
            IsVerified = merchant.IsVerified,
            VerifiedAt = merchant.VerifiedAt,
            CreatedAt = merchant.CreatedAt,
            Token = token
        };
    }
}
