using GiftFinder.Domain.Enums;

namespace GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

/// <summary>
/// Loại ý định của người dùng khi trò chuyện với AI Concierge
/// </summary>
public enum ChatIntentType
{
    Recommendation = 1,     // Tìm kiếm / Gợi ý quà tặng thông thường
    InventoryInquiry = 2,   // Thắc mắc về kho hàng (vd: "Không có món nào trên 500k à?", "Có quà gì độc lạ không?")
    ConsultingAdvice = 3,   // Tư vấn lựa chọn, so sánh, xin lời khuyên gu quà tặng (vd: "30 tuổi nên tặng ví hay đồng hồ?")
    ProductDetailQnA = 4,   // Hỏi đáp về sản phẩm đang xem (vd: "Món này có chống nước không?", "Dây da hay kim loại?")
    ServicePolicy = 5,      // Hỏi chính sách dịch vụ (vd: bọc quà, thiệp mừng, giao hàng, đổi trả)
    OffTopic = 6            // Lạc đề (toán, thời tiết, tin tức...)
}

/// <summary>
/// Các tiêu chí và ý định tìm kiếm do AI bóc tách từ câu Prompt tự nhiên của người dùng
/// </summary>
public class ParsedRecommendationIntent
{
    public ChatIntentType IntentType { get; set; } = ChatIntentType.Recommendation;
    public string? DirectAnswer { get; set; }           // Câu trả lời đàm thoại trực tiếp cho thắc mắc của người dùng
    public bool ShouldIncludeProduct { get; set; } = true; // True nếu cần hiển thị Card sản phẩm kèm theo

    public string? Relationship { get; set; }           // Ví dụ: "bạn gái", "mẹ", "bạn trai", "đồng nghiệp"
    public string? TargetGender { get; set; }           // Giới tính người nhận: "Male", "Female", "Unisex"
    public int? TargetAge { get; set; }                 // Tuổi của người nhận (nếu có)
    public ZodiacSign? TargetZodiac { get; set; }       // Cung hoàng đạo (nếu có nhắc đến)
    public long? MinBudget { get; set; }                // Ngân sách tối thiểu (VNĐ) khi khách nói "trên 200k"
    public long? MaxBudget { get; set; }                // Ngân sách tối đa (VNĐ) khi khách nói "dưới 200k"
    public string? BudgetType { get; set; }             // "Above", "Below", "Around", "Range"
    public string? Occasion { get; set; }               // Dịp: "sinh nhật", "20/10", "valentine", "giáng sinh"...
    public List<string> InterestKeywords { get; set; } = new(); // Các từ khóa sở thích: "chụp ảnh", "decor", "công nghệ"...
    public string? Summary { get; set; }                // Tóm tắt ngắn gọn những gì AI hiểu

    // AI Guardrail & Input Moderation
    public bool IsValidGiftIntent { get; set; } = true;  // True nếu câu hỏi đúng chủ đề quà tặng, False nếu lạc đề/quấy rối
    public string? AiMessage { get; set; }               // Lời giải thích, nhắc nhở hoặc hướng dẫn lịch sự của AI
    public List<string> SuggestedQuestions { get; set; } = new(); // Các câu hỏi mẫu để người dùng thử lại
}
