using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class GiftPoll : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public string? CreatorName { get; private set; }
    public string? Description { get; private set; }
    public string ShareCode { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<GiftPollItem> _pollItems = new();
    public IReadOnlyCollection<GiftPollItem> PollItems => _pollItems.AsReadOnly();

    protected GiftPoll() { }

    public GiftPoll(Guid userId, string title, string? creatorName, string? description, string shareCode, DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tiêu đề cuộc bình chọn không được để trống.", nameof(title));

        UserId = userId;
        Title = title.Trim();
        CreatorName = string.IsNullOrWhiteSpace(creatorName) ? null : creatorName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ShareCode = shareCode;
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(30); // BR-05: 30 ngày
        IsActive = true;
    }

    public void UpdateInfo(string title, string? creatorName, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tiêu đề cuộc bình chọn không được để trống.", nameof(title));

        Title = title.Trim();
        CreatorName = string.IsNullOrWhiteSpace(creatorName) ? null : creatorName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(Guid productId)
    {
        var existing = _pollItems.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
        {
            if (existing.IsDeleted)
            {
                existing.Restore();
            }
        }
        else
        {
            _pollItems.Add(new GiftPollItem(Id, productId));
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(Guid productId)
    {
        var existing = _pollItems.FirstOrDefault(i => i.ProductId == productId && !i.IsDeleted);
        if (existing != null)
        {
            existing.SoftDelete();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
