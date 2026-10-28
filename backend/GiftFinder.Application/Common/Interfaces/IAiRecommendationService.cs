using GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

namespace GiftFinder.Application.Common.Interfaces;

public interface IAiRecommendationService
{
    /// <summary>
    /// Đọc hiểu câu Prompt tự nhiên của người dùng và bóc tách các tiêu chí tìm kiếm quà tặng (Dịp, Người nhận, Tuổi, Cung, Giá, Sở thích).
    /// </summary>
    Task<ParsedRecommendationIntent> ParsePromptAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo ra một đoạn văn ngắn (2-3 câu) giải thích tại sao món quà này lại hợp với người nhận dựa trên Cung Hoàng Đạo.
    /// </summary>
    /// <param name="productName">Tên món quà</param>
    /// <param name="zodiacSign">Cung Hoàng Đạo của người nhận (nếu có)</param>
    /// <param name="relationship">Mối quan hệ (tùy chọn)</param>
    /// <param name="age">Độ tuổi (tùy chọn)</param>
    /// <param name="occasion">Dịp tặng quà (tùy chọn)</param>
    /// <returns>Đoạn văn tư vấn từ AI</returns>
    Task<string> GenerateGiftAdviceAsync(string productName, string? zodiacSign = null, string? relationship = null, int? age = null, string? occasion = null);

    /// <summary>
    /// Giám định chất lượng sản phẩm nhập từ sàn TMĐT và tự động gắn các Tag Dịp tặng, Sở thích, Cung hoàng đạo phù hợp.
    /// </summary>
    Task<GiftFinder.Application.Features.Affiliate.DTOs.ProductTaggingResult> ClassifyAndTagProductAsync(
        string productName, 
        string? description, 
        long price, 
        CancellationToken cancellationToken = default);
}

