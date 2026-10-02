using GiftFinder.Domain.Common;
using GiftFinder.Domain.Enums;

namespace GiftFinder.Domain.Entities;

public class User : BaseAuditableEntity
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public string? AvatarUrl { get; private set; }
    public UserRole Role { get; private set; } = UserRole.Member;
    public bool IsActive { get; private set; } = true;

    // Gói thành viên Premium (UC-14 - BR-19, BR-20)
    public MembershipTier Tier { get; private set; } = MembershipTier.Free;
    public DateTime? PremiumExpiresAt { get; private set; }
    public DateTime? GracePeriodUntil { get; private set; }
    public bool AutoRenew { get; private set; }

    public Merchant? MerchantProfile { get; private set; }

    protected User() { }

    public User(string email, string passwordHash, string fullName, UserRole role = UserRole.Member)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("Email không đúng định dạng hoặc bị để trống.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên không được để trống.");

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        Role = role;
    }

    public void UpdateProfile(string fullName, string? phoneNumber, string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên không được để trống.");

        FullName = fullName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        AvatarUrl = avatarUrl;
    }

    public void UpgradeToPremium(int months = 1, bool autoRenew = true)
    {
        Tier = MembershipTier.Premium;
        var start = (PremiumExpiresAt.HasValue && PremiumExpiresAt.Value > DateTime.UtcNow)
            ? PremiumExpiresAt.Value
            : DateTime.UtcNow;

        PremiumExpiresAt = start.AddMonths(months);
        GracePeriodUntil = null;
        AutoRenew = autoRenew;
    }

    public void EnterGracePeriod()
    {
        GracePeriodUntil = DateTime.UtcNow.AddDays(3); // Ân hạn 3 ngày (BR-20)
    }

    public void DowngradeToFree()
    {
        Tier = MembershipTier.Free;
        PremiumExpiresAt = null;
        GracePeriodUntil = null;
        AutoRenew = false;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}