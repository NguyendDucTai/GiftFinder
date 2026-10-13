using FluentValidation;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Commands.UpdateMerchantProfile;

public class UpdateMerchantProfileCommand : IRequest<MerchantProfileDto>
{
    public string ShopName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? BusinessLicense { get; set; }
}

public class UpdateMerchantProfileCommandValidator : AbstractValidator<UpdateMerchantProfileCommand>
{
    public UpdateMerchantProfileCommandValidator()
    {
        RuleFor(x => x.ShopName)
            .NotEmpty().WithMessage("Tên gian hàng không được để trống.")
            .MaximumLength(100).WithMessage("Tên gian hàng không được vượt quá 100 ký tự.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Email liên hệ không được để trống.")
            .EmailAddress().WithMessage("Email liên hệ không đúng định dạng.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("Số điện thoại liên hệ không được để trống.")
            .Matches(@"^[0-9+]{9,15}$").WithMessage("Số điện thoại không đúng định dạng.");
    }
}

public class UpdateMerchantProfileCommandHandler : IRequestHandler<UpdateMerchantProfileCommand, MerchantProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMerchantProfileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MerchantProfileDto> Handle(UpdateMerchantProfileCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để cập nhật thông tin gian hàng.");
        }

        var merchant = await _context.Merchants
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin gian hàng.");
        }

        var normalizedShopName = request.ShopName.Trim();
        if (!merchant.ShopName.Equals(normalizedShopName, StringComparison.OrdinalIgnoreCase))
        {
            var isDuplicateShopName = await _context.Merchants
                .AnyAsync(m => m.Id != merchant.Id && m.ShopName.ToLower() == normalizedShopName.ToLower(), cancellationToken);

            if (isDuplicateShopName)
            {
                throw new InvalidOperationException($"Tên gian hàng '{normalizedShopName}' đã được sử dụng. Vui lòng chọn tên khác.");
            }
        }

        merchant.UpdateProfile(
            normalizedShopName,
            request.ContactEmail.Trim(),
            request.ContactPhone.Trim(),
            request.BusinessLicense?.Trim()
        );

        await _context.SaveChangesAsync(cancellationToken);

        return new MerchantProfileDto
        {
            Id = merchant.Id,
            UserId = merchant.UserId,
            ShopName = merchant.ShopName,
            ContactEmail = merchant.ContactEmail,
            ContactPhone = merchant.ContactPhone,
            BusinessLicense = merchant.BusinessLicense,
            IsVerified = merchant.IsVerified,
            VerifiedAt = merchant.VerifiedAt,
            CreatedAt = merchant.CreatedAt
        };
    }
}
