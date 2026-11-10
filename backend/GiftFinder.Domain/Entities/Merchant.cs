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
    public string PickupProvince { get; private set; } = "Hà Nội";
    public string? PickupAddress { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    protected Merchant() { }

    public Merchant(
        Guid userId,
        string shopName,
        string contactEmail,
        string contactPhone,
        string? businessLicense = null,
        string? pickupProvince = null,
        string? pickupAddress = null)
    {
        if (string.IsNullOrWhiteSpace(shopName))
            throw new ArgumentException("Tên shop không được để trống.");
        if (string.IsNullOrWhiteSpace(contactPhone))
            throw new ArgumentException("Số điện thoại liên hệ không được để trống.");

        UserId = userId;
        ShopName = shopName.Trim();
        ContactEmail = contactEmail.Trim();
        ContactPhone = contactPhone.Trim();
        BusinessLicense = businessLicense?.Trim();
        PickupProvince = string.IsNullOrWhiteSpace(pickupProvince) ? "Hà Nội" : pickupProvince.Trim();
        PickupAddress = pickupAddress?.Trim();
        IsVerified = false;
    }

    public void UpdateProfile(
        string shopName,
        string contactEmail,
        string contactPhone,
        string? businessLicense = null,
        string? pickupProvince = null,
        string? pickupAddress = null)
    {
        if (string.IsNullOrWhiteSpace(shopName))
            throw new ArgumentException("Tên shop không được để trống.");
        if (string.IsNullOrWhiteSpace(contactPhone))
            throw new ArgumentException("Số điện thoại liên hệ không được để trống.");

        ShopName = shopName.Trim();
        ContactEmail = contactEmail.Trim();
        ContactPhone = contactPhone.Trim();
        BusinessLicense = businessLicense?.Trim();
        if (!string.IsNullOrWhiteSpace(pickupProvince))
        {
            PickupProvince = pickupProvince.Trim();
        }
        if (pickupAddress != null)
        {
            PickupAddress = pickupAddress.Trim();
        }
    }

    public void Verify()
    {
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
    }
}
