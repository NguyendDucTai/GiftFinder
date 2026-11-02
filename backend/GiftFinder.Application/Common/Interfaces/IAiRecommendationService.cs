using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

namespace GiftFinder.Application.Common.Interfaces;

public interface IAiRecommendationService
{
    /// <summary>
    /// Đọc hiểu câu Prompt tự nhiên của người dùng và bóc tách các tiêu chí tìm kiếm quà tặng (Dịp, Người nhận, Tuổi, Cung, Giá, Sở thích).
    /// </summary>
    Task<ParsedRecommendationIntent> ParsePromptAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thẩm định mẻ sản phẩm ứng viên (8-12 món), loại bỏ món sai lệch giới tính/persona,
    /// chọn ra món quà xuất sắc nhất và sinh lời tư vấn 2 câu tinh tế cho người nhận.
    /// </summary>
    Task<AiRerankResultDto> RerankAndAdviseAsync(
        List<CandidateProductItemDto> candidates,
        string targetRecipient,
        string? occasion = null,
        string? targetGender = null,
        long? targetBudget = null,
        string? zodiacSign = null,
        string? interestOrTheme = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo ra một đoạn văn ngắn (2-3 câu) giải thích tại sao món quà này lại hợp với người nhận dựa trên Cung Hoàng Đạo.
    /// </summary>
    /// <param name="productName">Tên món quà</param>
    /// <param name="zodiacSign">Cung Hoàng Đạo của người nhận (nếu có)</param>
    /// <param name="relationship">Mối quan hệ (tùy chọn)</param>
    /// <param name="age">Độ tuổi (tùy chọn)</param>
    /// <param name="occasion">Dịp tặng quà (tùy chọn)</param>
    /// <returns>Đoạn văn tư vấn từ AI</returns>
    Task<string> GenerateGiftAdviceAsync(
        string productName, 
        string? zodiacSign = null, 
        string? relationship = null, 
        int? age = null, 
        string? occasion = null,
        bool isRelaxedBudget = false,
        long? targetBudget = null);

    /// <summary>
    /// Giám định chất lượng sản phẩm nhập từ sàn TMĐT và tự động gắn các Tag Dịp tặng, Sở thích, Cung hoàng đạo phù hợp.
    /// </summary>
    Task<GiftFinder.Application.Features.Affiliate.DTOs.ProductTaggingResult> ClassifyAndTagProductAsync(
        string productName, 
        string? description, 
        long price, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Trả lời câu hỏi thắc mắc của người dùng về một sản phẩm cụ thể đang xem (công năng, chất liệu, tính phù hợp...)
    /// </summary>
    Task<string> AnswerProductInquiryAsync(
        string productName,
        string? productDescription,
        long productPrice,
        string userQuestion,
        CancellationToken cancellationToken = default);
}
