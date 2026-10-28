using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class RecommendationRule : BaseEntity
{
    public string Version { get; private set; } = default!;
    public double BudgetWeight { get; private set; }
    public double InterestWeight { get; private set; }
    public double OccasionWeight { get; private set; }
    public double FeaturedWeight { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    protected RecommendationRule() { }

    public RecommendationRule(
        string version,
        double budgetWeight,
        double interestWeight,
        double occasionWeight,
        double featuredWeight)
    {
        // BR-32: Tổng các trọng số phải bằng 100%
        var total = budgetWeight + interestWeight + occasionWeight + featuredWeight;
        if (Math.Abs(total - 100.0) > 0.001)
            throw new InvalidOperationException("Tổng trọng số các tiêu chí phải bằng 100%, vui lòng điều chỉnh lại.");

        Version = version.Trim();
        BudgetWeight = budgetWeight;
        InterestWeight = interestWeight;
        OccasionWeight = occasionWeight;
        FeaturedWeight = featuredWeight;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}