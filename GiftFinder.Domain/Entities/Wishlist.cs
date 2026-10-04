using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class Wishlist : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = default!;

    public long? TargetPrice { get; private set; }
    public string? Note { get; private set; }

    protected Wishlist() { }

    public Wishlist(Guid userId, Guid productId, long? targetPrice = null, string? note = null)
    {
        UserId = userId;
        ProductId = productId;
        TargetPrice = targetPrice;
        Note = note;
    }

    public void UpdateDetails(long? targetPrice, string? note)
    {
        TargetPrice = targetPrice;
        Note = note;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore(long? targetPrice = null, string? note = null)
    {
        IsDeleted = false;
        TargetPrice = targetPrice ?? TargetPrice;
        Note = note ?? Note;
        UpdatedAt = DateTime.UtcNow;
    }
}
