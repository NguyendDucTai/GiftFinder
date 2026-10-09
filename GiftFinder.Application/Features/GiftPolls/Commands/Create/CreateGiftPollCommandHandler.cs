using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.GiftPolls.DTOs;
using GiftFinder.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.GiftPolls.Commands.Create;

public class CreateGiftPollCommandHandler : IRequestHandler<CreateGiftPollCommand, PublicGiftPollDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateGiftPollCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PublicGiftPollDto> Handle(CreateGiftPollCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập để tạo cuộc bình chọn quà tặng.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Vui lòng nhập chủ đề cuộc bình chọn.");

        var distinctProductIds = request.ProductIds.Distinct().ToList();
        if (distinctProductIds.Count < 1 || distinctProductIds.Count > 10)
            throw new ArgumentException("Số lượng quà trong cuộc bình chọn phải từ 1 đến tối đa 10 món.");

        // Kiểm tra sự tồn tại của các sản phẩm
        var products = await _context.Products
            .Where(p => distinctProductIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != distinctProductIds.Count)
            throw new ArgumentException("Một số sản phẩm được chọn không tồn tại trong hệ thống.");

        // Sinh mã ShareCode duy nhất gồm 8 ký tự
        string shareCode;
        do
        {
            shareCode = Guid.NewGuid().ToString("N")[..8].ToLowerInvariant();
        } while (await _context.GiftPolls.AnyAsync(p => p.ShareCode == shareCode, cancellationToken));

        // Lấy tên người tạo nếu người dùng không tự nhập
        var creatorName = request.CreatorName;
        if (string.IsNullOrWhiteSpace(creatorName))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            creatorName = user?.FullName;
        }

        var giftPoll = new GiftPoll(
            userId: userId,
            title: request.Title,
            creatorName: creatorName,
            description: request.Description,
            shareCode: shareCode
        );

        foreach (var productId in distinctProductIds)
        {
            giftPoll.AddItem(productId);
        }

        _context.GiftPolls.Add(giftPoll);
        await _context.SaveChangesAsync(cancellationToken);

        // Trả về DTO
        var itemsDto = products.Select(p => new GiftPollItemDto
        {
            Id = giftPoll.PollItems.First(i => i.ProductId == p.Id).Id,
            ProductId = p.Id,
            ProductName = p.Name,
            ImageUrl = p.ImageUrl,
            Price = p.Price,
            TargetUrl = !string.IsNullOrWhiteSpace(p.AffiliateUrl) ? p.AffiliateUrl : p.OriginalUrl,
            VoteCount = 0,
            VotePercentage = 0
        }).ToList();

        return new PublicGiftPollDto
        {
            Id = giftPoll.Id,
            ShareCode = giftPoll.ShareCode,
            Title = giftPoll.Title,
            CreatorName = giftPoll.CreatorName,
            Description = giftPoll.Description,
            ExpiresAt = giftPoll.ExpiresAt,
            TotalVotes = 0,
            UserVotedProductId = null,
            Items = itemsDto
        };
    }
}
