using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Application.Features.Affiliate.Commands.ImportProductByUrl;

public class ImportProductByUrlCommandHandler : IRequestHandler<ImportProductByUrlCommand, ImportProductByUrlResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAffiliateNetworkService _affiliateService;
    private readonly IAiRecommendationService _aiService;
    private readonly ILogger<ImportProductByUrlCommandHandler> _logger;

    public ImportProductByUrlCommandHandler(
        IApplicationDbContext context,
        IAffiliateNetworkService affiliateService,
        IAiRecommendationService aiService,
        ILogger<ImportProductByUrlCommandHandler> logger)
    {
        _context = context;
        _affiliateService = affiliateService;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<ImportProductByUrlResultDto> Handle(ImportProductByUrlCommand request, CancellationToken cancellationToken)
    {
        var result = new ImportProductByUrlResultDto
        {
            TotalRequested = request.Urls.Count
        };

        if (request.Urls == null || request.Urls.Count == 0)
        {
            result.Message = "Danh sách URL không được để trống.";
            return result;
        }

        var existingTags = await _context.Tags.ToListAsync(cancellationToken);

        var uniqueUrls = request.Urls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        result.TotalRequested = uniqueUrls.Count;

        foreach (var url in uniqueUrls)
        {

            try
            {
                // Kiểm tra xem URL này đã được nạp vào kho chưa
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.OriginalUrl == url, cancellationToken);

                if (existingProduct != null)
                {
                    if (existingProduct.Status == ProductStatus.Approved)
                    {
                        result.TotalSkipped++;
                        result.Errors.Add($"Link '{url}' đã tồn tại trong kho (đã được duyệt).");
                        continue;
                    }

                    // Nếu trước đó sản phẩm bị từ chối (ví dụ do cào thiếu thông tin), gỡ bỏ bản ghi cũ để AI thẩm định lại từ đầu
                    _context.Products.Remove(existingProduct);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                // Bóc tách thông tin sản phẩm và tạo affiliate link
                var item = await _affiliateService.FetchProductByUrlAsync(url, cancellationToken);
                if (item == null)
                {
                    result.TotalFailed++;
                    result.Errors.Add($"Không thể trích xuất thông tin từ URL: '{url}'.");
                    continue;
                }

                // Kiểm tra trùng theo mã sản phẩm thật trên sàn (ExternalProductId)
                if (!string.IsNullOrWhiteSpace(item.ExternalProductId) && !item.ExternalProductId.StartsWith("URL_"))
                {
                    var duplicateByExternalId = await _context.Products
                        .FirstOrDefaultAsync(p => p.Source == item.Source && p.ExternalProductId == item.ExternalProductId, cancellationToken);

                    if (duplicateByExternalId != null)
                    {
                        if (duplicateByExternalId.Status == ProductStatus.Approved)
                        {
                            result.TotalSkipped++;
                            result.Errors.Add($"Sản phẩm '{item.Name}' (Mã sàn: {item.ExternalProductId}) đã tồn tại trong kho.");
                            continue;
                        }

                        _context.Products.Remove(duplicateByExternalId);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                var product = new Product(
                    item.Name,
                    item.Price,
                    item.OriginalUrl,
                    item.Source,
                    item.ExternalProductId,
                    item.ImageUrl,
                    item.OriginalPrice);

                if (!string.IsNullOrWhiteSpace(item.AffiliateUrl))
                {
                    product.SetAffiliateUrl(item.AffiliateUrl);
                }
                else
                {
                    var subId = $"url_{Guid.NewGuid():N}"[..16];
                    var deeplink = await _affiliateService.GenerateDeeplinkAsync(item.OriginalUrl, subId, cancellationToken);
                    product.SetAffiliateUrl(deeplink);
                }

                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    product.SetDescription(item.Description);
                }

                product.SetRating(item.Rating, item.TotalReviews);

                // AI giám định & gán tag tự động
                if (request.AutoTagWithAi)
                {
                    var aiResult = await _aiService.ClassifyAndTagProductAsync(
                        item.Name, 
                        item.Description, 
                        item.Price, 
                        cancellationToken);

                    if (!aiResult.IsValidGift)
                    {
                        var reason = aiResult.RejectReason ?? "AI đánh giá không phù hợp làm quà tặng";
                        product.Reject(reason);
                        _context.Products.Add(product);
                        result.TotalFailed++;
                        result.Errors.Add($"{item.Name} -> Bị AI từ chối: {reason}");
                        continue;
                    }

                    if (aiResult.SuitableZodiacs.Count > 0)
                    {
                        product.SetSuitableZodiacs(aiResult.SuitableZodiacs);
                    }

                    foreach (var occ in aiResult.Occasions)
                    {
                        var tag = GetOrCreateTag(existingTags, occ, TagType.Occasion);
                        product.AddTag(tag.Id);
                    }

                    foreach (var hob in aiResult.Hobbies)
                    {
                        var tag = GetOrCreateTag(existingTags, hob, TagType.Hobby);
                        product.AddTag(tag.Id);
                    }

                    product.Approve();
                }

                _context.Products.Add(product);
                result.TotalSuccess++;
                result.SuccessProductNames.Add(product.Name);
                result.ImportedProducts.Add(new ImportedProductItemDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    OriginalUrl = product.OriginalUrl,
                    AffiliateUrl = product.AffiliateUrl,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Source = product.Source.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nhập sản phẩm qua URL: {Url}", url);
                result.TotalFailed++;
                result.Errors.Add($"Lỗi xử lý link '{url}': {ex.Message}");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        result.Message = $"Nhập thành công {result.TotalSuccess}/{result.TotalRequested} sản phẩm qua URL. (Trùng: {result.TotalSkipped}, Lỗi/Từ chối: {result.TotalFailed})";
        return result;
    }

    private Tag GetOrCreateTag(List<Tag> existingTags, string tagName, TagType type)
    {
        var cleanName = tagName.Trim();
        var slug = ToUrlFriendlySlug(cleanName);

        var found = existingTags.FirstOrDefault(t => 
            t.Type == type && 
            (t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) || 
             t.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase)));

        if (found != null) return found;

        var newTag = new Tag(cleanName, slug, type);
        _context.Tags.Add(newTag);
        existingTags.Add(newTag);
        return newTag;
    }

    private static string ToUrlFriendlySlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "tag-" + Guid.NewGuid().ToString("N")[..6];

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var clean = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        clean = clean.Replace("đ", "d").Replace("Đ", "d");
        clean = Regex.Replace(clean, @"[^a-z0-9\s-]", "");
        clean = Regex.Replace(clean, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(clean) ? "tag-" + Guid.NewGuid().ToString("N")[..6] : clean;
    }
}
