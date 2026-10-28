using System.Collections.Generic;

namespace GiftFinder.Application.Features.Merchants.DTOs;

public class ProcessFeaturedExpirationResultDto
{
    public int ExpiredCount { get; set; }
    public int AlertedCount { get; set; }
    public List<string> ExpiredProductNames { get; set; } = new();
    public List<string> AlertedProductNames { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
