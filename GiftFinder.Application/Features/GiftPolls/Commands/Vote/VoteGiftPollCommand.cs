using GiftFinder.Application.Features.GiftPolls.DTOs;
using MediatR;

namespace GiftFinder.Application.Features.GiftPolls.Commands.Vote;

public class VoteGiftPollCommand : IRequest<PublicGiftPollDto>
{
    public string ShareCode { get; set; } = default!;
    public Guid ProductId { get; set; }
    public string IpAddress { get; set; } = default!;
    public string? UserAgent { get; set; }
}
