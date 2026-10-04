namespace GiftFinder.Application.Features.Wishlists.DTOs;

public class WishlistItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public long Price { get; set; }
    public long? OriginalPrice { get; set; }
    public string? OriginalUrl { get; set; }
    public string? AffiliateUrl { get; set; }
    public long? TargetPrice { get; set; }
    public string? Note { get; set; }
    public DateTime AddedAt { get; set; }
    
    public bool IsAvailable { get; set; }
    public string? AvailabilityMessage { get; set; }
}
