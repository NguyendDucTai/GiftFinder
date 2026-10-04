namespace GiftFinder.Application.Common.Interfaces;

public interface IAiRecommendationService
{
    /// <summary>
    /// Tạo ra một đoạn văn ngắn (2-3 câu) giải thích tại sao món quà này lại hợp với người nhận dựa trên Cung Hoàng Đạo.
    /// </summary>
    /// <param name="productName">Tên món quà</param>
    /// <param name="zodiacSign">Cung Hoàng Đạo của người nhận</param>
    /// <param name="relationship">Mối quan hệ (tùy chọn)</param>
    /// <param name="age">Độ tuổi (tùy chọn)</param>
    /// <returns>Đoạn văn tư vấn từ AI</returns>
    Task<string> GenerateGiftAdviceAsync(string productName, string zodiacSign, string? relationship = null, int? age = null);
}
