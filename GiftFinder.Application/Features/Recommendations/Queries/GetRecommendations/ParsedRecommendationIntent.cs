using GiftFinder.Domain.Enums;

namespace GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

/// <summary>
/// Các tiêu chí và ý định tìm kiếm do AI bóc tách từ câu Prompt tự nhiên của người dùng
/// </summary>
public class ParsedRecommendationIntent
{
    public string? Relationship { get; set; }           // Ví dụ: "bạn gái", "mẹ", "bạn trai", "đồng nghiệp"
    public int? TargetAge { get; set; }                 // Tuổi của người nhận (nếu có)
    public ZodiacSign? TargetZodiac { get; set; }       // Cung hoàng đạo (nếu có nhắc đến)
    public long? MaxBudget { get; set; }                // Ngân sách tối đa (VNĐ)
    public string? Occasion { get; set; }               // Dịp: "sinh nhật", "20/10", "valentine", "giáng sinh"...
    public List<string> InterestKeywords { get; set; } = new(); // Các từ khóa sở thích: "chụp ảnh", "decor", "công nghệ"...
    public string? Summary { get; set; }                // Tóm tắt ngắn gọn những gì AI hiểu

    // AI Guardrail & Input Moderation
    public bool IsValidGiftIntent { get; set; } = true;  // True nếu câu hỏi đúng chủ đề quà tặng, False nếu lạc đề/quấy rối
    public string? AiMessage { get; set; }               // Lời giải thích, nhắc nhở hoặc hướng dẫn lịch sự của AI
    public List<string> SuggestedQuestions { get; set; } = new(); // Các câu hỏi mẫu để người dùng thử lại
}
