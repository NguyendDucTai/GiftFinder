using MediatR;

namespace GiftFinder.Application.Features.Affiliate.Commands.ImportProductByUrl;

public class ImportProductByUrlCommand : IRequest<ImportProductByUrlResultDto>
{
    /// <summary>
    /// Danh sách 1 hoặc nhiều đường link sản phẩm Shopee hoặc TikTok Shop
    /// </summary>
    public List<string> Urls { get; set; } = new();

    /// <summary>
    /// Tự động chạy Groq AI giám định và gắn Tag Dịp, Sở thích, Cung hoàng đạo
    /// </summary>
    public bool AutoTagWithAi { get; set; } = true;
}

public class ImportProductByUrlResultDto
{
    public int TotalRequested { get; set; }
    public int TotalSuccess { get; set; }
    public int TotalFailed { get; set; }
    public int TotalSkipped { get; set; }
    public List<string> SuccessProductNames { get; set; } = new();
    public List<ImportedProductItemDto> ImportedProducts { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class ImportedProductItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string? AffiliateUrl { get; set; }
    public long Price { get; set; }
    public string? ImageUrl { get; set; }
    public string Source { get; set; } = string.Empty;
}
