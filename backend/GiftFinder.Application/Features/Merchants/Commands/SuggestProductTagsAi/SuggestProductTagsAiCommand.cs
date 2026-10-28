using GiftFinder.Application.Features.Merchants.DTOs;
using MediatR;

namespace GiftFinder.Application.Features.Merchants.Commands.SuggestProductTagsAi;

public class SuggestProductTagsAiCommand : IRequest<AiProductTagSuggestionDto>
{
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long Price { get; set; }
}
