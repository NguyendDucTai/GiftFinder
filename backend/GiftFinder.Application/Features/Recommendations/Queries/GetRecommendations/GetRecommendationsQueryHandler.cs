using System.Collections.Concurrent;
using GiftFinder.Domain.Entities;
using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

public class GetRecommendationsQueryHandler : IRequestHandler<GetRecommendationsQuery, PagedRecommendationResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IAiRecommendationService _aiService;

    // Cache in-memory kết quả phân tích ý định (ParsedRecommendationIntent) theo prompt
    // Giúp các lượt bấm "Món khác" cùng ngữ cảnh đạt tốc độ 0ms và tiêu tốn 0 token cho bước bóc tách ý định
    private static readonly ConcurrentDictionary<string, (ParsedRecommendationIntent Intent, DateTime ExpireAt)> _intentCache = new(StringComparer.OrdinalIgnoreCase);

    public GetRecommendationsQueryHandler(IApplicationDbContext context, IAiRecommendationService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    private static ParsedRecommendationIntent CloneIntent(ParsedRecommendationIntent src)
    {
        return new ParsedRecommendationIntent
        {
            IntentType = src.IntentType,
            DirectAnswer = src.DirectAnswer,
            ShouldIncludeProduct = src.ShouldIncludeProduct,
            Relationship = src.Relationship,
            TargetGender = src.TargetGender,
            TargetAge = src.TargetAge,
            TargetZodiac = src.TargetZodiac,
            MinBudget = src.MinBudget,
            MaxBudget = src.MaxBudget,
            BudgetType = src.BudgetType,
            Occasion = src.Occasion,
            InterestKeywords = new List<string>(src.InterestKeywords),
            Summary = src.Summary,
            IsValidGiftIntent = src.IsValidGiftIntent,
            AiMessage = src.AiMessage,
            SuggestedQuestions = new List<string>(src.SuggestedQuestions)
        };
    }

    public async Task<PagedRecommendationResult> Handle(GetRecommendationsQuery request, CancellationToken cancellationToken)
    {
        ParsedRecommendationIntent? parsedIntent = null;

        // 1. Nếu người dùng nhập câu Prompt tự nhiên -> Bóc tách ý định (có cache in-memory 15 phút)
        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            var trimmedPrompt = request.Prompt.Trim();
            var cacheKey = $"{trimmedPrompt}_prod_{request.CurrentProductId}";
            if (_intentCache.TryGetValue(cacheKey, out var cached) && cached.ExpireAt > DateTime.UtcNow)
            {
                parsedIntent = CloneIntent(cached.Intent);
            }
            else
            {
                parsedIntent = await _aiService.ParsePromptAsync(trimmedPrompt, cancellationToken);
                if (parsedIntent != null)
                {
                    _intentCache[cacheKey] = (CloneIntent(parsedIntent), DateTime.UtcNow.AddMinutes(15));

                    // Dọn dẹp cache nếu quá 500 mục
                    if (_intentCache.Count > 500)
                    {
                        var expiredKeys = _intentCache.Where(kvp => kvp.Value.ExpireAt <= DateTime.UtcNow).Select(kvp => kvp.Key).ToList();
                        foreach (var k in expiredKeys) _intentCache.TryRemove(k, out _);
                    }
                }
            }

            // AI Guardrail: Nếu câu hỏi không liên quan đến tìm quà (lạc đề, quấy rối, vô nghĩa)
            if (parsedIntent != null && !parsedIntent.IsValidGiftIntent)
            {
                return new PagedRecommendationResult
                {
                    Items = new List<ProductRecommendationDto>(),
                    TotalCount = 0,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    IsValidIntent = false,
                    Message = parsedIntent.AiMessage,
                    InterpretedIntent = parsedIntent,
                    DirectAnswer = parsedIntent.DirectAnswer,
                    IntentType = parsedIntent.IntentType.ToString()
                };
            }

            // 1b. Contextual Product Q&A: Nếu người dùng hỏi đáp về sản phẩm đang xem
            var isProductInquiry = (request.CurrentProductId.HasValue || !string.IsNullOrWhiteSpace(request.CurrentProductContext)) &&
                (parsedIntent?.IntentType == ChatIntentType.ProductDetailQnA ||
                 System.Text.RegularExpressions.Regex.IsMatch(
                     (request.Prompt ?? "").ToLowerInvariant(),
                     @"(món|sản\s*phẩm|cái|em|chiếc|bộ|set|đồng\s*hồ|vòng|dây\s*chuyền|gối)\s*này",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                 (request.Prompt ?? "").ToLowerInvariant().Contains("như nào") ||
                 (request.Prompt ?? "").ToLowerInvariant().Contains("thế nào") ||
                 (request.Prompt ?? "").ToLowerInvariant().Contains("nói qua") ||
                 (request.Prompt ?? "").ToLowerInvariant().Contains("chống nước"));

            if (isProductInquiry)
            {
                Product? currentProd = null;
                if (request.CurrentProductId.HasValue)
                {
                    currentProd = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                        .FirstOrDefaultAsync(p => p.Id == request.CurrentProductId.Value, cancellationToken);
                }

                if (currentProd != null)
                {
                    var answer = await _aiService.AnswerProductInquiryAsync(
                        currentProd.Name,
                        currentProd.Description,
                        currentProd.Price,
                        request.Prompt ?? "",
                        cancellationToken);

                    var prodDto = new ProductRecommendationDto
                    {
                        Id = currentProd.Id,
                        Name = currentProd.Name,
                        Price = currentProd.Price,
                        ImageUrl = currentProd.ImageUrl,
                        AffiliateUrl = currentProd.AffiliateUrl,
                        OriginalUrl = currentProd.OriginalUrl,
                        Rating = currentProd.Rating,
                        TotalReviews = currentProd.TotalReviews,
                        AiAdvice = null // Tránh trùng lặp với DirectAnswer ở UI
                    };

                    return new PagedRecommendationResult
                    {
                        Items = new List<ProductRecommendationDto> { prodDto },
                        TotalCount = 1,
                        PageNumber = 1,
                        PageSize = request.PageSize,
                        IsValidIntent = true,
                        InterpretedIntent = parsedIntent,
                        Message = parsedIntent?.Summary ?? $"Giải đáp thắc mắc về {currentProd.Name}",
                        DirectAnswer = answer,
                        IntentType = ChatIntentType.ProductDetailQnA.ToString()
                    };
                }
            }

            // 1c. Conversational Q&A, Policy & Inventory Stats: Nếu là câu hỏi tư vấn thuần túy, chính sách dịch vụ hoặc thống kê kho không cần kèm sản phẩm
            if ((parsedIntent?.IntentType == ChatIntentType.ConsultingAdvice || 
                 parsedIntent?.IntentType == ChatIntentType.ServicePolicy ||
                 parsedIntent?.IntentType == ChatIntentType.InventoryInquiry) 
                && !parsedIntent.ShouldIncludeProduct)
            {
                return new PagedRecommendationResult
                {
                    Items = new List<ProductRecommendationDto>(),
                    TotalCount = 0,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    IsValidIntent = true,
                    InterpretedIntent = parsedIntent,
                    Message = parsedIntent.Summary ?? "Phản hồi kho quà từ AI Concierge",
                    DirectAnswer = parsedIntent.DirectAnswer,
                    IntentType = parsedIntent.IntentType.ToString()
                };
            }
        }

        // 2. Gộp tiêu chí thông minh (Ưu tiên bộ lọc thủ công nếu có, nếu không thì lấy từ AI)
        var effectiveMinBudget = request.MinBudget ?? parsedIntent?.MinBudget;
        var effectiveMaxBudget = request.MaxBudget ?? parsedIntent?.MaxBudget;
        var budgetType = parsedIntent?.BudgetType;

        if (string.IsNullOrEmpty(budgetType))
        {
            if (effectiveMinBudget.HasValue && !effectiveMaxBudget.HasValue) budgetType = "Above";
            else if (!effectiveMinBudget.HasValue && effectiveMaxBudget.HasValue) budgetType = "Below";
            else if (effectiveMinBudget.HasValue && effectiveMaxBudget.HasValue) budgetType = "Range";
        }

        string NormalizeRelationship(string? rel)
        {
            if (string.IsNullOrWhiteSpace(rel)) return "";
            var r = rel.Trim().ToLowerInvariant();
            if (r == "father" || r == "dad" || r == "cha" || r == "ba") return "bố";
            if (r == "mother" || r == "mom" || r == "má") return "mẹ";
            if (r == "brother" || r == "anh trai") return "anh trai";
            if (r == "sister" || r == "chị gái") return "chị gái";
            if (r == "younger brother" || r == "em trai") return "em trai";
            if (r == "younger sister" || r == "em gái") return "em gái";
            if (r == "boyfriend" || r == "bạn trai") return "bạn trai";
            if (r == "girlfriend" || r == "bạn gái") return "bạn gái";
            if (r == "husband" || r == "chồng") return "chồng";
            if (r == "wife" || r == "vợ") return "vợ";
            if (r == "colleague" || r == "co-worker") return "đồng nghiệp";
            if (r == "boss" || r == "sếp") return "sếp";
            if (r == "grandfather" || r == "ông nội" || r == "ông ngoại") return "ông";
            if (r == "grandmother" || r == "bà nội" || r == "bà ngoại") return "bà";
            return rel.Trim();
        }

        var effectiveZodiac = request.TargetZodiac ?? parsedIntent?.TargetZodiac;
        var effectiveRelationship = !string.IsNullOrWhiteSpace(request.Relationship) 
            ? request.Relationship 
            : parsedIntent?.Relationship;
        effectiveRelationship = NormalizeRelationship(effectiveRelationship);
        var effectiveAge = request.TargetAge ?? parsedIntent?.TargetAge;

        // Giới tính và Quan hệ lấy trực tiếp từ AI phân tích (tin tưởng kết quả NLU của AI)
        var effectiveGender = parsedIntent?.TargetGender;
        var promptText = (request.Prompt ?? "").ToLowerInvariant();

        // Bổ sung hỗ trợ bóc tách ngân sách bằng Regex dự phòng nếu AI chưa gán số cụ thể
        if (!effectiveMinBudget.HasValue && !effectiveMaxBudget.HasValue)
        {
            var budgetMatches = System.Text.RegularExpressions.Regex.Matches(promptText, @"(\d+)\s*(k|cành|lít|lốp|củ|triệu|tr)\b");
            if (budgetMatches.Count > 0)
            {
                // Ưu tiên mốc giá cuối cùng trong câu (mốc giá mới nhất khi người dùng cập nhật ngân sách)
                var budgetMatch = budgetMatches[budgetMatches.Count - 1];
                if (long.TryParse(budgetMatch.Groups[1].Value, out var num))
                {
                    var unit = budgetMatch.Groups[2].Value;
                    long budget = (unit == "k") ? num * 1000 :
                                  (unit == "cành" || unit == "lít" || unit == "lốp") ? num * 100000 :
                                  (unit == "củ" || unit == "triệu" || unit == "tr") ? num * 1000000 : num;

                    // Kiểm tra từ ngữ ngay trước mốc giá cuối cùng (phạm vi 30 ký tự trước đó)
                    var textBeforeMatch = promptText.Substring(0, budgetMatch.Index);
                    var recentContext = textBeforeMatch.Length > 30 ? textBeforeMatch.Substring(textBeforeMatch.Length - 30) : textBeforeMatch;

                    if (recentContext.Contains("dưới") || recentContext.Contains("tối đa") || recentContext.Contains("đổ lại"))
                    {
                        effectiveMaxBudget = budget;
                        budgetType = "Below";
                    }
                    else if (recentContext.Contains("trên") || recentContext.Contains("hơn") || recentContext.Contains("từ"))
                    {
                        effectiveMinBudget = budget;
                        budgetType = "Above";
                    }
                    else
                    {
                        effectiveMinBudget = (long)(budget * 0.75);
                        effectiveMaxBudget = (long)(budget * 1.3);
                        budgetType = "Around";
                    }
                }
            }
            else if (promptText.Contains("đắt") || promptText.Contains("cao cấp") || promptText.Contains("sang trọng") || promptText.Contains("hàng hiệu") || promptText.Contains("xịn"))
            {
                effectiveMinBudget = 700000;
                budgetType = "Above";
            }
            else if (promptText.Contains("rẻ") || promptText.Contains("bình dân") || promptText.Contains("tiết kiệm") || promptText.Contains("hạt dẻ"))
            {
                effectiveMaxBudget = 250000;
                budgetType = "Below";
            }
        }

        if (parsedIntent != null)
        {
            var negativeInventoryMatch = System.Text.RegularExpressions.Regex.Match(
                promptText,
                @"(không\s*có|sao\s*không\s*có|sao\s*không\s*thấy|chẳng\s*có|chả\s*có)\s*(món|quà|set|cái nào)?\s*(nào)?\s*(trên|hơn|từ)\s*(\d+)\s*(k|cành|lít|lốp|củ|triệu|tr)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (negativeInventoryMatch.Success)
            {
                parsedIntent.IntentType = ChatIntentType.InventoryInquiry;
                parsedIntent.BudgetType = "Above";
                budgetType = "Above";
                if (long.TryParse(negativeInventoryMatch.Groups[5].Value, out var nVal))
                {
                    var unit = negativeInventoryMatch.Groups[6].Value.ToLowerInvariant();
                    long budget = (unit == "k") ? nVal * 1000 :
                                  (unit == "cành" || unit == "lít" || unit == "lốp") ? nVal * 100000 :
                                  (unit == "củ" || unit == "triệu" || unit == "tr") ? nVal * 1000000 : 
                                  (nVal < 1000 ? nVal * 1000 : nVal);
                    parsedIntent.MinBudget = budget;
                    parsedIntent.MaxBudget = null;
                    effectiveMinBudget = budget;
                    effectiveMaxBudget = null;
                }
                parsedIntent.ShouldIncludeProduct = true;
                parsedIntent.IsValidGiftIntent = true;
                if (string.IsNullOrWhiteSpace(parsedIntent.DirectAnswer))
                {
                    parsedIntent.DirectAnswer = $"Dạ bên mình có các set quà cao cấp trên {(effectiveMinBudget ?? 500000):N0}đ rất sang trọng và tinh tế chứ ạ! Dưới đây là gợi ý nổi bật nhất để bạn tham khảo nhé:";
                }
            }

            // Nhận diện câu hỏi điều chỉnh ngân sách tương đối (thấp hơn / rẻ hơn / mềm hơn HOẶC cao hơn / đắt hơn)
            var isRelativeLowerBudget = System.Text.RegularExpressions.Regex.IsMatch(
                promptText,
                @"(thấp\s*hơn|rẻ\s*hơn|mềm\s*hơn|bình\s*dân\s*hơn|tiết\s*kiệm\s*hơn|bớt\s*tiền|hạ\s*giá|giảm\s*giá|giá\s*thấp|giá\s*mềm|món\s*(nào)?\s*thấp\s*hơn|món\s*(nào)?\s*rẻ\s*hơn|cái\s*(nào)?\s*thấp\s*hơn|cái\s*(nào)?\s*rẻ\s*hơn|có\s*gì\s*rẻ\s*hơn|có\s*gì\s*thấp\s*hơn|giá\s*cao\s*(quá|vậy|thế)|đắt\s*(quá|vậy|thế)|chát\s*(quá|vậy|thế))",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                ((promptText.Contains("giá cao") || promptText.Contains("đắt")) && (promptText.Contains("thấp") || promptText.Contains("rẻ") || promptText.Contains("mềm")));

            var isRelativeHigherBudget = System.Text.RegularExpressions.Regex.IsMatch(
                promptText,
                @"(đắt\s*hơn|cao\s*hơn|cao\s*cấp\s*hơn|sang\s*hơn|xịn\s*hơn|nhiều\s*tiền\s*hơn|tăng\s*ngân\s*sách|món\s*(nào)?\s*cao\s*hơn|món\s*(nào)?\s*đắt\s*hơn|cái\s*(nào)?\s*đắt\s*hơn)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (isRelativeLowerBudget || isRelativeHigherBudget)
            {
                long? refProductPrice = null;

                if (request.CurrentProductId.HasValue)
                {
                    var cp = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.CurrentProductId.Value, cancellationToken);
                    if (cp != null)
                    {
                        refProductPrice = cp.Price;
                    }
                }

                if (!refProductPrice.HasValue && !string.IsNullOrWhiteSpace(request.CurrentProductContext))
                {
                    var mPrice = System.Text.RegularExpressions.Regex.Match(request.CurrentProductContext, @"(\d+)\s*đ");
                    if (mPrice.Success && long.TryParse(mPrice.Groups[1].Value, out var cpVal))
                    {
                        refProductPrice = cpVal;
                    }
                }

                if (isRelativeLowerBudget)
                {
                    var maxBound = refProductPrice.HasValue 
                        ? (long)(refProductPrice.Value * 0.95) 
                        : (effectiveMaxBudget ?? 400000);

                    effectiveMaxBudget = maxBound;
                    effectiveMinBudget = null;
                    budgetType = "Below";

                    parsedIntent.IntentType = ChatIntentType.InventoryInquiry;
                    parsedIntent.BudgetType = "Below";
                    parsedIntent.MaxBudget = effectiveMaxBudget;
                    parsedIntent.MinBudget = null;
                    parsedIntent.ShouldIncludeProduct = true;
                    parsedIntent.IsValidGiftIntent = true;

                    var relText = !string.IsNullOrEmpty(effectiveRelationship) ? $" cho {effectiveRelationship}" : "";
                    parsedIntent.DirectAnswer = $"Dạ bên mình có các gợi ý quà tặng{relText} với mức giá mềm hơn (dưới {effectiveMaxBudget:N0}đ) rất thiết thực và chu đáo để bạn dễ dàng lựa chọn nhé:";
                }
                else if (isRelativeHigherBudget)
                {
                    var minBound = refProductPrice.HasValue 
                        ? (long)(refProductPrice.Value * 1.05) 
                        : (effectiveMinBudget ?? 500000);

                    effectiveMinBudget = minBound;
                    effectiveMaxBudget = null;
                    budgetType = "Above";

                    parsedIntent.IntentType = ChatIntentType.InventoryInquiry;
                    parsedIntent.BudgetType = "Above";
                    parsedIntent.MinBudget = effectiveMinBudget;
                    parsedIntent.MaxBudget = null;
                    parsedIntent.ShouldIncludeProduct = true;
                    parsedIntent.IsValidGiftIntent = true;

                    var relText = !string.IsNullOrEmpty(effectiveRelationship) ? $" cho {effectiveRelationship}" : "";
                    parsedIntent.DirectAnswer = $"Dạ bên mình có các lựa chọn quà tặng cao cấp hơn{relText} trên {effectiveMinBudget:N0}đ rất sang trọng để bạn tham khảo nhé:";
                }
            }

            parsedIntent.TargetGender = effectiveGender;
            parsedIntent.Relationship = effectiveRelationship;
            if (effectiveMinBudget.HasValue) parsedIntent.MinBudget = effectiveMinBudget;
            if (effectiveMaxBudget.HasValue) parsedIntent.MaxBudget = effectiveMaxBudget;
            if (!string.IsNullOrEmpty(budgetType)) parsedIntent.BudgetType = budgetType;

            if (!string.IsNullOrEmpty(parsedIntent.Summary))
            {
                parsedIntent.Summary = parsedIntent.Summary
                    .Replace("father", "bố", StringComparison.OrdinalIgnoreCase)
                    .Replace("mother", "mẹ", StringComparison.OrdinalIgnoreCase)
                    .Replace("dad", "bố", StringComparison.OrdinalIgnoreCase)
                    .Replace("mom", "mẹ", StringComparison.OrdinalIgnoreCase);
            }
        }

        // 3. Truy vấn Database cơ bản (Chỉ lấy sản phẩm đã duyệt VÀ đã được gắn ít nhất 1 Tag)
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p => p.Status == Domain.Enums.ProductStatus.Approved && p.ProductTags.Any());

        if (request.MinBudget.HasValue && request.MinBudget.Value > 0)
        {
            query = query.Where(p => p.Price >= request.MinBudget.Value);
        }

        if (request.MaxBudget.HasValue && request.MaxBudget.Value > 0)
        {
            query = query.Where(p => p.Price <= request.MaxBudget.Value);
        }

        if (request.OccasionTagId.HasValue)
        {
            query = query.Where(p => p.ProductTags.Any(pt => pt.TagId == request.OccasionTagId.Value));
        }

        if (request.InterestTagId.HasValue)
        {
            query = query.Where(p => p.ProductTags.Any(pt => pt.TagId == request.InterestTagId.Value));
        }

        var products = await query.ToListAsync(cancellationToken);

        // Loại trừ các sản phẩm đã từng gợi ý trong phiên chat (nếu có yêu cầu từ client)
        var excludedGuids = request.GetExcludedGuidList();
        if (excludedGuids.Any())
        {
            var excludeSet = excludedGuids.ToHashSet();
            var seenNames = products
                .Where(p => excludeSet.Contains(p.Id))
                .Select(p => p.Name.ToLowerInvariant().Trim())
                .ToList();

            var remaining = products.Where(p => 
                !excludeSet.Contains(p.Id) &&
                !seenNames.Any(sn => 
                    p.Name.ToLowerInvariant().Trim() == sn ||
                    (sn.Contains("instax") && p.Name.ToLowerInvariant().Contains("instax")) ||
                    (sn.Contains("bộ quà tặng lưu niệm") && p.Name.ToLowerInvariant().Contains("bộ quà tặng lưu niệm"))
                )
            ).ToList();

            if (remaining.Any())
            {
                products = remaining;
            }
        }

        // 4. Tính điểm phù hợp (Match Scoring) dựa trên từ khóa do AI bóc tách
        var interestKeywords = parsedIntent?.InterestKeywords ?? new List<string>();
        var occasionKeyword = parsedIntent?.Occasion;
        var relationshipKeyword = effectiveRelationship?.ToLowerInvariant();

        // Nhận diện nếu người dùng đang chủ động tìm đồ Sức khỏe / Thư giãn / Massage
        bool userAskedHealth = interestKeywords.Any(kw => kw.Contains("sức khỏe") || kw.Contains("thư giãn") || kw.Contains("y tế") || kw.Contains("massage") || kw.Contains("mát xa") || kw.Contains("bồi bổ"))
                               || promptText.Contains("sức khỏe") || promptText.Contains("thư giãn") || promptText.Contains("massage") || promptText.Contains("mát xa") || promptText.Contains("bồi bổ") || promptText.Contains("đau lưng") || promptText.Contains("cổ vai gáy");

        // Nhận diện nếu người dùng tìm đồ Uống trà / Thưởng trà / Bộ ấm chén bàn trà
        bool userAskedTea = interestKeywords.Any(kw => kw.Contains("thưởng trà") || kw.Contains("uống trà") || kw.Contains("ấm trà") || kw.Contains("ấm chén") || kw.Contains("pha trà") || kw.Contains("bàn trà") || kw.Contains("trà đạo"))
                            || promptText.Contains("thưởng trà") || promptText.Contains("uống trà") || promptText.Contains("ấm trà") || promptText.Contains("ấm chén") || promptText.Contains("pha trà") || promptText.Contains("bàn trà") || promptText.Contains("bộ ấm") || promptText.Contains("bình lọc trà") || promptText.Contains("trà đạo");

        // Nhận diện nếu người dùng tìm Bình giữ nhiệt / Tiện ích cá nhân
        bool userAskedThermos = interestKeywords.Any(kw => kw.Contains("giữ nhiệt") || kw.Contains("bình nước"))
                                || promptText.Contains("giữ nhiệt") || promptText.Contains("bình nước");

        // Nhận diện nếu người dùng tìm Ly / Cốc
        bool userAskedCup = interestKeywords.Any(kw => kw.Contains("cốc") || kw.Contains("ly ") || kw.Contains("cốc sứ") || kw.Contains("cốc đôi"))
                            || promptText.Contains("cốc sứ") || promptText.Contains("cốc đôi") || promptText.Contains("ly sứ") || promptText.Contains("ly giữ nhiệt") || promptText.Contains("cốc ");

        // Nhận diện nếu người dùng tìm Skincare / Mỹ phẩm / Làm đẹp
        bool userAskedSkincare = interestKeywords.Any(kw => kw.Contains("skincare") || kw.Contains("dưỡng da") || kw.Contains("mỹ phẩm") || kw.Contains("làm đẹp") || kw.Contains("son"))
                                 || promptText.Contains("skincare") || promptText.Contains("dưỡng da") || promptText.Contains("mỹ phẩm") || promptText.Contains("làm đẹp");

        // Nhận diện nếu người dùng tìm Quà Gaming / Chơi game / Game thủ
        bool userAskedGaming = interestKeywords.Any(kw => kw.Contains("game") || kw.Contains("gaming") || kw.Contains("chơi game") || kw.Contains("game thủ") || kw.Contains("esport"))
                               || promptText.Contains("game") || promptText.Contains("gaming") || promptText.Contains("chơi game") || promptText.Contains("game thủ") || promptText.Contains("esport");

        // Nhận diện nếu người dùng tìm Đồ Công nghệ / Phụ kiện PC / Máy tính / Thiết bị thông minh
        bool userAskedTech = userAskedGaming
                             || interestKeywords.Any(kw => kw.Contains("công nghệ") || kw.Contains("pc") || kw.Contains("máy tính") || kw.Contains("tai nghe") || kw.Contains("chuột") || kw.Contains("bàn phím") || kw.Contains("loa") || kw.Contains("thông minh") || kw.Contains("tiện ích") || kw.Contains("bluetooth") || kw.Contains("máy ảnh") || kw.Contains("instax"))
                             || promptText.Contains("công nghệ") || promptText.Contains("pc") || promptText.Contains("máy tính") || promptText.Contains("laptop") || promptText.Contains("tai nghe") || promptText.Contains("chuột") || promptText.Contains("bàn phím") || promptText.Contains("loa") || promptText.Contains("thông minh") || promptText.Contains("tiện ích") || promptText.Contains("máy ảnh") || promptText.Contains("instax");

        bool hasThemeRequest = userAskedTech || userAskedGaming || userAskedHealth || userAskedTea || userAskedThermos || userAskedSkincare;

        bool IsProductMatchingTheme(Product p)
        {
            var pText = (p.Name + " " + (p.Description ?? "")).ToLowerInvariant();
            if (userAskedGaming)
            {
                var gamingSynonyms = new[] { "gaming", "chơi game", "game thủ", "tai nghe", "chuột", "lót chuột", "bàn phím", "mouse pad", "rgb" };
                return !pText.Contains("rửa mặt") && gamingSynonyms.Any(kw => pText.Contains(kw));
            }
            if (userAskedTech)
            {
                var techSynonyms = new[] { 
                    "công nghệ", "tai nghe", "chuột", "bàn phím", "loa", "bluetooth", "máy tính", "laptop", 
                    "usb", "sạc", "pin dự phòng", "máy ảnh", "instax", "smartwatch", "đồng hồ thông minh", 
                    "máy rửa mặt", "thông minh", "fujifilm", "tiện ích", "hoco", "furycube" 
                };
                return techSynonyms.Any(kw => pText.Contains(kw));
            }
            if (userAskedHealth)
            {
                var healthSynonyms = new[] { 
                    "massage", "mát xa", "mát-xa", "cổ vai gáy", "ngải cứu", "hồng ngoại", "công thái học", 
                    "giảm đau", "ngủ ngon", "huyết áp", "sức khỏe", "yến", "yến sào", "thảo dược", "dinh dưỡng", 
                    "đông trùng", "nhân sâm", "trà thảo mộc" 
                };
                return healthSynonyms.Any(kw => pText.Contains(kw));
            }
            if (userAskedTea)
            {
                var teaSynonyms = new[] { "ấm trà", "ấm chén", "bình lọc trà", "ấm pha trà", "bộ ấm", "thưởng trà", "bàn trà", "pha trà", "ly pha trà" };
                return teaSynonyms.Any(kw => pText.Contains(kw));
            }
            if (userAskedThermos)
            {
                return pText.Contains("giữ nhiệt");
            }
            if (userAskedSkincare)
            {
                var skincareSynonyms = new[] { "skincare", "dưỡng da", "mỹ phẩm", "làm đẹp", "son", "rửa mặt", "serum", "kem dưỡng", "mặt nạ" };
                return skincareSynonyms.Any(kw => pText.Contains(kw));
            }
            return true;
        }

        // Danh sách từ khóa nhận diện sản phẩm đặc thù thuần giới tính (KHÔNG chứa từ khóa ngày lễ 20/10, 8/3, 20/11...)
        var strictlyFemaleIndicators = new[] { 
            "túi kẹp nách", "kẹp nách", "túi đeo vai", "túi tote", "clutch", "túi xách nữ", "nữ đeo chéo",
            "váy", "đầm", "chân váy", "áo lót", "nội y nữ", "nội y", "băng vệ sinh", 
            "son môi", "son dưỡng", "mascara", "phấn phủ", "má hồng", "phấn mắt", "trang điểm", "makeup",
            "kẹp tóc", "cột tóc", "băng đô", "bông tai", "khuyên tai", "hoa kẽm", "kẽm nhung", "hoa sáp",
            "dành cho nữ", "quà tặng bạn gái", "cho nàng", "tiểu thư", "xinh xắn", "dây chuyền nữ", "lắc tay nữ",
            "loopy", "gấu dâu", "chibi", "love box"
        };
        var strictlyMaleIndicators = new[] { 
            "thắt lưng nam", "ví nam", "ví da nam", "dây nịt nam", "cà vạt", "nước hoa nam", 
            "dao cạo", "cạo râu", "tông đơ", "dành cho nam", "quà tặng bạn trai", "cho chàng", "sáp vuốt tóc", 
            "ví nịt nam", "ví thắt lưng nam", "đồng hồ nam", "quà tặng nam"
        };

        // Hàm tính điểm liên quan cho từng sản phẩm
        int CalculateKeywordMatchScore(Product p)
        {
            int score = 0;
            var nameLower = p.Name.ToLowerInvariant();
            var descLower = (p.Description ?? "").ToLowerInvariant();

            // Kiểm tra tính tương thích giới tính
            if (effectiveGender == "Male")
            {
                // Nếu tên có chữ "nữ" mà KHÔNG có chữ "nam" (ví dụ: Nước hoa nữ, Giày nữ, Ví nữ...) -> 100% Đồ nữ!
                bool isFemaleGendered = nameLower.Contains("nữ") && !nameLower.Contains("nam");

                // Nếu có son (son môi, son thỏi, son kem, son tint...)
                bool isLipstick = nameLower.Contains("son ") || nameLower.Contains("son,") || nameLower.Contains("son+") || nameLower.Contains("son-") || nameLower.EndsWith("son");

                // Trang sức nữ tính (dây chuyền, lắc tay, vòng cổ, bông tai, khuyên tai không có chữ nam)
                bool isFemaleJewelry = (nameLower.Contains("dây chuyền") || nameLower.Contains("lắc tay") || 
                                        nameLower.Contains("vòng cổ") || nameLower.Contains("bông tai") || 
                                        nameLower.Contains("khuyên tai") || nameLower.Contains("vòng tay"))
                                        && !nameLower.Contains("nam");

                // Các từ ngữ chỉ đối tượng hoặc sản phẩm nữ tính đặc thù
                bool hasFemaleTarget = nameLower.Contains("cho nữ") || nameLower.Contains("dành cho nữ") || 
                                       nameLower.Contains("bạn nữ") || nameLower.Contains("phụ nữ") || 
                                       nameLower.Contains("cho nàng") || nameLower.Contains("bạn gái") ||
                                       nameLower.Contains("tặng mẹ") || nameLower.Contains("quà tặng mẹ") ||
                                       nameLower.Contains("tặng bà") || nameLower.Contains("cô giáo") ||
                                       nameLower.Contains("ngày của mẹ") ||
                                       nameLower.Contains("bó hoa") || nameLower.Contains("hoa gấu bông") ||
                                       nameLower.Contains("hoa hồng") || nameLower.Contains("khăn lụa");

                // Nếu tìm quà cho nam mà sản phẩm là đồ phụ nữ đặc trưng -> LOẠI THẲNG TAY
                if (isFemaleGendered || isLipstick || isFemaleJewelry || hasFemaleTarget || strictlyFemaleIndicators.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw) || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(kw))))
                {
                    return -9999;
                }

                if (strictlyMaleIndicators.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw) || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(kw))))
                {
                    score += 150;
                }

                if (nameLower.Contains("nam") || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains("nam")))
                {
                    score += 80;
                }
            }
            else if (effectiveGender == "Female")
            {
                // Nếu tên có chữ "nam" mà KHÔNG có chữ "nữ" -> 100% Đồ nam!
                bool isMaleGendered = nameLower.Contains("nam") && !nameLower.Contains("nữ");
                bool isMaleItem = nameLower.Contains("dao cạo") || nameLower.Contains("cạo râu") || nameLower.Contains("tông đơ") || nameLower.Contains("cà vạt");

                // Nếu tìm quà cho nữ mà sản phẩm là đồ nam giới đặc trưng -> LOẠI THẲNG TAY
                if (isMaleGendered || isMaleItem || strictlyMaleIndicators.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw) || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(kw))))
                {
                    return -9999;
                }

                if (strictlyFemaleIndicators.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw) || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(kw))))
                {
                    score += 150;
                }

                if (nameLower.Contains("nữ") || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains("nữ")))
                {
                    score += 80;
                }
            }

            // Khớp Dịp tặng
            if (!string.IsNullOrEmpty(occasionKeyword))
            {
                var occLower = occasionKeyword.ToLowerInvariant();
                if (nameLower.Contains(occLower) || descLower.Contains(occLower) ||
                    p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(occLower)))
                {
                    score += 50;
                }
            }

            // Khớp Đối tượng tặng (Relationship)
            if (!string.IsNullOrEmpty(relationshipKeyword))
            {
                if (nameLower.Contains(relationshipKeyword) || descLower.Contains(relationshipKeyword) ||
                    p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(relationshipKeyword)))
                {
                    score += 40;
                }

                if (relationshipKeyword.Contains("mẹ") || relationshipKeyword.Contains("bố mẹ") ||
                    relationshipKeyword.Contains("bố") || relationshipKeyword.Contains("ba") ||
                    relationshipKeyword.Contains("má") || relationshipKeyword.Contains("ông") || relationshipKeyword.Contains("bà"))
                {
                    // Ưu tiên cực cao cho sản phẩm đã được gắn tag chuyên biệt cho Bố mẹ / Người lớn tuổi
                    if (p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains("bố mẹ")))
                    {
                        score += 120;
                    }
                    else if (p.ProductTags.Any(pt => pt.Tag.Name.Contains("20/10") || pt.Tag.Name.Contains("8/3")))
                    {
                        score += 30;
                    }

                    // Quà biếu tao nhã, truyền thống cho người lớn tuổi (Ấm trà, bàn trà) - chỉ cộng khi không tìm đích danh sức khỏe
                    if (!userAskedHealth && (nameLower.Contains("ấm") || descLower.Contains("ấm") || nameLower.Contains("bình lọc trà")))
                    {
                        score += 60;
                    }

                    // Loại bỏ triệt để các món đồ tuổi teen xì-tin, đồ chơi hoặc máy ảnh selfie chụp liền hoàn toàn không hợp với cha mẹ
                    var teenInappropriateKeywords = new[] { 
                        "nơ", "loopy", "túi mù", "blindbox", "cosplay", "bó hoa noel", "lắp ráp bó hoa", "hộp nhạc",
                        "băng vệ sinh", "dung dịch vệ sinh", "ddvs", "makeup", "trang điểm", "tiểu thư", "chibi",
                        "instax", "fujifilm", "máy ảnh", "polaroid", "kẹp tóc", "cột tóc", "băng đô", "gấu dâu", "thú bông", "tai thỏ"
                    };
                    if (teenInappropriateKeywords.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw)))
                    {
                        score -= 500;
                    }
                }
                else if (relationshipKeyword.Contains("sếp") || relationshipKeyword.Contains("đối tác") || relationshipKeyword.Contains("thầy"))
                {
                    // Quà biếu trang trọng, lịch sự
                    if (!userAskedHealth && (nameLower.Contains("ấm") || descLower.Contains("ấm") || nameLower.Contains("gỗ sồi") || nameLower.Contains("yến")))
                    {
                        score += 60;
                    }
                }
                else if (relationshipKeyword.Contains("bạn gái") || relationshipKeyword.Contains("người yêu") || relationshipKeyword.Contains("crush"))
                {
                    if (p.ProductTags.Any(pt => pt.Tag.Name.Contains("Valentine") || pt.Tag.Name.Contains("20/10") || pt.Tag.Name.Contains("8/3")))
                    {
                        score += 30;
                    }

                    // Cốc đôi, bình giữ nhiệt, đồ quà tặng tình cảm
                    if (nameLower.Contains("cốc đôi") || nameLower.Contains("cốc") || nameLower.Contains("giữ nhiệt"))
                    {
                        score += 40;
                    }
                }
                else if (relationshipKeyword.Contains("bạn trai") || relationshipKeyword.Contains("nam"))
                {
                    if (p.ProductTags.Any(pt => pt.Tag.Name.Contains("Valentine") || pt.Tag.Name.ToLowerInvariant().Contains("nam")))
                    {
                        score += 30;
                    }

                    // Bình giữ nhiệt, tiện ích thực tế
                    if (nameLower.Contains("giữ nhiệt"))
                    {
                        score += 40;
                    }
                }
                else if (relationshipKeyword.Contains("đồng nghiệp") || relationshipKeyword.Contains("bạn thân") || relationshipKeyword.Contains("bạn"))
                {
                    // Tiện ích văn phòng: bình giữ nhiệt, bình lọc trà, cốc sứ
                    if (nameLower.Contains("giữ nhiệt") || nameLower.Contains("lọc trà") || nameLower.Contains("cốc"))
                    {
                        score += 50;
                    }
                }
            }

            // Khớp Sở thích / Ngành hàng (Bóc tách thông minh từ AI)
            foreach (var kw in interestKeywords)
            {
                var kwLower = kw.ToLowerInvariant();
                if (nameLower.Contains(kwLower) || descLower.Contains(kwLower) ||
                    p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(kwLower)))
                {
                    score += 60; // Tăng điểm ưu tiên cho từ khóa sở thích/vibe của AI
                }
            }

            // Mở rộng ngữ nghĩa thông minh: Khi người dùng tìm đồ Sức khỏe / Bồi bổ / Trị liệu / Massage / Dinh dưỡng
            if (userAskedHealth)
            {
                var healthSynonyms = new[] { 
                    "massage", "mát xa", "mát-xa", "cổ vai gáy", "ngải cứu", 
                    "hồng ngoại", "công thái học", "giảm đau", "ngủ ngon", "huyết áp", "sức khỏe",
                    "yến", "yến sào", "thảo dược", "dinh dưỡng", "đông trùng", "nhân sâm",
                    "trà thảo mộc", "trà dưỡng sinh", "trà dưỡng nhan", "trà hoa cúc", "trà an thần"
                };

                if (healthSynonyms.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw)))
                {
                    score += 350; // Ưu tiên vượt trội cho máy massage, gối cổ vai gáy, đồ sức khỏe, yến sào
                }
                else
                {
                    score -= 250; // Trừ mạnh các món không thuộc chủ đề sức khỏe (đồng hồ, ví da, thắt lưng...)
                }
            }

            // Mở rộng ngữ nghĩa: Khi người dùng tìm đồ Uống trà / Thưởng trà / Ấm chén
            if (userAskedTea)
            {
                var teaSynonyms = new[] { 
                    "ấm trà", "ấm chén", "bình lọc trà", "ấm pha trà", "bộ ấm", "thưởng trà", "bàn trà", "pha trà", "ly pha trà"
                };

                if (teaSynonyms.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw)))
                {
                    score += 300; // Ưu tiên vượt trội cho ấm chén, bình lọc trà khi tìm quà về trà
                }
            }

            // Mở rộng ngữ nghĩa: Khi người dùng tìm Bình giữ nhiệt
            if (userAskedThermos)
            {
                if (nameLower.Contains("giữ nhiệt") || descLower.Contains("giữ nhiệt"))
                {
                    score += 300;
                }
            }

            // Mở rộng ngữ nghĩa: Khi người dùng tìm Ly / Cốc
            if (userAskedCup)
            {
                if (nameLower.Contains("cốc") || nameLower.Contains("ly ") || descLower.Contains("cốc") || descLower.Contains("ly "))
                {
                    score += 300;
                }
            }

            // Mở rộng ngữ nghĩa: Khi người dùng tìm Skincare / Mỹ phẩm / Chăm sóc da
            if (userAskedSkincare)
            {
                if (nameLower.Contains("skincare") || nameLower.Contains("sữa rửa mặt") || nameLower.Contains("nước hoa hồng") || 
                    nameLower.Contains("tẩy trang") || nameLower.Contains("dưỡng") || nameLower.Contains("mỹ phẩm") || 
                    nameLower.Contains("son ") || nameLower.Contains("son môi") || descLower.Contains("skincare") || descLower.Contains("dưỡng da"))
                {
                    score += 350;
                }
            }

            // Mở rộng ngữ nghĩa: Khi người dùng tìm đồ Gaming / Chơi game / Game thủ
            if (userAskedGaming)
            {
                var gamingSynonyms = new[] {
                    "gaming", "chơi game", "tai nghe", "chuột", "lót chuột", "mouse pad", "rgb", "bàn phím", "pad chuột", "led rgb", "bluetooth", "hoco", "sidotech", "furycube", "lenovo"
                };

                // Loại trừ sản phẩm 'chuột rửa mặt' không phải chuột gaming
                bool isFacialMouse = nameLower.Contains("rửa mặt");

                if (!isFacialMouse && gamingSynonyms.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw)))
                {
                    score += 350; // Ưu tiên vượt trội cho thiết bị gaming thực thụ

                    if (nameLower.Contains("gaming") || descLower.Contains("gaming"))
                    {
                        score += 100;
                    }

                    if (p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains("công nghệ")))
                    {
                        score += 50;
                    }

                    // Ưu tiên khớp chính xác thiết bị cụ thể nếu người dùng nêu rõ (tai nghe / chuột / lót chuột / bàn phím)
                    if (promptText.Contains("tai nghe") && nameLower.Contains("tai nghe"))
                    {
                        score += 200;
                    }
                    else if (promptText.Contains("chuột") && nameLower.Contains("chuột"))
                    {
                        score += 200;
                    }
                    else if ((promptText.Contains("lót chuột") || promptText.Contains("pad")) && (nameLower.Contains("lót chuột") || nameLower.Contains("mouse pad")))
                    {
                        score += 200;
                    }

                    // Ưu tiên đặc biệt cho bạn nữ thích chơi game nếu sản phẩm có thiết kế tai mèo, màu trà sữa, dễ thương
                    if (effectiveGender == "Female" && (nameLower.Contains("tai mèo") || nameLower.Contains("trà sữa") || nameLower.Contains("hồng") || nameLower.Contains("cute") || nameLower.Contains("dễ thương")))
                    {
                        score += 150;
                    }
                }

                // Phạt nặng các sản phẩm hoàn toàn không phù hợp với người mê game (yến sào, hạt dinh dưỡng biếu đối tác, ấm chén bàn trà, dưỡng da, son môi, váy, chocolate, quà lưu niệm thông thường)
                var nonGamingInappropriate = new[] {
                    "yến", "yến sào", "gỗ sồi", "hạt dinh dưỡng", "đối tác", "ấm trà", "ấm chén", "bàn trà", "thưởng trà",
                    "dưỡng trắng", "serum", "mặt nạ", "son môi", "son dưỡng", "sữa rửa mặt", "nước hoa hồng", "váy", "đầm",
                    "chocolate", "nến thơm", "capybara", "hoa sáp", "dây chuyền", "vòng cổ"
                };

                if (nonGamingInappropriate.Any(kw => nameLower.Contains(kw)))
                {
                    score -= 500;
                }
            }
            if (hasThemeRequest)
            {
                if (IsProductMatchingTheme(p))
                {
                    score += 450;
                }
                else
                {
                    // Trừ cực nặng (-3000) để triệt tiêu mọi sản phẩm lạc đề khi người dùng đã yêu cầu chủ đề cụ thể (công nghệ, sức khỏe, trà...)
                    score -= 3000;
                }
            }

            return score;
        }

        // Lọc bỏ triệt để các sản phẩm bị xung đột giới tính (score <= -1000)
        products = products.Where(p => CalculateKeywordMatchScore(p) > -1000).ToList();

        var now = DateTime.UtcNow;
        bool IsActiveFeatured(Product p) => p.IsFeatured && p.FeaturedUntil.HasValue && p.FeaturedUntil.Value > now;

        string GetProductCategory(Product prod)
        {
            var text = (prod.Name + " " + (prod.Description ?? "")).ToLowerInvariant();
            
            // 1. Gaming & Công nghệ & Thiết bị thông minh
            if (text.Contains("gaming") || text.Contains("chơi game") || text.Contains("game thủ") || 
                text.Contains("tai nghe") || text.Contains("chuột") || text.Contains("lót chuột") || 
                text.Contains("bàn phím") || text.Contains("mouse pad") || text.Contains("rgb") || 
                text.Contains("công nghệ") || text.Contains("hoco") || text.Contains("sidotech") || 
                text.Contains("furycube") || text.Contains("lenovo") || text.Contains("bluetooth") ||
                text.Contains("máy ảnh") || text.Contains("instax") || text.Contains("fujifilm") ||
                text.Contains("loa") || text.Contains("smartwatch") || text.Contains("thông minh") ||
                text.Contains("máy rửa mặt"))
                return "Tech";

            // 0. Làm đẹp & Rửa mặt & Mỹ phẩm
            if (text.Contains("skincare") || text.Contains("son") || text.Contains("mỹ phẩm") || text.Contains("dưỡng"))
                return "Beauty";

            // 2. Sức khỏe & Thiết bị trị liệu
            if (text.Contains("massage") || text.Contains("ngải cứu") || text.Contains("gối") || text.Contains("sức khỏe") || text.Contains("công thái học") || text.Contains("hồng ngoại") || text.Contains("cổ vai gáy"))
                return "Health";
                
            // 2. Dinh dưỡng & Bồi bổ (Ăn/uống)
            if (text.Contains("saffron") || text.Contains("mật ong") || text.Contains("yến") || text.Contains("dinh dưỡng") || text.Contains("hạt") || text.Contains("đông trùng") || text.Contains("nhân sâm") || text.Contains("trà thảo mộc"))
                return "Nutrition";
                
            // 3. Bàn trà & Dụng cụ pha chế / Gia dụng / Bình giữ nhiệt / Ly cốc
            if (text.Contains("ấm") || text.Contains("bình lọc trà") || text.Contains("bình giữ nhiệt") || text.Contains("ấm chén") || text.Contains("bộ trà") || text.Contains("ly") || text.Contains("cốc") || text.Contains("pha trà"))
                return "Tableware";
                
            // 4. Trang sức & Phụ kiện
            if (text.Contains("dây chuyền") || text.Contains("lắc tay") || text.Contains("vòng cổ") || text.Contains("nhẫn") || text.Contains("bông tai") || text.Contains("khuyên tai") || text.Contains("bạc 925"))
                return "Jewelry";
                
            // 5. Làm đẹp & Hương thơm
            if (text.Contains("nến thơm") || text.Contains("tinh dầu") || text.Contains("son") || text.Contains("nước hoa") || text.Contains("mỹ phẩm") || text.Contains("dưỡng"))
                return "Beauty";
                
            // 6. Thời trang & Phụ kiện cá nhân
            if (text.Contains("ví") || text.Contains("thắt lưng") || text.Contains("túi xách") || text.Contains("khăn") || text.Contains("đồng hồ"))
                return "Fashion";
                
            // 7. Nhà cửa & Trang trí
            if (text.Contains("đèn") || text.Contains("pha lê") || text.Contains("decor") || text.Contains("gấu bông") || text.Contains("đồ chơi"))
                return "Home";
                
            return "General";
        }

        long? anchorBudget = null;
        if (effectiveMinBudget.HasValue && effectiveMaxBudget.HasValue)
        {
            anchorBudget = (effectiveMinBudget.Value + effectiveMaxBudget.Value) / 2;
        }
        else if (effectiveMaxBudget.HasValue)
        {
            anchorBudget = effectiveMaxBudget.Value;
        }
        else if (effectiveMinBudget.HasValue)
        {
            anchorBudget = effectiveMinBudget.Value;
        }

        // Hàm tính Tầng giá (Price Tier) theo đúng nguyên lý 3 tầng giá (Two-Way Tier 2 Relaxation):
        // Tier 1: Khớp chuẩn khoảng giá khách muốn (67% - 120% anchor)
        // Tier 2: Nới lỏng 2 chiều (Rẻ hơn: 50% - 67%; Cao hơn: 120% - 185%)
        // Tier 3: Quá xa khoảng giá (< 50% hoặc > 185%) -> LOẠI BỎ 100%
        int GetPriceTier(Product p)
        {
            if (!effectiveMinBudget.HasValue && !effectiveMaxBudget.HasValue)
                return 1;

            if (budgetType == "Above" && effectiveMinBudget.HasValue)
            {
                var min = effectiveMinBudget.Value;
                if (p.Price >= min && p.Price <= min * 2.5) return 1;
                // Nới lỏng 2 chiều hợp lý: cao hơn (> 2.5 min) HOẶC tiệm cận ngân sách (từ 80% min trở lên)
                if (p.Price > min * 2.5 || (p.Price >= min * 0.8 && p.Price < min)) return 2;
                return 3;
            }

            if (budgetType == "Below" && effectiveMaxBudget.HasValue)
            {
                var max = effectiveMaxBudget.Value;
                if (p.Price <= max && p.Price >= max * 0.4) return 1;
                // Chỉ nới lỏng theo hướng RẺ HƠN (25% - 40%), TUYỆT ĐỐI KHÔNG chọn món đắt hơn max!
                if (p.Price <= max && p.Price >= max * 0.25) return 2;
                return 3;
            }

            if (budgetType == "Range" && effectiveMinBudget.HasValue && effectiveMaxBudget.HasValue)
            {
                var min = effectiveMinBudget.Value;
                var max = effectiveMaxBudget.Value;
                if (p.Price >= min && p.Price <= max) return 1;
                if ((min > 0 && p.Price >= min * 0.7 && p.Price < min) || (p.Price > max && p.Price <= max * 1.35)) return 2;
                return 3;
            }

            if (anchorBudget.HasValue)
            {
                var anchor = anchorBudget.Value;
                if (p.Price >= anchor * 0.67 && p.Price <= anchor * 1.2) return 1;

                if ((p.Price >= anchor * 0.5 && p.Price < anchor * 0.67) ||
                    (p.Price > anchor * 1.2 && p.Price <= anchor * 1.85))
                {
                    return 2;
                }

                return 3;
            }

            return 1;
        }

        // LOẠI BỎ 100% các sản phẩm thuộc Tầng 3 (Quá xa hoặc giá cấm)
        products = products.Where(p => GetPriceTier(p) <= 2).ToList();

        if (!products.Any())
        {
            return new PagedRecommendationResult
            {
                Items = new List<ProductRecommendationDto>(),
                TotalCount = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                IsValidIntent = true,
                Message = "Hệ thống đã rà soát hết các món quà phù hợp trong tầm giá nới lỏng. Bạn có thể mở rộng thêm ngân sách hoặc chọn một sở thích cụ thể khác để AI tìm thêm nhé! ✨",
                InterpretedIntent = parsedIntent
            };
        }

        // Hàm tính điểm cơ bản (Base Score) cho sản phẩm theo từ khóa, độ hot, rating và cung hoàng đạo
        double CalculateBaseScore(Product p)
        {
            double score = CalculateKeywordMatchScore(p);

            if (effectiveZodiac.HasValue && p.SuitableZodiacs != null && p.SuitableZodiacs.Contains(effectiveZodiac.Value))
            {
                score += 40;
            }

            score += (p.PopularityScore * 0.1);
            score += (p.Rating * 5);

            return score;
        }

        // BR-25: Điểm ưu tiên cho Featured (cộng tối đa 20% + 15 điểm ưu tiên)
        double CalculateFeaturedScore(Product p)
        {
            return (CalculateBaseScore(p) * 1.2) + 15;
        }

        // [BR-25: Relevance First] Kiểm tra sản phẩm Featured có đủ điều kiện lọt vào nhóm quảng cáo ưu tiên không
        bool IsEligibleFeatured(Product p)
        {
            if (!IsActiveFeatured(p)) return false;

            // Loại bỏ sản phẩm lệch tầng giá quá xa (Tier 3)
            if (GetPriceTier(p) > 2) return false;

            // Kiểm tra độ liên quan
            bool hasSpecificIntent = !string.IsNullOrEmpty(occasionKeyword) ||
                                     !string.IsNullOrEmpty(relationshipKeyword) ||
                                     interestKeywords.Count > 0 ||
                                     effectiveZodiac.HasValue;

            if (hasSpecificIntent)
            {
                int keywordScore = CalculateKeywordMatchScore(p);
                bool matchesZodiac = effectiveZodiac.HasValue && p.SuitableZodiacs != null && p.SuitableZodiacs.Contains(effectiveZodiac.Value);

                if (keywordScore == 0 && !matchesZodiac)
                {
                    return false; // Lạc đề hoàn toàn -> không cho hiển thị trong slot tài trợ
                }
            }

            return true;
        }

        // 5. Thuật toán Xếp hạng & Xen kẽ Vị trí có điều kiện (Interleaved Slotting - Amazon & Shopee Pattern)
        // Tách các sản phẩm thành nhóm Featured đủ điều kiện và nhóm Organic
        var eligibleFeatured = products.Where(IsEligibleFeatured).ToList();
        var eligibleFeaturedIds = eligibleFeatured.Select(f => f.Id).ToHashSet();
        var organicCandidates = products.Where(p => !eligibleFeaturedIds.Contains(p.Id)).ToList();

        // Xoay vòng thông minh có trọng số
        double GetOrganicRotationScore(Product p)
        {
            var baseScore = CalculateBaseScore(p);
            if (baseScore <= 0) return baseScore;
            var jitter = Random.Shared.NextDouble() * Math.Min(40.0, Math.Max(5.0, baseScore * 0.15));
            return baseScore + jitter;
        }

        double GetFeaturedRotationScore(Product p)
        {
            var featScore = CalculateFeaturedScore(p);
            if (featScore <= 0) return featScore;
            var jitter = Random.Shared.NextDouble() * Math.Min(40.0, Math.Max(5.0, featScore * 0.15));
            return featScore + jitter;
        }

        // [TẦNG 2: Unified LLM Reranker & Concierge]
        bool top1IsFeatured = false;
        Product? top1Product = null;
        string? top1Advice = null;

        if (request.PageNumber == 1 && products.Any())
        {
            // Tách các sản phẩm khả dụng theo Tầng giá
            var tier1Pool = products
                .Where(p => GetPriceTier(p) == 1)
                .OrderByDescending(p => IsActiveFeatured(p) ? GetFeaturedRotationScore(p) : GetOrganicRotationScore(p))
                .ToList();

            var tier2Pool = products
                .Where(p => GetPriceTier(p) == 2)
                .OrderByDescending(p => IsActiveFeatured(p) ? GetFeaturedRotationScore(p) : GetOrganicRotationScore(p))
                .ToList();

            // Lấy mẻ 8 - 12 sản phẩm gửi cho AI thẩm định (Sliding Window)
            var candidateBatch = new List<Product>();

            if (hasThemeRequest)
            {
                // Khi người dùng yêu cầu một chủ đề cụ thể (Công nghệ, Gaming, Sức khỏe, Trà...):
                // Tiêu chí CHỦ ĐỀ là ưu tiên số 1!
                // Gom các sản phẩm thuộc Tier 1 và Tier 2 thực sự khớp đúng chủ đề lên danh sách thẩm định!
                var themeMatchedCandidates = products
                    .Where(p => IsProductMatchingTheme(p) && CalculateKeywordMatchScore(p) > 0)
                    .OrderBy(p => GetPriceTier(p)) // Ưu tiên Tier 1 trước Tier 2 trong số các món khớp chủ đề
                    .ThenByDescending(p => IsActiveFeatured(p) ? GetFeaturedRotationScore(p) : GetOrganicRotationScore(p))
                    .ToList();

                if (themeMatchedCandidates.Any())
                {
                    // BẮT BUỘC CHỈ gửi sản phẩm khớp chủ đề để AI thẩm định!
                    // TUYỆT ĐỐI KHÔNG đệm các sản phẩm lạc đề (như nước hoa, dây chuyền) vào làm AI chọn nhầm!
                    candidateBatch.AddRange(themeMatchedCandidates.Take(12));
                }
            }
            else if ((budgetType == "Above" || budgetType == "Below") && tier1Pool.Any())
            {
                // Khi người dùng yêu cầu mốc giá cụ thể (trên X hoặc dưới X), ưu tiên tối đa các món chuẩn Tier 1 nếu còn hàng
                candidateBatch.AddRange(tier1Pool.Take(12));
                var needed = 12 - candidateBatch.Count;
                if (needed > 0)
                {
                    candidateBatch.AddRange(tier2Pool.Take(needed));
                }
            }
            else if (tier1Pool.Count >= 8)
            {
                candidateBatch.AddRange(tier1Pool.Take(8));
                candidateBatch.AddRange(tier2Pool.Take(4));
            }
            else
            {
                candidateBatch.AddRange(tier1Pool);
                var needed = 12 - candidateBatch.Count;
                if (needed > 0)
                {
                    candidateBatch.AddRange(tier2Pool.Take(needed));
                }
            }

            if (candidateBatch.Any())
            {
                var candidateDtos = candidateBatch.Select(p => new CandidateProductItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    PriceTier = GetPriceTier(p),
                    Category = GetProductCategory(p),
                    Rating = p.Rating,
                    TotalReviews = p.TotalReviews
                }).ToList();

                var targetRel = effectiveRelationship;
                if (string.IsNullOrEmpty(targetRel))
                {
                    if (effectiveGender == "Male") targetRel = "đồng nghiệp nam";
                    else if (effectiveGender == "Female") targetRel = "đồng nghiệp nữ";
                    else targetRel = "người nhận";
                }

                var zodiacName = effectiveZodiac.HasValue ? GetZodiacName(effectiveZodiac.Value) : null;

                var themeList = new List<string>();
                if (userAskedTech) themeList.Add("Công nghệ, thiết bị thông minh, phụ kiện công nghệ tiện ích");
                if (userAskedGaming) themeList.Add("Gaming, chơi game, thiết bị game");
                if (userAskedHealth) themeList.Add("Sức khỏe, bồi bổ, massage, thư giãn");
                if (userAskedTea) themeList.Add("Uống trà, ấm chén, trà đạo");
                if (userAskedThermos) themeList.Add("Bình giữ nhiệt, tiện ích cá nhân");
                if (interestKeywords.Any()) themeList.AddRange(interestKeywords);
                var themeStr = themeList.Any() ? string.Join(", ", themeList.Distinct()) : null;

                var rerankResult = await _aiService.RerankAndAdviseAsync(
                    candidateDtos,
                    targetRel,
                    occasionKeyword,
                    effectiveGender,
                    anchorBudget,
                    zodiacName,
                    themeStr,
                    cancellationToken);

                if (rerankResult.SelectedProductId.HasValue)
                {
                    top1Product = products.FirstOrDefault(p => p.Id == rerankResult.SelectedProductId.Value);
                    top1Advice = rerankResult.Advice;
                }

                // Chỉ fallback nếu mẻ ứng viên còn món thật sự phù hợp giới tính (score > 0)
                if (top1Product == null)
                {
                    top1Product = candidateBatch.FirstOrDefault(p => CalculateKeywordMatchScore(p) > 0);
                    if (top1Product != null && !string.IsNullOrWhiteSpace(rerankResult.Advice))
                    {
                        top1Advice = rerankResult.Advice;
                    }
                }

                if (top1Product != null)
                {
                    top1IsFeatured = IsEligibleFeatured(top1Product);
                }
                else
                {
                    // Kích hoạt Graceful Handover: Đã hết sạch quà phù hợp tiêu chí
                    return new PagedRecommendationResult
                    {
                        Items = new List<ProductRecommendationDto>(),
                        TotalCount = 0,
                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize,
                        IsValidIntent = true,
                        Message = "Hệ thống đã rà soát hết các món quà phù hợp trong tầm giá nới lỏng. Bạn có thể mở rộng thêm ngân sách hoặc chọn một sở thích cụ thể khác để AI tìm thêm nhé! ✨",
                        InterpretedIntent = parsedIntent
                    };
                }
            }
        }
        else if (products.Any())
        {
            top1Product = products.FirstOrDefault();
            if (top1Product != null) top1IsFeatured = IsEligibleFeatured(top1Product);
        }

        // Danh sách các ứng viên còn lại sau khi đã chốt Top 1
        var remainingFeatured = eligibleFeatured
            .Where(p => top1Product == null || p.Id != top1Product.Id)
            .OrderBy(p => GetPriceTier(p))
            .ThenByDescending(GetFeaturedRotationScore)
            .ToList();

        List<Product> ApplyCategoryDiversity(List<Product> sourceList, string? startingCat = null)
        {
            if (sourceList.Count <= 2) return sourceList;

            var result = new List<Product>();
            var pool = new List<Product>(sourceList);
            var lastCat = startingCat ?? "";

            while (pool.Count > 0)
            {
                Product? candidate = null;

                // Nếu người dùng có nhu cầu ngành hàng cụ thể:
                // 1. Sức khỏe -> Ưu tiên 100% Health & Nutrition
                if (userAskedHealth)
                {
                    var healthCandidate = pool.FirstOrDefault(p => GetProductCategory(p) == "Health" || GetProductCategory(p) == "Nutrition");
                    if (healthCandidate != null)
                    {
                        candidate = healthCandidate;
                    }
                }
                // 2. Thưởng trà -> Ưu tiên Tableware & đồ trà
                else if (userAskedTea)
                {
                    var teaCandidate = pool.FirstOrDefault(p => GetProductCategory(p) == "Tableware" && (p.Name.ToLowerInvariant().Contains("trà") || (p.Description ?? "").ToLowerInvariant().Contains("trà")));
                    if (teaCandidate != null)
                    {
                        candidate = teaCandidate;
                    }
                }
                // 3. Bình giữ nhiệt -> Ưu tiên đồ giữ nhiệt
                else if (userAskedThermos)
                {
                    var thermosCandidate = pool.FirstOrDefault(p => GetProductCategory(p) == "Tableware" && (p.Name.ToLowerInvariant().Contains("giữ nhiệt") || (p.Description ?? "").ToLowerInvariant().Contains("giữ nhiệt")));
                    if (thermosCandidate != null)
                    {
                        candidate = thermosCandidate;
                    }
                }
                // 4. Ly / Cốc -> Ưu tiên cốc sứ, ly
                else if (userAskedCup)
                {
                    var cupCandidate = pool.FirstOrDefault(p => GetProductCategory(p) == "Tableware" && (p.Name.ToLowerInvariant().Contains("cốc") || p.Name.ToLowerInvariant().Contains("ly")));
                    if (cupCandidate != null)
                    {
                        candidate = cupCandidate;
                    }
                }
                // 5. Skincare / Làm đẹp -> Ưu tiên Beauty & mỹ phẩm
                else if (userAskedSkincare)
                {
                    var beautyCandidate = pool.FirstOrDefault(p => GetProductCategory(p) == "Beauty" || p.Name.ToLowerInvariant().Contains("skincare") || p.Name.ToLowerInvariant().Contains("dưỡng"));
                    if (beautyCandidate != null)
                    {
                        candidate = beautyCandidate;
                    }
                }
                // 6. Gaming / Chơi game / Công nghệ -> Ưu tiên 100% Tech / Gaming
                else if (userAskedGaming || userAskedTech)
                {
                    var techCandidate = pool.FirstOrDefault(p => GetProductCategory(p) == "Tech");
                    if (techCandidate != null)
                    {
                        candidate = techCandidate;
                    }
                }

                if (candidate == null)
                {
                    // Ưu tiên chọn sản phẩm cùng Tier nhưng Category khác với món trước đó để tạo độ phong phú
                    candidate = pool.FirstOrDefault(p => GetProductCategory(p) != lastCat) ?? pool[0];
                }

                result.Add(candidate);
                lastCat = GetProductCategory(candidate);
                pool.Remove(candidate);
            }

            return result;
        }

        var rawRemainingOrganic = organicCandidates
            .Where(p => top1Product == null || p.Id != top1Product.Id)
            .OrderBy(p => GetPriceTier(p))
            .ThenByDescending(GetOrganicRotationScore)
            .ToList();

        var remainingOrganic = ApplyCategoryDiversity(
            rawRemainingOrganic, 
            top1Product != null ? GetProductCategory(top1Product) : null
        );

        // [Quy tắc 3: Lắp ráp danh sách theo công thức Xen kẽ]
        var rankedItems = new List<(Product Product, bool IsFeaturedSlot, string? BadgeLabel)>();

        if (top1Product != null)
        {
            rankedItems.Add((
                top1Product, 
                top1IsFeatured, 
                top1IsFeatured ? "Top Pick • Đối tác" : null
            ));

            int currentPos = 2;
            int featIndex = 0;
            int orgIndex = 0;

            bool IsTargetFeaturedSlot(int pos)
            {
                if (top1IsFeatured)
                {
                    // Top 1 đã là Featured -> các slot tiếp theo: Vị trí #5, #9 (giữa luôn có 3 organic)
                    if (pos == 5 || pos == 9) return true;
                    if (pos > 10 && (pos - 9) % 5 == 0) return true;
                }
                else
                {
                    // Top 1 là Organic -> các slot tiếp theo: Vị trí #3, #7 (giữa luôn có 3 organic)
                    if (pos == 3 || pos == 7) return true;
                    if (pos > 10 && (pos - 7) % 5 == 0) return true;
                }
                return false;
            }

            while (rankedItems.Count < products.Count)
            {
                bool wantFeatured = IsTargetFeaturedSlot(currentPos);

                if (wantFeatured && featIndex < remainingFeatured.Count)
                {
                    var fProd = remainingFeatured[featIndex++];
                    rankedItems.Add((fProd, true, "Được tài trợ"));
                }
                else if (orgIndex < remainingOrganic.Count)
                {
                    var oProd = remainingOrganic[orgIndex++];
                    rankedItems.Add((oProd, false, null));
                }
                else if (featIndex < remainingFeatured.Count)
                {
                    var fProd = remainingFeatured[featIndex++];
                    rankedItems.Add((fProd, true, "Được tài trợ"));
                }

                currentPos++;
            }
        }

        int totalCount = rankedItems.Count;

        // 6. Phân trang
        var pagedProducts = rankedItems
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new ProductRecommendationDto
            {
                Id = item.Product.Id,
                Name = item.Product.Name,
                Price = item.Product.Price,
                ImageUrl = item.Product.ImageUrl,
                AffiliateUrl = item.Product.AffiliateUrl,
                OriginalUrl = item.Product.OriginalUrl,
                Rating = item.Product.Rating,
                TotalReviews = item.Product.TotalReviews,
                IsFeatured = item.IsFeaturedSlot,
                BadgeLabel = item.BadgeLabel
            }).ToList();

        // 7. Gắn lời khuyên cho Món Quà Top 1 ở trang đầu tiên (ưu tiên lời khuyên từ Unified Reranker)
        if (request.PageNumber == 1 && pagedProducts.Any())
        {
            var topGift = pagedProducts.First();
            if (!string.IsNullOrWhiteSpace(top1Advice))
            {
                topGift.AiAdvice = top1Advice;
            }
            else
            {
                var zodiacName = effectiveZodiac.HasValue 
                    ? GetZodiacName(effectiveZodiac.Value) 
                    : null;
                var effectiveOccasion = parsedIntent?.Occasion;

                var targetRelationship = effectiveRelationship;
                if (string.IsNullOrEmpty(targetRelationship))
                {
                    if (effectiveGender == "Male") targetRelationship = "bạn nam";
                    else if (effectiveGender == "Female") targetRelationship = "bạn nữ";
                }

                bool isRelaxed = top1Product != null && GetPriceTier(top1Product) == 2;
                long? budgetTarget = effectiveMaxBudget ?? effectiveMinBudget;

                topGift.AiAdvice = await _aiService.GenerateGiftAdviceAsync(
                    topGift.Name, 
                    zodiacName, 
                    targetRelationship, 
                    effectiveAge,
                    effectiveOccasion,
                    isRelaxed,
                    budgetTarget);
            }
        }

        // 8. Sinh bản ghi log tìm kiếm để phục vụ đánh giá ngầm (UC-04)
        var recommendationLog = new RecommendationLog(
            searchText: request.Prompt,
            recipient: effectiveRelationship,
            occasion: request.OccasionTagId?.ToString() ?? parsedIntent?.Occasion,
            budgetMin: effectiveMinBudget,
            budgetMax: effectiveMaxBudget,
            hobbies: request.InterestTagId?.ToString() ?? string.Join(", ", interestKeywords),
            resultCount: totalCount
        );

        _context.RecommendationLogs.Add(recommendationLog);
        await _context.SaveChangesAsync(cancellationToken);

        // Chuẩn hóa tên cung hoàng đạo sang tiếng Việt trong Summary nếu AI lỡ viết tiếng Anh
        if (parsedIntent != null && effectiveZodiac.HasValue && !string.IsNullOrEmpty(parsedIntent.Summary))
        {
            var enZodiac = effectiveZodiac.Value.ToString();
            var vnZodiac = GetZodiacName(effectiveZodiac.Value);
            parsedIntent.Summary = parsedIntent.Summary.Replace(enZodiac, vnZodiac, StringComparison.OrdinalIgnoreCase);
        }

        // Lọc câu hỏi gợi ý để đảm bảo không trùng lặp với câu vừa hỏi và không mâu thuẫn với ý định giá
        if (parsedIntent != null)
        {
            parsedIntent.SuggestedQuestions ??= new List<string>();
            var promptNorm = (request.Prompt ?? "").Trim().ToLowerInvariant();

            // Loại bỏ câu gợi ý trùng hoặc quá giống với câu hỏi người dùng vừa nhập
            parsedIntent.SuggestedQuestions.RemoveAll(q =>
            {
                var qNorm = q.Trim().ToLowerInvariant();
                if (qNorm == promptNorm) return true;
                if (promptNorm.Length > 8 && (qNorm.Contains(promptNorm) || promptNorm.Contains(qNorm))) return true;

                // Nếu câu hỏi người dùng vừa nhập đã có phong cách này rồi thì không gợi ý lại
                var styleKeys = new[] { "sức khỏe", "thư giãn", "tinh tế", "sang trọng", "lịch lãm", "thiết thực", "tiện dụng", "hộp quà", "kèm thiệp", "công nghệ" };
                foreach (var sk in styleKeys)
                {
                    if (promptNorm.Contains(sk) && qNorm.Contains(sk)) return true;
                }
                return false;
            });

            if (budgetType == "Below" || promptText.Contains("thấp hơn") || promptText.Contains("rẻ hơn") || promptText.Contains("mềm hơn"))
            {
                parsedIntent.SuggestedQuestions.RemoveAll(q => 
                    q.Contains("trên 500k", StringComparison.OrdinalIgnoreCase) ||
                    q.Contains("trên 1", StringComparison.OrdinalIgnoreCase) ||
                    q.Contains("đắt hơn", StringComparison.OrdinalIgnoreCase) ||
                    q.Contains("tăng ngân sách", StringComparison.OrdinalIgnoreCase));
            }
            else if (budgetType == "Above" || promptText.Contains("cao hơn") || promptText.Contains("đắt hơn"))
            {
                parsedIntent.SuggestedQuestions.RemoveAll(q => 
                    q.Contains("dưới 200k", StringComparison.OrdinalIgnoreCase) ||
                    q.Contains("dưới 300k", StringComparison.OrdinalIgnoreCase) ||
                    q.Contains("rẻ hơn", StringComparison.OrdinalIgnoreCase));
            }

            // Bổ sung các câu hỏi gợi ý tinh chỉnh chất lượng cao nếu danh sách chưa đủ 3
            var freshPool = new List<string>
            {
                "Tìm quà mang phong cách tinh tế, sang trọng",
                "Quà có hộp đóng gói kèm thiệp chúc mừng",
                "Tìm quà thiết thực, tiện dụng hàng ngày",
                "Đổi sang mức giá trên 500k",
                "Quà thời trang, phụ kiện xinh xắn",
                "Quà công nghệ thông minh, tiện ích",
                "Quà chăm sóc sức khỏe, thư giãn"
            };

            foreach (var fb in freshPool)
            {
                if (parsedIntent.SuggestedQuestions.Count >= 3) break;
                var fbNorm = fb.ToLowerInvariant();

                bool hasKeywordOverlap = false;
                var styleKeys = new[] { "sức khỏe", "thư giãn", "tinh tế", "sang trọng", "lịch lãm", "thiết thực", "tiện dụng", "hộp quà", "kèm thiệp", "công nghệ" };
                foreach (var sk in styleKeys)
                {
                    if (promptNorm.Contains(sk) && fbNorm.Contains(sk))
                    {
                        hasKeywordOverlap = true;
                        break;
                    }
                }

                if (!hasKeywordOverlap &&
                    !parsedIntent.SuggestedQuestions.Any(sq => sq.ToLowerInvariant() == fbNorm) &&
                    !promptNorm.Contains(fbNorm))
                {
                    parsedIntent.SuggestedQuestions.Add(fb);
                }
            }
        }

        return new PagedRecommendationResult
        {
            Items = pagedProducts,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            RecommendationLogId = recommendationLog.Id,
            InterpretedIntent = parsedIntent,
            IsValidIntent = true,
            Message = parsedIntent?.Summary,
            DirectAnswer = parsedIntent?.DirectAnswer,
            IntentType = parsedIntent?.IntentType.ToString()
        };
    }

    private string GetZodiacName(Domain.Enums.ZodiacSign sign)
    {
        return sign switch
        {
            Domain.Enums.ZodiacSign.Aries => "Bạch Dương",
            Domain.Enums.ZodiacSign.Taurus => "Kim Ngưu",
            Domain.Enums.ZodiacSign.Gemini => "Song Tử",
            Domain.Enums.ZodiacSign.Cancer => "Cự Giải",
            Domain.Enums.ZodiacSign.Leo => "Sư Tử",
            Domain.Enums.ZodiacSign.Virgo => "Xử Nữ",
            Domain.Enums.ZodiacSign.Libra => "Thiên Bình",
            Domain.Enums.ZodiacSign.Scorpio => "Bọ Cạp",
            Domain.Enums.ZodiacSign.Sagittarius => "Nhân Mã",
            Domain.Enums.ZodiacSign.Capricorn => "Ma Kết",
            Domain.Enums.ZodiacSign.Aquarius => "Bảo Bình",
            Domain.Enums.ZodiacSign.Pisces => "Song Ngư",
            _ => sign.ToString()
        };
    }
}
