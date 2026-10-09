using GiftFinder.Application.Features.GiftPolls.DTOs;
using MediatR;

namespace GiftFinder.Application.Features.GiftPolls.Queries.GetPublicPoll;

public class GetPublicGiftPollQuery : IRequest<PublicGiftPollDto>
{
    public string ShareCode { get; set; } = default!;
    public string? IpAddress { get; set; }

    public GetPublicGiftPollQuery(string shareCode, string? ipAddress = null)
    {
        ShareCode = shareCode;
        IpAddress = ipAddress;
    }
}
