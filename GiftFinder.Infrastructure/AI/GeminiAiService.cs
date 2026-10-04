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
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
        
        var response = await _httpClient.PostAsync(url, content);
        
        if (!response.IsSuccessStatusCode)
        {
            return "Món quà này rất tuyệt vời và phù hợp với tính cách của người nhận!"; // Fallback nếu AI lỗi
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

            return text?.Trim() ?? "Món quà này rất tuyệt vời!";
        }
        catch
        {
            return "Đây là một sự lựa chọn quà tặng hoàn hảo!";
        }
    }
}
