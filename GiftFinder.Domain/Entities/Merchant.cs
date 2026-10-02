using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class Merchant : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    public string ShopName { get; private set; } = default!;
    public string ContactEmail { get; private set; } = default!;
    public string ContactPhone { get; private set; } = default!;
    public string? BusinessLicense { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    protected Merchant() { }

    public Merchant(Guid userId, string shopName, string contactEmail, string contactPhone)
    {
        if (string.IsNullOrWhiteSpace(shopName))
            throw new ArgumentException("Tên shop không được để trống.");
        if (string.IsNullOrWhiteSpace(contactPhone))
            throw new ArgumentException("Số điện thoại liên hệ không được để trống.");

        UserId = userId;
        ShopName = shopName.Trim();
        ContactEmail = contactEmail.Trim();
        ContactPhone = contactPhone.Trim();
        IsVerified = false;
    }

    public void Verify()
    {
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
    }
}
