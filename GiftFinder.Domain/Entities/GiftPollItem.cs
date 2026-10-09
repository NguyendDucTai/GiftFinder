using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class GiftPollItem : BaseEntity
{
    public Guid GiftPollId { get; private set; }
    public GiftPoll GiftPoll { get; private set; } = default!;

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = default!;

    public int VoteCount { get; private set; } = 0;
    public bool IsDeleted { get; private set; } = false;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    protected GiftPollItem() { }

    public GiftPollItem(Guid giftPollId, Guid productId)
    {
        GiftPollId = giftPollId;
        ProductId = productId;
        VoteCount = 0;
        IsDeleted = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void IncrementVote()
    {
        VoteCount++;
    }

    public void DecrementVote()
    {
        if (VoteCount > 0)
        {
            VoteCount--;
        }
    }

    public void SoftDelete()
    {
        IsDeleted = true;
    }

    public void Restore()
    {
        IsDeleted = false;
    }
}
