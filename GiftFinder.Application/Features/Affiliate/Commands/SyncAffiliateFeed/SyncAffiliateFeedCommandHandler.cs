using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Application.Features.Affiliate.Commands.SyncAffiliateFeed;

public class SyncAffiliateFeedCommandHandler : IRequestHandler<SyncAffiliateFeedCommand, SyncAffiliateFeedResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAffiliateNetworkService _affiliateService;
    private readonly IAiRecommendationService _aiService;
    private readonly ILogger<SyncAffiliateFeedCommandHandler> _logger;

    public SyncAffiliateFeedCommandHandler(
        IApplicationDbContext context,
        IAffiliateNetworkService affiliateService,
        IAiRecommendationService aiService,
        ILogger<SyncAffiliateFeedCommandHandler> logger)
    {
        _context = context;
        _affiliateService = affiliateService;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<SyncAffiliateFeedResultDto> Handle(SyncAffiliateFeedCommand request, CancellationToken cancellationToken)
    {
        var result = new SyncAffiliateFeedResultDto();

        // 1. Kéo danh sách sản phẩm từ sàn TMĐT qua mạng lưới Accesstrade
        var feedItems = await _affiliateService.FetchFeedAsync(
            request.Keyword, 
            request.Source, 
            request.Limit, 
            cancellationToken);

        result.TotalFetched = feedItems.Count;

        if (feedItems.Count == 0)
        {
            result.Message = $"Không tìm thấy sản phẩm nào từ sàn {request.Source} với từ khóa '{request.Keyword}'.";
            return result;
        }

        // Cache các Tags hiện có trong database để tối ưu hóa truy vấn
        var existingTags = await _context.Tags.ToListAsync(cancellationToken);

        foreach (var item in feedItems)
        {
            // Kiểm tra trùng lặp sản phẩm đã có trong kho
            var isDuplicate = await _context.Products.AnyAsync(p => 
                p.OriginalUrl == item.OriginalUrl || 
                (item.ExternalProductId != null && p.ExternalProductId == item.ExternalProductId), 
                cancellationToken);

            if (isDuplicate)
            {
                result.TotalSkipped++;
                continue;
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
                // Tự sinh deeplink nếu sàn chưa trả về
                var trackingSubId = $"feed_{Guid.NewGuid():N}"[..16];
                var deeplink = await _affiliateService.GenerateDeeplinkAsync(item.OriginalUrl, trackingSubId, cancellationToken);
                product.SetAffiliateUrl(deeplink);
            }

            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                product.SetDescription(item.Description);
            }

            product.SetRating(item.Rating, item.TotalReviews);

            // 2. Chạy Groq AI giám định và gắn Tag tự động
            if (request.AutoTagWithAi)
            {
                try
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
                        result.TotalRejectedByAi++;
                        result.RejectedDetails.Add($"{item.Name} -> Bị từ chối: {reason}");
                        continue;
                    }

                    // Gán cung hoàng đạo phù hợp
                    if (aiResult.SuitableZodiacs.Count > 0)
                    {
                        product.SetSuitableZodiacs(aiResult.SuitableZodiacs);
                    }

                    // Gắn Tags Dịp tặng (Occasion)
                    foreach (var occasion in aiResult.Occasions)
                    {
                        var tag = GetOrCreateTag(existingTags, occasion, TagType.Occasion);
                        product.AddTag(tag.Id);
                    }

                    // Gắn Tags Sở thích (Hobby)
                    foreach (var hobby in aiResult.Hobbies)
                    {
                        var tag = GetOrCreateTag(existingTags, hobby, TagType.Hobby);
                        product.AddTag(tag.Id);
                    }

                    // Phê duyệt và kích hoạt sản phẩm ngay
                    product.Approve();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi phân loại AI cho sản phẩm {ProductName}, sử dụng tag mặc định.", item.Name);
                    var defaultOccasion = GetOrCreateTag(existingTags, "Sinh nhật", TagType.Occasion);
                    var defaultHobby = GetOrCreateTag(existingTags, "Decor", TagType.Hobby);
                    product.AddTag(defaultOccasion.Id);
                    product.AddTag(defaultHobby.Id);
                    product.Approve();
                }
            }

            _context.Products.Add(product);
            result.TotalImported++;
            result.ImportedProductNames.Add(product.Name);
        }

        await _context.SaveChangesAsync(cancellationToken);

        result.Message = $"Đồng bộ thành công {result.TotalImported}/{result.TotalFetched} sản phẩm từ sàn {request.Source}. (Bỏ qua trùng: {result.TotalSkipped}, AI từ chối: {result.TotalRejectedByAi})";
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
