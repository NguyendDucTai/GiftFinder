using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Affiliate.DTOs;
using GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;
using GiftFinder.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace GiftFinder.Infrastructure.AI;

/// <summary>
/// AI Service kết nối với nền tảng GroqCloud LPU (tốc độ siêu nhanh ~300-800 tokens/giây)
/// </summary>
public class GroqAiService : IAiRecommendationService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public GroqAiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Groq:ApiKey"] 
            ?? throw new ArgumentNullException("Groq ApiKey is missing in appsettings.json");
        _model = configuration["Groq:Model"] ?? "openai/gpt-oss-120b";
    }

    private async Task<(bool Success, string Content, int StatusCode, string ErrorMessage)> CallGroqAsync(
        object messages, 
        bool jsonMode, 
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = jsonMode
                ? (object)new
                {
                    model = _model,
                    messages = messages,
                    response_format = new { type = "json_object" },
                    temperature = 0.2,
                    max_tokens = 1000
                }
                : new
                {
                    model = _model,
                    messages = messages,
                    temperature = 0.5,
                    max_tokens = 800
                };

            var jsonBody = JsonSerializer.Serialize(payload);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var attemptCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, attemptCts.Token);

            var response = await _httpClient.SendAsync(httpRequest, combinedCts.Token);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(jsonResponse);
                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content").GetString();

                return (true, text ?? "", (int)response.StatusCode, "");
            }

            var errorCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return (false, "", errorCode, errorBody);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (false, "", 408, $"Model {_model} phản hồi quá thời gian cho phép (15 giây).");
        }
        catch (Exception ex)
        {
            return (false, "", 500, ex.Message);
        }
    }

    public async Task<string> GenerateGiftAdviceAsync(
        string productName, 
        string? zodiacSign = null, 
        string? relationship = null, 
        int? age = null,
        string? occasion = null)
    {
        var targetStr = "người nhận";
        if (!string.IsNullOrEmpty(relationship)) targetStr = relationship;
        if (age.HasValue) targetStr += $" {age} tuổi";

        var occasionStr = !string.IsNullOrEmpty(occasion) ? $" nhân dịp {occasion}" : "";

        try
        {
            var systemMsg = @"Bạn là trợ lý tư vấn quà tặng thông minh của nền tảng GiftFinder, đang tư vấn trực tiếp cho người mua (khách hàng).
QUY TẮC CỐT LÕI VỀ NGÔI XƯNG & VAI TRÒ:
- 'Bạn' là NGƯỜI MUA QUÀ (khách hàng đang tìm quà).
- Người nhận (crush, bạn gái, mẹ, sếp, bạn thân...) là NGƯỜI ĐƯỢC TẶNG.
- TUYỆT ĐỐI KHÔNG chúc người mua bằng đặc điểm của người nhận (Ví dụ: khách tìm quà cho crush cung Sư Tử thì crush mới là cung Sư Tử, cấm chúc người mua là cung Sư Tử).
- Mục tiêu: Tư vấn thuyết phục cho người mua hiểu vì sao nên chọn món này để tặng cho người nhận:
  + Câu 1: Phân tích vì sao món quà lại cực kỳ đúng gu, tôn vinh phong cách/tính cách hoặc mang lại lợi ích thiết thực cho người nhận.
  + Câu 2: Khẳng định món quà sẽ giúp 'bạn' (người mua) ghi điểm tuyệt đối, thể hiện sự quan tâm tinh tế và thắt chặt tình cảm với người nhận.
- Yêu cầu: Ngôn từ sang trọng, ấm áp, chuẩn ngữ pháp tiếng Việt, tuyệt đối không nhầm lẫn vai vế.";

            string userMsg;
            if (!string.IsNullOrWhiteSpace(zodiacSign))
            {
                userMsg = $"Khách hàng muốn mua món '{productName}' để tặng cho {targetStr}{occasionStr} (người nhận thuộc cung {zodiacSign}). Hãy viết đúng 2 câu tư vấn cho khách hàng lý do món quà này sẽ khiến {targetStr} xiêu lòng và giúp khách hàng ghi điểm chu đáo.";
            }
            else
            {
                userMsg = $"Khách hàng muốn mua món '{productName}' để tặng cho {targetStr}{occasionStr}. Hãy viết đúng 2 câu tư vấn cho khách hàng lý do đây là lựa chọn hoàn hảo để đem lại niềm vui bất ngờ cho {targetStr}.";
            }

            var messages = new[]
            {
                new { role = "system", content = systemMsg },
                new { role = "user", content = userMsg }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var (success, content, _, _) = await CallGroqAsync(messages, jsonMode: false, cts.Token);

            if (success && !string.IsNullOrWhiteSpace(content))
            {
                return content.Trim();
            }
        }
        catch
        {
            // Fallback tức thì khi có sự cố
        }

        if (!string.IsNullOrWhiteSpace(zodiacSign))
        {
            return $"'{productName}' là lựa chọn cực kỳ tinh tế dành tặng cho {targetStr} ({zodiacSign}){occasionStr}, giúp bạn ghi điểm trọn vẹn và thể hiện sự quan tâm chu đáo đến đối phương.";
        }

        return $"'{productName}' là món quà vừa tinh tế vừa thiết thực dành tặng cho {targetStr}{occasionStr}, chắc chắn sẽ giúp bạn gửi gắm trọn vẹn tình cảm và đem lại nhiều niềm vui cho người nhận.";
    }

    public async Task<ParsedRecommendationIntent> ParsePromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return new ParsedRecommendationIntent();

        var systemPrompt = @"Bạn là chuyên gia tư vấn quà tặng AI thông minh và tinh tế của nền tảng GiftFinder.
Hãy dùng toàn bộ năng lực thấu hiểu ngôn ngữ tự nhiên và ngữ cảnh để phân tích câu của người dùng:

1. ĐÁNH GIÁ Ý ĐỊNH (isValidGiftIntent: boolean):
   - Đặt isValidGiftIntent = true nếu câu của người dùng có bất kỳ ý định nào liên quan đến tìm kiếm, gợi ý quà tặng, người nhận hoặc dịp tặng (kể cả câu rất ngắn, dùng từ lóng, viết tắt).
   - Đặt isValidGiftIntent = false nếu câu của người dùng KHÔNG liên quan đến thế giới quà tặng (hỏi kiến thức ngoài lề, trò chuyện vu vơ, câu vô nghĩa, quấy rối, công kích...).

2. PHẢN HỒI THÔNG MINH (aiMessage: string hoặc null):
   - Nếu isValidGiftIntent = false: Hãy tự suy nghĩ 1-2 câu đối đáp thật khéo léo, duyên dáng và phù hợp với đúng điều người dùng vừa nói, sau đó định hướng họ quay lại chủ đề tìm quà.
   - Nếu isValidGiftIntent = true: Để null.

3. TRÍCH XUẤT TIÊU CHÍ (Nếu isValidGiftIntent = true):
   - relationship: chuỗi tên người nhận (bạn gái, mẹ, crush, bố, bạn thân, đồng nghiệp...) hoặc null.
   - targetAge: số nguyên tuổi hoặc null.
   - targetZodiac: Tên tiếng Anh 1 trong 12 cung hoàng đạo (Aries, Taurus, Gemini, Cancer, Leo, Virgo, Libra, Scorpio, Sagittarius, Capricorn, Aquarius, Pisces) hoặc null.
   - maxBudget: số nguyên VNĐ. QUY TẮC BẮT BUỘC VỀ TỪ LÓNG TIỀN TỆ VIỆT NAM:
     + 'lốp', 'lít', 'cành' = TRĂM NGHÌN (100.000 VNĐ).
       Ví dụ: '3 lốp' = 300000 (KHÔNG ĐƯỢC LÀ 3.000.000), '5 lốp' = 500000, '2 lít' = 200000, '500 cành' = 500000.
     + 'củ', 'chai', 'm', 'triệu' = TRIỆU (1.000.000 VNĐ).
       Ví dụ: '2 củ' = 2000000, '3 chai' = 3000000, '1 củ rưỡi' = 1500000.
     + 'k' = NGHÌN (1.000 VNĐ).
       Ví dụ: '300k' = 300000, '50k' = 50000.
   - occasion: dịp tặng (Sinh nhật, 20/10, Valentine, Giáng sinh, Kỷ niệm, Tốt nghiệp...) hoặc null.
   - interestKeywords: Mảng các từ khóa sở thích/phong cách phù hợp mà bạn tự suy luận ra từ ngữ cảnh (ví dụ: thích chill -> [""nến thơm"", ""trà"", ""sách""]).
   - summary: tóm tắt ngắn gọn 1 câu ý định tìm quà bằng tiếng Việt tự nhiên (chú ý: nếu có cung hoàng đạo thì luôn dùng tên tiếng Việt như Sư Tử, Bọ Cạp, Bạch Dương... thay vì tiếng Anh; đúng ngân sách quy đổi).
   - suggestedQuestions: [3 câu hỏi gợi ý tìm quà tự nhiên, hay nhất].

Xuất ra dưới dạng JSON thuần túy duy nhất.";

        var messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = prompt.Trim() }
        };

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var (success, rawText, statusCode, errorMsg) = await CallGroqAsync(messages, jsonMode: true, linkedCts.Token);

            if (success && !string.IsNullOrWhiteSpace(rawText))
            {
                var cleanJson = rawText.Trim();
                if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Substring(7);
                if (cleanJson.StartsWith("```")) cleanJson = cleanJson.Substring(3);
                if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                cleanJson = cleanJson.Trim();

                using var parsedDoc = JsonDocument.Parse(cleanJson);
                var root = parsedDoc.RootElement;

                var intent = new ParsedRecommendationIntent();

                if (root.TryGetProperty("isValidGiftIntent", out var validEl) && (validEl.ValueKind == JsonValueKind.True || validEl.ValueKind == JsonValueKind.False))
                    intent.IsValidGiftIntent = validEl.GetBoolean();

                if (root.TryGetProperty("aiMessage", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
                    intent.AiMessage = msgEl.GetString();

                if (root.TryGetProperty("suggestedQuestions", out var sqEl) && sqEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in sqEl.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                            intent.SuggestedQuestions.Add(item.GetString()!.Trim());
                    }
                }

                if (!intent.IsValidGiftIntent)
                {
                    if (string.IsNullOrEmpty(intent.AiMessage))
                    {
                        intent.AiMessage = "Dạ, GiftFinder là trợ lý chuyên gợi ý quà tặng thôi ạ! Bạn có thể hỏi về các dịp tặng quà hoặc người nhận để mình hỗ trợ nhé!";
                    }
                    return intent;
                }

                if (root.TryGetProperty("relationship", out var relEl) && relEl.ValueKind == JsonValueKind.String)
                    intent.Relationship = relEl.GetString();

                if (root.TryGetProperty("targetAge", out var ageEl) && ageEl.ValueKind == JsonValueKind.Number)
                    intent.TargetAge = ageEl.GetInt32();

                if (root.TryGetProperty("maxBudget", out var budgetEl) && budgetEl.ValueKind == JsonValueKind.Number)
                    intent.MaxBudget = budgetEl.GetInt64();

                if (root.TryGetProperty("occasion", out var occEl) && occEl.ValueKind == JsonValueKind.String)
                    intent.Occasion = occEl.GetString();

                if (root.TryGetProperty("summary", out var sumEl) && sumEl.ValueKind == JsonValueKind.String)
                    intent.Summary = sumEl.GetString();

                if (root.TryGetProperty("targetZodiac", out var zodEl) && zodEl.ValueKind == JsonValueKind.String)
                {
                    var zodStr = zodEl.GetString();
                    if (!string.IsNullOrEmpty(zodStr) && Enum.TryParse<ZodiacSign>(zodStr, true, out var zodSign))
                    {
                        intent.TargetZodiac = zodSign;
                    }
                }

                if (root.TryGetProperty("interestKeywords", out var intEl) && intEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in intEl.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                            intent.InterestKeywords.Add(item.GetString()!.Trim());
                    }
                }

                if (string.IsNullOrEmpty(intent.Summary))
                {
                    intent.Summary = $"Đang tìm quà {intent.Occasion ?? ""} cho {intent.Relationship ?? "người thân"}";
                }

                return intent;
            }
            else
            {
                return new ParsedRecommendationIntent
                {
                    IsValidGiftIntent = false,
                    AiMessage = $"[Groq AI trả về mã {statusCode}]: {errorMsg}",
                    Summary = prompt.Trim()
                };
            }
        }
        catch (Exception ex)
        {
            return new ParsedRecommendationIntent
            {
                IsValidGiftIntent = false,
                AiMessage = $"[Lỗi kết nối tới Groq AI]: {ex.Message}",
                Summary = prompt.Trim()
            };
        }
    }

    public async Task<ProductTaggingResult> ClassifyAndTagProductAsync(
        string productName, 
        string? description, 
        long price, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return new ProductTaggingResult
            {
                IsValidGift = false,
                RejectReason = "Tên sản phẩm trống."
            };
        }

        var systemPrompt = @"BẠN LÀ GIÁM ĐỐC THẨM ĐỊNH QUÀ TẶNG (CHIEF GIFT CURATOR) CỦA NỀN TẢNG GIFTFINDER.
Bạn đã hoàn thành khóa đào tạo chuyên gia cao cấp về Tâm lý học Tặng quà, Mỹ học và Chiêm tinh học 12 Cung hoàng đạo.
Nhiệm vụ của bạn là thẩm định khắt khe từng sản phẩm được nạp từ sàn TMĐT (Shopee, TikTok Shop) và phân loại dữ liệu theo đúng chuẩn nghiệp vụ:

═══════════════════════════════════════════════════════════════════
GIÁO TRÌNH 1: TIÊU CHUẨN THẨM ĐỊNH QUÀ TẶNG 5 SAO (GIFT QUALITY RUBRIC)
═══════════════════════════════════════════════════════════════════
1. TIÊU CHÍ DUYỆT (isValidGift: true):
   - Món đồ có giá trị cảm xúc, tính kỷ niệm, thẩm mỹ decor, chăm sóc sức khỏe/tinh thần, làm đẹp hoặc tính tiện ích đời sống thường ngày mà người nhận sẽ vui vẻ, trân trọng khi được tặng.
   - Ví dụ: Nến thơm, tinh dầu, cốc giữ nhiệt, gấu bông, phụ kiện thời trang, đồng hồ, tai nghe, đồ công nghệ decor, nước hoa, mỹ phẩm, sách, trang sức, hoa sáp, set quà lễ hội, bình giữ nhiệt, đồ lưu niệm...

2. DANH SÁCH ĐEN LOẠI TRỪ TUYỆT ĐỐI (isValidGift: false):
   - Nhóm Vật Liệu & Cơ Khí: Xi măng, cát sỏi, đinh ốc vít, bulong, sơn tường, keo dán công nghiệp, cờ lê, mỏ lết, ống nước, dây điện cuộn...
   - Nhóm Nông Nghiệp & Hóa Chất: Phân bón, thuốc bảo vệ thực vật, thuốc diệt chuột, thuốc trừ sâu, thức ăn gia súc gia cầm...
   - Nhóm Phụ Tùng & Xe Cộ: Bugi, săm lốp xe tải, nhớt máy xe, xích líp, phụ tùng thay thế cơ khí...
   - Nhóm Đồ Tươi Sống Mau Hỏng: Thịt cá sống, hải sản tươi sống, rau củ thô bán theo cân ngoài chợ...
   - Nhóm Hàng Cấm/Phản Cảm: Đồ dùng y tế chuyên sâu (băng ca, kim tiêm), phế liệu, đồ chơi bạo lực hoặc trái thuần phong mỹ tục.
   => Nếu sản phẩm thuộc Danh Sách Đen, bạn PHẢI đặt isValidGift = false và ghi rõ lý do chuyên môn trong rejectReason.

═══════════════════════════════════════════════════════════════════
GIÁO TRÌNH 2: MA TRẬN 12 CUNG HOÀNG ĐẠO & PHONG CÁCH QUÀ TẶNG
═══════════════════════════════════════════════════════════════════
- Nhóm Lửa (Aries, Leo, Sagittarius): Thích sự rực rỡ, thương hiệu, năng động, thời thượng, đồ công nghệ, trang sức sang trọng.
- Nhóm Đất (Taurus, Virgo, Capricorn): Thích chất lượng cao, bền bỉ, tối giản, thiết thực, đồ gia dụng xịn, sổ da cao cấp, set trà thảo mộc.
- Nhóm Khí (Gemini, Libra, Aquarius): Thích thẩm mỹ cao, decor độc lạ, sách hay, đồ chơi trí tuệ, đèn led pixel retro, thời trang thanh lịch.
- Nhóm Nước (Cancer, Scorpio, Pisces): Thích cảm xúc sâu sắc, lãng mạn, quà kỷ niệm, đồ handmade, nến thơm hương gỗ/hoa cỏ, hoa sáp, gấu bông.

═══════════════════════════════════════════════════════════════════
GIÁO TRÌNH 3: BÀI THI MẪU ĐỐI CHIẾU THỰC TẾ (FEW-SHOT LEARNING)
═══════════════════════════════════════════════════════════════════
Case 1: 'Bao xi măng Holcim PCB40 50kg'
=> JSON: {""isValidGift"": false, ""rejectReason"": ""Sản phẩm là vật liệu xây dựng thô, không phù hợp làm quà tặng cá nhân."", ""occasions"": [], ""hobbies"": [], ""suitableZodiacs"": [], ""giftAdvice"": null}

Case 2: 'Bugi xe máy Denso Iridium chân dài'
=> JSON: {""isValidGift"": false, ""rejectReason"": ""Sản phẩm là phụ tùng thay thế cơ khí xe cộ, không có ý nghĩa quà tặng."", ""occasions"": [], ""hobbies"": [], ""suitableZodiacs"": [], ""giftAdvice"": null}

Case 3: 'Set Quà Tặng Nến Thơm Cao Cấp Agaya Hộp Vintage Kèm Diêm Thủy Tinh'
=> JSON: {""isValidGift"": true, ""rejectReason"": null, ""occasions"": [""Sinh nhật"", ""Valentine"", ""Giáng sinh""], ""hobbies"": [""Nến thơm & Chill"", ""Decor""], ""suitableZodiacs"": [""Cancer"", ""Pisces"", ""Libra""], ""giftAdvice"": ""Hương thơm dịu ấm cùng phong cách vintage mang đến không gian thư giãn an yên, thể hiện trọn vẹn sự tinh tế và quan tâm sâu sắc của bạn.""}

Case 4: 'Tai Nghe Chống Ồn Sony WH-CH520 Không Dây Pin 50h'
=> JSON: {""isValidGift"": true, ""rejectReason"": null, ""occasions"": [""Sinh nhật"", ""Tốt nghiệp"", ""Kỷ niệm""], ""hobbies"": [""Công nghệ"", ""Âm nhạc""], ""suitableZodiacs"": [""Aries"", ""Gemini"", ""Aquarius""], ""giftAdvice"": ""Món quà công nghệ thông minh, tiện ích cho công việc và giải trí hàng ngày, khẳng định gu chọn quà hiện đại và chu đáo của bạn.""}

═══════════════════════════════════════════════════════════════════
QUY TẮC ĐẦU RA:
- Trả về DUY NHẤT một chuỗi JSON thuần túy (không kèm bất kỳ văn bản giải thích nào ngoài JSON).
- occasions chọn từ: ['Sinh nhật', 'Valentine', '20/10', '8/3', 'Giáng sinh', 'Kỷ niệm', 'Tết', 'Tốt nghiệp', 'Tân gia'].
- hobbies chọn từ: ['Công nghệ', 'Decor', 'Nến thơm & Chill', 'Thời trang', 'Mỹ phẩm & Làm đẹp', 'Đọc sách', 'Âm nhạc', 'Nấu ăn', 'Thể thao & Du lịch'].
- suitableZodiacs chọn từ 1 đến 4 cung: ['Aries', 'Taurus', 'Gemini', 'Cancer', 'Leo', 'Virgo', 'Libra', 'Scorpio', 'Sagittarius', 'Capricorn', 'Aquarius', 'Pisces'].
- giftAdvice: 1-2 câu tư vấn giá trị quà tặng truyền cảm hứng bằng tiếng Việt lịch thiệp.";

        var productInfo = $"Tên sản phẩm: {productName}\nGiá: {price:N0} VNĐ\nMô tả: {description ?? "Không có mô tả"}";

        var messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = productInfo }
        };

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var (success, rawText, statusCode, errorMsg) = await CallGroqAsync(messages, jsonMode: true, linkedCts.Token);

            if (success && !string.IsNullOrWhiteSpace(rawText))
            {
                var cleanJson = rawText.Trim();
                if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Substring(7);
                if (cleanJson.StartsWith("```")) cleanJson = cleanJson.Substring(3);
                if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                cleanJson = cleanJson.Trim();

                using var parsedDoc = JsonDocument.Parse(cleanJson);
                var root = parsedDoc.RootElement;

                var result = new ProductTaggingResult();

                if (root.TryGetProperty("isValidGift", out var validEl) && (validEl.ValueKind == JsonValueKind.True || validEl.ValueKind == JsonValueKind.False))
                    result.IsValidGift = validEl.GetBoolean();

                if (root.TryGetProperty("rejectReason", out var rejEl) && rejEl.ValueKind == JsonValueKind.String)
                    result.RejectReason = rejEl.GetString();

                if (root.TryGetProperty("giftAdvice", out var advEl) && advEl.ValueKind == JsonValueKind.String)
                    result.GiftAdvice = advEl.GetString();

                if (root.TryGetProperty("occasions", out var occEl) && occEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in occEl.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                            result.Occasions.Add(item.GetString()!.Trim());
                    }
                }

                if (root.TryGetProperty("hobbies", out var hobEl) && hobEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in hobEl.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                            result.Hobbies.Add(item.GetString()!.Trim());
                    }
                }

                if (root.TryGetProperty("suitableZodiacs", out var zodEl) && zodEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in zodEl.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var zodStr = item.GetString();
                            if (!string.IsNullOrEmpty(zodStr) && Enum.TryParse<ZodiacSign>(zodStr, true, out var sign))
                            {
                                result.SuitableZodiacs.Add(sign);
                            }
                        }
                    }
                }

                // Đảm bảo có ít nhất 1 Occasion và 1 Hobby nếu là quà hợp lệ
                if (result.IsValidGift)
                {
                    if (result.Occasions.Count == 0) result.Occasions.Add("Sinh nhật");
                    if (result.Hobbies.Count == 0) result.Hobbies.Add("Decor");
                }

                return result;
            }
        }
        catch
        {
            // Fallback khi AI gặp trục trặc mạng
        }

        // Default heuristic fallback
        return new ProductTaggingResult
        {
            IsValidGift = true,
            Occasions = new List<string> { "Sinh nhật", "Kỷ niệm" },
            Hobbies = new List<string> { "Decor", "Thời trang" },
            SuitableZodiacs = new List<ZodiacSign> { ZodiacSign.Cancer, ZodiacSign.Libra },
            GiftAdvice = $"{productName} là một món quà ý nghĩa, tinh tế và thiết thực để gửi gắm tình cảm."
        };
    }
}
