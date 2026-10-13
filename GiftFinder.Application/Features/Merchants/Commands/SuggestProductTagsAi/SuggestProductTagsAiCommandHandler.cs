using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Commands.SuggestProductTagsAi;

public class SuggestProductTagsAiCommandHandler : IRequestHandler<SuggestProductTagsAiCommand, AiProductTagSuggestionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAiRecommendationService _aiService;

    public SuggestProductTagsAiCommandHandler(
        IApplicationDbContext context,
        IAiRecommendationService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    public async Task<AiProductTagSuggestionDto> Handle(SuggestProductTagsAiCommand request, CancellationToken cancellationToken)
    {
        // 1. Gọi Groq AI thẩm định và phân tích sản phẩm (120B)
        var taggingResult = await _aiService.ClassifyAndTagProductAsync(
            request.ProductName,
            request.Description,
            request.Price,
            cancellationToken);

        // 2. Guardrail: Nếu sản phẩm không hợp lệ làm quà tặng (thuộc Blacklist: xi măng, phụ tùng...)
        if (!taggingResult.IsValidGift)
        {
            throw new ArgumentException(
                taggingResult.RejectReason ?? "Sản phẩm không phù hợp với tiêu chí quà tặng của GiftFinder.");
        }

        // 3. Khớp các tag AI gợi ý với kho Tag trong Database
        var existingTags = await _context.Tags.ToListAsync(cancellationToken);
        var suggestedTags = new List<SuggestedTagItemDto>();
        var suggestedTagIds = new List<Guid>();
        bool hasNewTag = false;

        Tag GetOrCreateTag(string tagName, TagType type)
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
            hasNewTag = true;
            return newTag;
        }

        // Khớp Tag Dịp tặng (Occasion)
        foreach (var occasionName in taggingResult.Occasions)
        {
            if (string.IsNullOrWhiteSpace(occasionName)) continue;
            var tag = GetOrCreateTag(occasionName, TagType.Occasion);
            if (!suggestedTagIds.Contains(tag.Id))
            {
                suggestedTagIds.Add(tag.Id);
                suggestedTags.Add(new SuggestedTagItemDto
                {
                    TagId = tag.Id,
                    Name = tag.Name,
                    Type = tag.Type.ToString()
                });
            }
        }

        // Khớp Tag Sở thích (Hobby)
        foreach (var hobbyName in taggingResult.Hobbies)
        {
            if (string.IsNullOrWhiteSpace(hobbyName)) continue;
            var tag = GetOrCreateTag(hobbyName, TagType.Hobby);
            if (!suggestedTagIds.Contains(tag.Id))
            {
                suggestedTagIds.Add(tag.Id);
                suggestedTags.Add(new SuggestedTagItemDto
                {
                    TagId = tag.Id,
                    Name = tag.Name,
                    Type = tag.Type.ToString()
                });
            }
        }

        if (hasNewTag)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 4. Định dạng danh sách Cung Hoàng Đạo
        var suitableZodiacs = taggingResult.SuitableZodiacs
            .Select(z => z.ToString())
            .Distinct()
            .ToList();

        var rationale = !string.IsNullOrWhiteSpace(taggingResult.ZodiacRationale)
            ? taggingResult.ZodiacRationale
            : (!string.IsNullOrWhiteSpace(taggingResult.GiftAdvice)
                ? taggingResult.GiftAdvice
                : $"{request.ProductName} có phong cách và công năng rất phù hợp với gu thẩm mỹ của các cung hoàng đạo trên.");

        return new AiProductTagSuggestionDto
        {
            ProductName = request.ProductName,
            SuitableZodiacs = suitableZodiacs,
            ZodiacRationale = rationale,
            SuggestedTags = suggestedTags,
            SuggestedTagIds = suggestedTagIds
        };
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
