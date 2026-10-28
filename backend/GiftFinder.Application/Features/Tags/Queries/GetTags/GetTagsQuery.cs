using GiftFinder.Domain.Enums;
using MediatR;

namespace GiftFinder.Application.Features.Tags.Queries.GetTags;

public class GetTagsQuery : IRequest<List<TagDto>>
{
    public TagType? Type { get; set; }
}
