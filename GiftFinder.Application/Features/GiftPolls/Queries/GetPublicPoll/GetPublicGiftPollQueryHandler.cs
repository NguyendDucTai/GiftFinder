using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.GiftPolls.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.GiftPolls.Queries.GetPublicPoll;

public class GetPublicGiftPollQueryHandler : IRequestHandler<GetPublicGiftPollQuery, PublicGiftPollDto>
{
    private readonly IApplicationDbContext _context;

    public GetPublicGiftPollQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublicGiftPollDto> Handle(GetPublicGiftPollQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ShareCode))
            throw new ArgumentException("Mã chia sẻ không hợp lệ.");

        var giftPoll = await _context.GiftPolls
            .Include(p => p.PollItems.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.ShareCode == request.ShareCode.Trim().ToLowerInvariant(), cancellationToken);

        if (giftPoll == null)
            throw new KeyNotFoundException("Không tìm thấy cuộc bình chọn.");

        // Kiểm tra hết hạn 30 ngày (BR-05)
        if (DateTime.UtcNow > giftPoll.ExpiresAt)
            throw new InvalidOperationException("Liên kết chia sẻ này đã hết hạn.");

        if (!giftPoll.IsActive)
            throw new InvalidOperationException("Cuộc bình chọn này đã kết thúc.");

        // Kiểm tra xem IP này đã từng vote món nào chưa
        Guid? userVotedProductId = null;
        if (!string.IsNullOrWhiteSpace(request.IpAddress))
        {
            var existingVote = await _context.GiftPollVotes
                .FirstOrDefaultAsync(v => v.GiftPollId == giftPoll.Id && v.IpAddress == request.IpAddress, cancellationToken);
            userVotedProductId = existingVote?.ProductId;
        }

        var activeItems = giftPoll.PollItems.Where(i => !i.IsDeleted).ToList();
        int totalVotes = activeItems.Sum(i => i.VoteCount);

        var itemsDto = activeItems.Select(i => new GiftPollItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            ImageUrl = i.Product.ImageUrl,
            Price = i.Product.Price,
            TargetUrl = !string.IsNullOrWhiteSpace(i.Product.AffiliateUrl) ? i.Product.AffiliateUrl : i.Product.OriginalUrl,
            VoteCount = i.VoteCount,
            VotePercentage = totalVotes > 0 ? Math.Round((double)i.VoteCount / totalVotes * 100, 1) : 0
        }).OrderByDescending(i => i.VoteCount).ToList();

        return new PublicGiftPollDto
        {
            Id = giftPoll.Id,
            ShareCode = giftPoll.ShareCode,
            Title = giftPoll.Title,
            CreatorName = giftPoll.CreatorName,
            Description = giftPoll.Description,
            ExpiresAt = giftPoll.ExpiresAt,
            TotalVotes = totalVotes,
            UserVotedProductId = userVotedProductId,
            Items = itemsDto
        };
    }
}
