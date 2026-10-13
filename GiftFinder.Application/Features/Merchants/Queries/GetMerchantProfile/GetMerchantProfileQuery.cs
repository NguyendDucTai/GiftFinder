using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Queries.GetMerchantProfile;

public class GetMerchantProfileQuery : IRequest<MerchantProfileDto>
{
}

public class GetMerchantProfileQueryHandler : IRequestHandler<GetMerchantProfileQuery, MerchantProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMerchantProfileQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MerchantProfileDto> Handle(GetMerchantProfileQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem thông tin gian hàng.");
        }

        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin gian hàng của tài khoản này.");
        }

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
