using GiftFinder.Domain.Enums;

namespace GiftFinder.Application.Features.Affiliate.DTOs;

public class ProductTaggingResult
{
    public bool IsValidGift { get; set; } = true;
    public string? RejectReason { get; set; }
    public List<string> Occasions { get; set; } = new();
    public List<string> Hobbies { get; set; } = new();
    public List<ZodiacSign> SuitableZodiacs { get; set; } = new();
    public string? GiftAdvice { get; set; }
}
