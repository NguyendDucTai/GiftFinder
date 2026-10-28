using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class RecommendationLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public User? User { get; private set; }

    // Dữ liệu tìm kiếm / Quiz gợi ý (UC-01)
    public string? SearchText { get; private set; }
    public string? Recipient { get; private set; }
    public string? Occasion { get; private set; }
    public long? BudgetMin { get; private set; }
    public long? BudgetMax { get; private set; }
    public string? Hobbies { get; private set; }
    public int ResultCount { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Đánh giá phản hồi từ người dùng (UC-04)
    public bool? IsHelpful { get; private set; }
    public string? FeedbackNote { get; private set; }
    public DateTime? FeedbackGivenAt { get; private set; }

    protected RecommendationLog() { }

    public RecommendationLog(
        string? searchText,
        string? recipient,
        string? occasion,
        long? budgetMin,
        long? budgetMax,
        string? hobbies,
        int resultCount,
        Guid? userId = null)
    {
        SearchText = searchText;
        Recipient = recipient;
        Occasion = occasion;
        BudgetMin = budgetMin;
        BudgetMax = budgetMax;
        Hobbies = hobbies;
        ResultCount = resultCount;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddFeedback(bool isHelpful, string? feedbackNote = null)
    {
        IsHelpful = isHelpful;
        FeedbackNote = feedbackNote?.Trim();
        FeedbackGivenAt = DateTime.UtcNow;
    }
}