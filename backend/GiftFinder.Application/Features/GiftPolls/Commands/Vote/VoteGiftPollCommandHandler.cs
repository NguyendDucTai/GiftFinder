using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.GiftPolls.DTOs;
using GiftFinder.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.GiftPolls.Commands.Vote;

public class VoteGiftPollCommandHandler : IRequestHandler<VoteGiftPollCommand, PublicGiftPollDto>
{
    private readonly IApplicationDbContext _context;

    public VoteGiftPollCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublicGiftPollDto> Handle(VoteGiftPollCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ShareCode))
            throw new ArgumentException("Mã chia sẻ không hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.IpAddress))
            throw new ArgumentException("Không xác định được địa chỉ IP của bạn.");

        var giftPoll = await _context.GiftPolls
            .Include(p => p.PollItems.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.ShareCode == request.ShareCode.Trim().ToLowerInvariant(), cancellationToken);

        if (giftPoll == null)
            throw new KeyNotFoundException("Không tìm thấy cuộc bình chọn.");

        if (DateTime.UtcNow > giftPoll.ExpiresAt)
            throw new InvalidOperationException("Liên kết chia sẻ này đã hết hạn.");

        if (!giftPoll.IsActive)
            throw new InvalidOperationException("Cuộc bình chọn này đã kết thúc.");

        var targetItem = giftPoll.PollItems.FirstOrDefault(i => i.ProductId == request.ProductId && !i.IsDeleted);
        if (targetItem == null)
            throw new ArgumentException("Món quà này không nằm trong danh sách bình chọn.");

        // Kiểm tra xem IP này đã vote chưa
        var existingVote = await _context.GiftPollVotes
            .FirstOrDefaultAsync(v => v.GiftPollId == giftPoll.Id && v.IpAddress == request.IpAddress, cancellationToken);

        Guid? currentVotedProductId = null;

        if (existingVote == null)
        {
            // 1. Chưa từng vote: Tạo vote mới
            var newVote = new GiftPollVote(giftPoll.Id, request.ProductId, request.IpAddress, request.UserAgent);
            _context.GiftPollVotes.Add(newVote);
            targetItem.IncrementVote();
            currentVotedProductId = request.ProductId;
        }
        else if (existingVote.ProductId == request.ProductId)
        {
            // 2. Đã vote chính món này: Bấm lần nữa để hủy vote (Un-vote)
            _context.GiftPollVotes.Remove(existingVote);
            targetItem.DecrementVote();
            currentVotedProductId = null;
        }
        else
        {
            // 3. Đang vote món khác: Chuyển phiếu bầu sang món mới
            var oldItem = giftPoll.PollItems.FirstOrDefault(i => i.ProductId == existingVote.ProductId && !i.IsDeleted);
            oldItem?.DecrementVote();

            existingVote.ChangeVote(request.ProductId);
            targetItem.IncrementVote();
            currentVotedProductId = request.ProductId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Trả về kết quả mới nhất sau khi vote
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
            UserVotedProductId = currentVotedProductId,
            Items = itemsDto
        };
    }
}
