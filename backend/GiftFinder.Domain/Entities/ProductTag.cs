using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class ProductTag : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = default!;

    public Guid TagId { get; private set; }
    public Tag Tag { get; private set; } = default!;

    protected ProductTag() { }

    public ProductTag(Guid productId, Guid tagId)
    {
        ProductId = productId;
        TagId = tagId;
    }
}
