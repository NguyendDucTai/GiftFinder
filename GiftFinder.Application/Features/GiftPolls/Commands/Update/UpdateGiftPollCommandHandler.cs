using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.GiftPolls.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.GiftPolls.Commands.Update;

public class UpdateGiftPollCommandHandler : IRequestHandler<UpdateGiftPollCommand, PublicGiftPollDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateGiftPollCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PublicGiftPollDto> Handle(UpdateGiftPollCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập để cập nhật cuộc bình chọn.");

        var giftPoll = await _context.GiftPolls
            .Include(p => p.PollItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (giftPoll == null)
            throw new ArgumentException("Cuộc bình chọn không tồn tại.");

        if (giftPoll.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa cuộc bình chọn này.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Tiêu đề cuộc bình chọn không được để trống.");

        var distinctProductIds = request.ProductIds.Distinct().ToList();
        if (distinctProductIds.Count < 1 || distinctProductIds.Count > 10)
            throw new ArgumentException("Số lượng quà trong cuộc bình chọn phải từ 1 đến tối đa 10 món.");

        // Kiểm tra tất cả các sản phẩm mới có tồn tại trong hệ thống không
        var validProductIds = await _context.Products
            .Where(p => distinctProductIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (validProductIds.Count != distinctProductIds.Count)
            throw new ArgumentException("Một số sản phẩm được chọn không tồn tại trong hệ thống.");

        // Cập nhật thông tin chung
        giftPoll.UpdateInfo(request.Title, request.CreatorName, request.Description);

        // Xử lý thêm mới và xóa bớt sản phẩm
        var currentActiveProductIds = giftPoll.PollItems
            .Where(i => !i.IsDeleted)
            .Select(i => i.ProductId)
            .ToList();

        // 1. Thêm các món mới
        var productsToAdd = distinctProductIds.Except(currentActiveProductIds).ToList();
        foreach (var prodId in productsToAdd)
        {
            giftPoll.AddItem(prodId);
        }

        // 2. Xóa các món không còn được chọn
        var productsToRemove = currentActiveProductIds.Except(distinctProductIds).ToList();
        foreach (var prodId in productsToRemove)
        {
            giftPoll.RemoveItem(prodId);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Lấy lại danh sách chi tiết sau khi cập nhật
        var activeItems = await _context.GiftPollItems
            .Include(i => i.Product)
            .Where(i => i.GiftPollId == giftPoll.Id && !i.IsDeleted)
            .ToListAsync(cancellationToken);

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
            UserVotedProductId = null,
            Items = itemsDto
        };
    }
}
