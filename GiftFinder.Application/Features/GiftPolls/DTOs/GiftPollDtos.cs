namespace GiftFinder.Application.Features.GiftPolls.DTOs;

public class GiftPollItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public long Price { get; set; }
    public string? TargetUrl { get; set; }
    public int VoteCount { get; set; }
    public double VotePercentage { get; set; }
}

public class PublicGiftPollDto
{
    public Guid Id { get; set; }
    public string ShareCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? CreatorName { get; set; }
    public string? Description { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public int TotalVotes { get; set; }
    public Guid? UserVotedProductId { get; set; }
    public List<GiftPollItemDto> Items { get; set; } = new();
}

public class GiftPollSummaryDto
{
    public Guid Id { get; set; }
    public string ShareCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? CreatorName { get; set; }
    public string? Description { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public int TotalItems { get; set; }
    public int TotalVotes { get; set; }
    public DateTime CreatedAt { get; set; }
}
