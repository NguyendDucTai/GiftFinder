using FluentValidation;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Auth.DTOs;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Auth.Commands.RegisterMerchantAccount;

public class RegisterMerchantAccountCommand : IRequest<AuthResult>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? BusinessLicense { get; set; }
}

public class RegisterMerchantAccountCommandValidator : AbstractValidator<RegisterMerchantAccountCommand>
{
    public RegisterMerchantAccountCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải từ 6 ký tự trở lên.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên người đại diện không được để trống.")
            .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

        RuleFor(x => x.ShopName)
            .NotEmpty().WithMessage("Tên gian hàng không được để trống.")
            .MaximumLength(100).WithMessage("Tên gian hàng không được vượt quá 100 ký tự.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("Số điện thoại liên hệ không được để trống.")
            .Matches(@"^[0-9+]{9,15}$").WithMessage("Số điện thoại không đúng định dạng.");
    }
}

public class RegisterMerchantAccountCommandHandler : IRequestHandler<RegisterMerchantAccountCommand, AuthResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterMerchantAccountCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResult> Handle(RegisterMerchantAccountCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _context.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new InvalidOperationException("Email này đã được sử dụng trên hệ thống.");
        }

        var normalizedShopName = request.ShopName.Trim();
        var isDuplicateShopName = await _context.Merchants
            .AnyAsync(m => m.ShopName.ToLower() == normalizedShopName.ToLower(), cancellationToken);

        if (isDuplicateShopName)
        {
            throw new InvalidOperationException($"Tên gian hàng '{normalizedShopName}' đã được sử dụng. Vui lòng chọn tên khác.");
        }

        var hash = _passwordHasher.HashPassword(request.Password);

        // Tạo User với vai trò Merchant
        var user = new User(normalizedEmail, hash, request.FullName.Trim(), UserRole.Merchant);
        _context.Users.Add(user);

        // Tạo hồ sơ Merchant tương ứng
        var merchant = new Merchant(
            user.Id,
            normalizedShopName,
            normalizedEmail,
            request.ContactPhone.Trim(),
            request.BusinessLicense?.Trim()
        );
        _context.Merchants.Add(merchant);

        await _context.SaveChangesAsync(cancellationToken);

        // Sinh JWT Token mang role Merchant
        var token = _jwtTokenGenerator.GenerateToken(
            user.Id.ToString(),
            user.Email,
            user.FullName,
            user.Role.ToString());

        return new AuthResult(
            user.Id.ToString(),
            user.Email,
            user.FullName,
            user.Role.ToString(),
            token);
    }
}
