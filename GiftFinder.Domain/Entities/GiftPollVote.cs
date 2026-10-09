using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class GiftPollVote : BaseEntity
{
    public Guid GiftPollId { get; private set; }
    public GiftPoll GiftPoll { get; private set; } = default!;

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = default!;

    public string IpAddress { get; private set; } = default!;
    public string? UserAgent { get; private set; }
    public DateTime VotedAt { get; private set; } = DateTime.UtcNow;

    protected GiftPollVote() { }

    public GiftPollVote(Guid giftPollId, Guid productId, string ipAddress, string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("Địa chỉ IP không được để trống.", nameof(ipAddress));

        GiftPollId = giftPollId;
        ProductId = productId;
        IpAddress = ipAddress.Trim();
        UserAgent = userAgent;
        VotedAt = DateTime.UtcNow;
    }

    public void ChangeVote(Guid newProductId)
    {
        ProductId = newProductId;
        VotedAt = DateTime.UtcNow;
    }
}
