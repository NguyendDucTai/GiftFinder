using System.Text;
using System.Text.Json;
using GiftFinder.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GiftFinder.Infrastructure.AI;

public class GeminiAiService : IAiRecommendationService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiAiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini ApiKey is missing");
    }

    public async Task<string> GenerateGiftAdviceAsync(string productName, string zodiacSign, string? relationship = null, int? age = null)
    {
        var targetStr = "khách hàng";
        if (!string.IsNullOrEmpty(relationship)) targetStr = relationship;
        if (age.HasValue) targetStr += $" {age} tuổi";

        var prompt = $"Bạn là chuyên gia tư vấn quà tặng. Khách hàng muốn tặng '{productName}' cho {targetStr} (cung {zodiacSign}). Hãy viết đúng 2 câu thật hay, giải thích lý do tại sao món đồ này lại cực kỳ hợp với tính cách của cung {zodiacSign} (và độ tuổi nếu có) để thuyết phục họ mua.";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var jsonBody = JsonSerializer.Serialize(requestBody);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={_apiKey}";
        
        HttpResponseMessage? response = null;
        int maxRetries = 3;
        
        for (int i = 0; i < maxRetries; i++)
        {
            // Cần tạo lại StringContent mỗi lần gửi vì HttpClient có thể dispose nó sau khi dùng
            var retryContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync(url, retryContent);
            
            if (response.IsSuccessStatusCode) break; // Thành công thì thoát vòng lặp
            
            // Nếu lỗi 503 (Overload) hoặc 429 (Too many requests), thì chờ một chút rồi thử lại
            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || (int)response.StatusCode == 429)
            {
                await Task.Delay(1500 * (i + 1)); // Chờ 1.5s, 3s...
                continue;
            }
            
            break; // Các lỗi khác (sai key, v.v.) thì dừng luôn
        }
        
        if (response == null || !response.IsSuccessStatusCode)
        {
            var errorMsg = await response.Content.ReadAsStringAsync();
            return $"[Lỗi AI - {response.StatusCode}]: {errorMsg}";
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);
        
        try
        {
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            return text?.Trim() ?? $"[Lỗi AI]: Google trả về dữ liệu rỗng. Dữ liệu gốc: {jsonResponse}";
        }
        catch (Exception ex)
        {
            return $"[Lỗi Phân Tích JSON]: Không đọc được kết quả từ Google. Chi tiết: {ex.Message}. Dữ liệu gốc: {jsonResponse}";
        }
    }
}
