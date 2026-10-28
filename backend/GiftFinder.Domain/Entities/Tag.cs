using GiftFinder.Domain.Common;
using GiftFinder.Domain.Enums;

namespace GiftFinder.Domain.Entities;

public class Tag : BaseAuditableEntity
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public TagType Type { get; private set; } = TagType.Hobby;
    public string? Description { get; private set; }

    protected Tag() { }

    public Tag(string name, string slug, TagType type, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên nhãn (Tag) không được để trống.");

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Type = type;
        Description = description;
    }
}