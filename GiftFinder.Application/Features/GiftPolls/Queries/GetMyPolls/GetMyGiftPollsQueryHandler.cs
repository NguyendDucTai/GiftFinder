using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.GiftPolls.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.GiftPolls.Queries.GetMyPolls;

public class GetMyGiftPollsQueryHandler : IRequestHandler<GetMyGiftPollsQuery, List<GiftPollSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyGiftPollsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<GiftPollSummaryDto>> Handle(GetMyGiftPollsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập để xem danh sách bình chọn.");

        var polls = await _context.GiftPolls
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new GiftPollSummaryDto
            {
                Id = p.Id,
                ShareCode = p.ShareCode,
                Title = p.Title,
                CreatorName = p.CreatorName,
                Description = p.Description,
                ExpiresAt = p.ExpiresAt,
                IsActive = p.IsActive,
                TotalItems = p.PollItems.Count(i => !i.IsDeleted),
                TotalVotes = p.PollItems.Where(i => !i.IsDeleted).Sum(i => i.VoteCount),
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return polls;
    }
}
