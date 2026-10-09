using GiftFinder.Domain.Enums;

namespace GiftFinder.Application.Features.Tags.Queries.GetTags;

public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public TagType Type { get; set; }
    public string TypeName { get; set; } = default!;
    public string? Description { get; set; }
}
