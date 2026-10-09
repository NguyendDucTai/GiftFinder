using MediatR;

namespace GiftFinder.Application.Features.GiftPolls.Commands.Delete;

public class DeleteGiftPollCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public DeleteGiftPollCommand(Guid id)
    {
        Id = id;
    }
}
