using GiftFinder.Application.Features.GiftPolls.DTOs;
using MediatR;

namespace GiftFinder.Application.Features.GiftPolls.Queries.GetMyPolls;

public class GetMyGiftPollsQuery : IRequest<List<GiftPollSummaryDto>>
{
}
