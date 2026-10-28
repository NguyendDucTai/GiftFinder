namespace GiftFinder.Domain.Constants;

public static class SubscriptionPricing
{
    // BR-19: Mức phí gói Premium được cấu hình 15.000 VNĐ / tháng
    public const long MonthlyPrice = 15_000;
    public const long QuarterlyPrice = 45_000;
    public const long YearlyPrice = 150_000; // Tiết kiệm 30.000đ khi mua cả năm (tương đương 10 tháng tặng 2 tháng)

    public static long GetPrice(int months)
    {
        return months switch
        {
            1 => MonthlyPrice,
            3 => QuarterlyPrice,
            12 => YearlyPrice,
            _ => months > 0 ? months * MonthlyPrice : MonthlyPrice
        };
    }

    public static string GetPlanName(int months)
    {
        return months switch
        {
            1 => "Gói Premium 1 Tháng",
            3 => "Gói Premium 3 Tháng",
            12 => "Gói Premium 1 Năm (12 Tháng)",
            _ => $"Gói Premium {months} Tháng"
        };
    }
}
