using GiftFinder.Application.Features.GiftPolls.DTOs;
using MediatR;

namespace GiftFinder.Application.Features.GiftPolls.Commands.Create;

public class CreateGiftPollCommand : IRequest<PublicGiftPollDto>
{
    public string Title { get; set; } = default!;
    public string? CreatorName { get; set; }
    public string? Description { get; set; }
    public List<Guid> ProductIds { get; set; } = new();
}
