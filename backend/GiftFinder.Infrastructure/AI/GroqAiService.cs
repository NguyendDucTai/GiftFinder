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
        CancellationToken cancellationToken,
        int maxTokens = 450)
    {
        var modelsToTry = new[] { _model, "openai/gpt-oss-20b", "qwen/qwen3.8-27b" }
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .ToArray();

        foreach (var modelName in modelsToTry)
        {
            try
            {
                var payload = jsonMode
                    ? (object)new
                    {
                        model = modelName,
                        messages = messages,
                        response_format = new { type = "json_object" },
                        reasoning_effort = "low",
                        temperature = 0.2,
                        max_tokens = maxTokens
                    }
                    : new
                    {
                        model = modelName,
                        messages = messages,
                        reasoning_effort = "low",
                        temperature = 0.5,
                        max_tokens = maxTokens
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
                Console.WriteLine($"[CallGroqAsync] Model {modelName} returned {errorCode}: {errorBody}. Trying fallback model...");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"[CallGroqAsync] Model {modelName} timed out (15s)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CallGroqAsync] Model {modelName} exception: {ex.Message}");
            }
        }

        return (false, "", 500, "All Groq model attempts exhausted or rate-limited.");
    }

    public async Task<string> GenerateGiftAdviceAsync(
        string productName, 
        string? zodiacSign = null, 
        string? relationship = null, 
        int? age = null,
        string? occasion = null,
        bool isRelaxedBudget = false,
        long? targetBudget = null)
    {
        var targetStr = "người nhận";
        if (!string.IsNullOrEmpty(relationship)) targetStr = relationship;
        if (age.HasValue) targetStr += $" {age} tuổi";

        var occasionStr = "";
        if (!string.IsNullOrWhiteSpace(occasion))
        {
            var cleanOcc = occasion.Trim();
            occasionStr = cleanOcc.StartsWith("dịp", StringComparison.OrdinalIgnoreCase) 
                ? $" nhân {cleanOcc}" 
                : $" nhân dịp {cleanOcc}";
        }

        try
        {
            var systemMsg = @"Bạn là trợ lý tư vấn quà tặng thông minh và tinh tế của nền tảng GiftFinder, đang tư vấn trực tiếp cho người mua (khách hàng).
QUY TẮC CỐT LÕI VỀ NGÔI XƯNG & VAI TRÒ:
1. 'Bạn' là NGƯỜI MUA QUÀ (khách hàng đang tìm quà).
2. Người nhận (bố, mẹ, bạn gái, bạn trai, sếp, đồng nghiệp...) là NGƯỜI ĐƯỢC TẶNG.
3. QUY TẮC XƯNG HÔ VỚI NGƯỜI NHẬN (NGHIÊM CẤM SAI LỆCH VAI VẾ):
   - BẬC PHỤ HUYNH / NGƯỜI LỚN TUỔI (Bố, Mẹ, Ba, Má, Ông, Bà, Bác, Chú, Cô...):
     + BẮT BUỘC dùng danh xưng kính cẩn: 'bố', 'mẹ', 'ông', 'bà', hoặc đại từ tôn trọng 'ông', 'bà', 'bác ấy'.
     + TUYỆT ĐỐI CẤM gọi bố/mẹ/ông/bà là 'anh ấy', 'cô ấy', 'anh', 'em'!
     + TUYỆT ĐỐI CẤM dùng từ tiếng Anh ('father', 'mother', 'dad', 'mom').
   - BẠN BÈ / NGƯỜI YÊU / ĐỒNG NGHIỆP:
     + Dùng 'bạn gái', 'bạn trai', 'người ấy', 'anh ấy' (cho bạn trai/chồng), 'cô ấy' (cho bạn gái/vợ).
4. QUY TẮC NỘI DUNG:
   - TUYỆT ĐỐI KHÔNG tư vấn sản phẩm nữ tính (váy đầm, son môi, mỹ phẩm nữ...) cho nam giới và ngược lại.
   - TUYỆT ĐỐI KHÔNG chúc người mua bằng đặc điểm của người nhận (Ví dụ: khách tìm quà cho crush cung Sư Tử thì crush mới là cung Sư Tử, cấm chúc người mua là cung Sư Tử).
   - XỬ LÝ TỪ KHÓA MÙA VỤ / MARKETING TRONG TIÊU ĐỀ:
     Nhiều sản phẩm trên sàn TMĐT người bán gắn các từ khóa mùa vụ/dịp lễ khác vào tiêu đề để tối ưu tìm kiếm SEO (như '20/11', '8/3', 'Valentine', 'Noel'...).
     Khi khách hàng đang tìm quà cho một dịp khác hoặc đối tượng khác (ví dụ: đang tặng quà Tết cho bố), TUYỆT ĐỐI KHÔNG lặp lại các từ khóa mùa vụ chệch nhịp đó (như '20/11') trong lời tư vấn! Hãy tập trung hoàn toàn vào CÔNG NĂNG & GIÁ TRỊ THỰC TẾ của sản phẩm (ví dụ: các loại thảo dược tự nhiên bồi bổ sức khỏe, hộp trà ấm áp ngày xuân...) dành cho đúng {targetStr}{occasionStr}.
   - Viết đúng 2 câu ngắn gọn, truyền cảm hứng:
     + Câu 1: Phân tích vì sao món quà cực kỳ phù hợp, mang lại giá trị thiết thực hoặc thể hiện sự thấu hiểu phong cách của người nhận.
     + Câu 2: Khẳng định món quà sẽ giúp 'bạn' ghi điểm tinh tế và gắn kết tình cảm với người nhận.
   - Ngôn từ sang trọng, ấm áp, chuẩn ngữ pháp tiếng Việt.";

            if (isRelaxedBudget)
            {
                systemMsg += @"
5. GHI CHÚ VỀ NGÂN SÁCH NỚI LỎNG:
   Món quà này có mức giá nhỉnh hơn hoặc thấp hơn một chút so với khoảng ngân sách dự kiến ban đầu (do kho quà ở tầm giá chính xác hiện chưa có món thật sự xuất sắc). Hãy mở đầu câu 1 thật tinh tế và tự nhiên (Ví dụ: 'Dù mức giá nhỉnh hơn đôi chút so với dự kiến ban đầu, nhưng món...' hoặc 'Tuy có giá mềm hơn dự tính, nhưng...').";
            }

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
            var (success, content, _, _) = await CallGroqAsync(messages, jsonMode: false, cts.Token, maxTokens: 250);

            if (success && !string.IsNullOrWhiteSpace(content))
            {
                var cleanAdvice = content.Trim();
                var tLower = targetStr.ToLowerInvariant();
                if (tLower.Contains("bố") || tLower.Contains("ba") || tLower.Contains("cha") || tLower.Contains("ông") || tLower.Contains("bác"))
                {
                    cleanAdvice = System.Text.RegularExpressions.Regex.Replace(cleanAdvice, @"\b(anh ấy|anh)\b", "bố", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                else if (tLower.Contains("mẹ") || tLower.Contains("má") || tLower.Contains("bà"))
                {
                    cleanAdvice = System.Text.RegularExpressions.Regex.Replace(cleanAdvice, @"\b(cô ấy|chị ấy|chị)\b", "mẹ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                return cleanAdvice;
            }
        }
        catch
        {
            // Fallback tức thì khi có sự cố
        }

        // Cắt gọn tên sản phẩm cho tự nhiên nếu quá dài
        var shortName = productName;
        if (shortName.Length > 45)
        {
            var commaIdx = shortName.IndexOfAny(new[] { ',', '-', '|', '–' });
            if (commaIdx > 12) shortName = shortName.Substring(0, commaIdx).Trim();
        }

        if (!string.IsNullOrWhiteSpace(zodiacSign))
        {
            return $"{shortName} là lựa chọn cực kỳ tinh tế dành tặng cho {targetStr} ({zodiacSign}){occasionStr}, vừa mang ý nghĩa may mắn vừa thể hiện sự quan tâm chu đáo của bạn.";
        }

        return $"{shortName} là món quà tinh tế và thiết thực dành tặng cho {targetStr}{occasionStr}, chắc chắn sẽ giúp bạn gửi gắm trọn vẹn sự chu đáo và gắn kết tình cảm gia đình.";
    }

    public async Task<AiRerankResultDto> RerankAndAdviseAsync(
        List<CandidateProductItemDto> candidates,
        string targetRecipient,
        string? occasion = null,
        string? targetGender = null,
        long? targetBudget = null,
        string? zodiacSign = null,
        string? interestOrTheme = null,
        CancellationToken cancellationToken = default)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return new AiRerankResultDto
            {
                SelectedProductId = null,
                Advice = null,
                IsRelaxedBudget = false,
                SelectionReason = "Danh sách ứng viên rỗng."
            };
        }

        var targetStr = string.IsNullOrWhiteSpace(targetRecipient) ? "người nhận" : targetRecipient.Trim();
        var occasionStr = !string.IsNullOrWhiteSpace(occasion) ? occasion.Trim() : "dịp đặc biệt";
        var genderStr = !string.IsNullOrWhiteSpace(targetGender) ? targetGender.Trim() : "Không chỉ định";
        var budgetStr = targetBudget.HasValue ? $"{targetBudget.Value:N0} VNĐ" : "Không giới hạn";
        var zodiacStr = !string.IsNullOrWhiteSpace(zodiacSign) ? zodiacSign.Trim() : "Không có";
        var themeStr = !string.IsNullOrWhiteSpace(interestOrTheme) ? interestOrTheme.Trim() : null;

        // Xây dựng danh sách ứng viên ngắn gọn (tối ưu hóa token: ~20 tokens / sản phẩm)
        var sb = new StringBuilder();
        foreach (var c in candidates)
        {
            sb.AppendLine($"- ID: {c.Id} | Tên: {c.Name} | Giá: {c.Price:N0} đ | Tầng giá: Tier {c.PriceTier} ({(c.PriceTier == 1 ? "Chuẩn ngân sách" : "Nới lỏng")}) | Ngành: {c.Category ?? "Chung"} | Đánh giá: {c.Rating:0.0} sao ({c.TotalReviews} đánh giá)");
        }

        var systemPrompt = @"Bạn là Giám Khảo Thẩm Định & Chuyên Viên Tư Vấn Quà Tặng Cấp Cao của hệ thống GiftFinder.
Nhiệm vụ của bạn: Thẩm định mẻ sản phẩm ứng viên từ cơ sở dữ liệu, loại bỏ các món sai lệch giới tính/đối tượng, chọn ra 1 MÓN XUẤT SẮC NHẤT, và viết lời tư vấn tinh tế gồm 2 câu gửi cho khách hàng (người mua).

QUY TẮC THẨM ĐỊNH & LOẠI BỎ (BẮT BUỘC):
1. GIỚI TÍNH & ĐỐI TƯỢNG (TIÊU CHÍ TIÊN QUYẾT):
   - Khi tìm quà cho NAM (targetGender = 'Male' hoặc đối tượng là bạn trai, bố, em trai, anh trai, đồng nghiệp nam...):
     + BẮT BUỘC LOẠI BỎ TRIỆT ĐỂ: Các món đồ thuần nữ tính như túi kẹp nách, túi đeo vai nữ, clutch, váy đầm, đồ trang điểm (son môi, kem nền, phấn), phụ kiện tóc (kẹp tóc, cột tóc), đồ handmade bánh bèo (kẽm nhung, hoa kẽm, hoa sáp, thiệp trái tim, loopy...).
     + TUYỆT ĐỐI KHÔNG chọn các món này cho nam giới trong bất kỳ trường hợp nào!
   - Khi tìm quà cho NỮ (targetGender = 'Female' hoặc đối tượng là bạn gái, mẹ, chị gái, em gái...):
     + BẮT BUỘC LOẠI BỎ TRIỆT ĐỂ: Dao cạo râu nam, tông đơ nam, thắt lưng nam, ví nam kiểu dáng thô nam tính, sáp vuốt tóc nam...
   - Khi là Đồng nghiệp / Bạn bè: Ưu tiên các món tiện ích văn phòng, thanh lịch, hữu dụng (bình giữ nhiệt, ly sứ, chuột máy tính, tai nghe, trà thảo mộc, sổ tay...).

2. XỬ LÝ TỪ KHÓA SEO MÙA VỤ CỦA SÀN TMĐT (SEO NOISE FILTERING):
   - Người bán trên sàn thường nhồi từ khóa mùa vụ vào tiêu đề để tối ưu SEO (ví dụ: 'Bình giữ nhiệt Lock&Lock quà tặng 20/11 tri ân thầy cô sinh nhật đồng nghiệp').
   - HÃY ĐÁNH GIÁ BẢN CHẤT SẢN PHẨM: Chiếc bình giữ nhiệt đó VẪN LÀ MÓN QUÀ XUẤT SẮC cho đồng nghiệp nam!
   - TRONG LỜI TƯ VẤN: TUYỆT ĐỐI KHÔNG nhắc lại từ khóa mùa vụ chệch nhịp (như '20/11', 'tri ân thầy cô' khi khách tìm quà 'Sinh nhật'). Chỉ tập trung vào công năng, chất liệu, tính tiện dụng và sự tinh tế của món quà.

3. THỨ TỰ ƯU TIÊN CHỌN SẢN PHẨM (THEME-FIRST & TIER AWARE):
   - KHI CÓ CHỦ ĐỀ / NHU CẦU ĐẶC THÙ (vd: Công nghệ, Sức khỏe, Trà, Gaming, Làm đẹp...):
     + TIÊU CHÍ CHỦ ĐỀ LÀ ƯU TIÊN SỐ 1: BẮT BUỘC CHỌN SẢN PHẨM ĐÚNG CHỦ ĐỀ / CÔNG NĂNG ĐÓ!
     + Kể cả sản phẩm đúng chủ đề thuộc Tầng giá 2 (PriceTier: 2 - nhỉnh hơn hoặc mềm hơn dự tính một chút), VẪN PHẢI ƯU TIÊN CHỌN SẢN PHẨM ĐÚNG CHỦ ĐỀ ĐÓ!
     + TUYỆT ĐỐI CẤM chọn sản phẩm lạc đề (ví dụ: nước hoa, sữa tắm, dây chuyền, vòng cổ khi khách hỏi công nghệ) chỉ vì nó thuộc Tầng 1!
   - KHI KHÔNG CÓ CHỦ ĐỀ ĐẶC THÙ (tìm quà chung chung):
     + Ưu tiên số 1: Sản phẩm thuộc Tầng giá 1 (PriceTier: 1 - khớp chuẩn ngân sách mục tiêu).
     + Chỉ chọn sang Tầng giá 2 khi các món Tầng 1 không còn món nào thật sự phù hợp về giới tính.
   - Nếu TẤT CẢ các sản phẩm trong danh sách đều sai lệch giới tính hoặc không phù hợp, trả về selectedProductId là null.

4. QUY TẮC VIẾT LỜI TƯ VẤN (ADVICE):
   - Ngôi xưng: 'Bạn' là NGƯỜI MUA. Người nhận là {targetRecipient}.
   - Kính cẩn nếu người nhận là Bố, Mẹ, Ông, Bà (tuyệt đối không gọi bố mẹ là 'anh/em/anh ấy').
   - Đúng 2 câu văn súc tích, sang trọng, giàu cảm xúc:
     + Câu 1: Phân tích lý do vì sao món quà thiết thực, chuẩn gu người nhận hoặc ý nghĩa cho dịp này.
       * Nếu chọn món ở Tầng 2 (nới lỏng), hãy mở đầu thật khéo léo (Ví dụ: 'Dù mức giá nhỉnh hơn đôi chút so với ngân sách dự kiến, nhưng...' hoặc 'Tuy có mức giá mềm hơn dự tính, nhưng...').
     + Câu 2: Khẳng định món quà sẽ giúp bạn thể hiện sự chu đáo và gắn kết tình cảm.

5. ĐỊNH DẠNG JSON TRẢ VỀ (BẮT BUỘC):
{
  ""selectedProductId"": ""GUID của sản phẩm được chọn (hoặc null nếu không có món nào hợp)"",
  ""advice"": ""Lời tư vấn đúng 2 câu (hoặc null nếu không chọn)"",
  ""isRelaxedBudget"": true/false (true nếu sản phẩm được chọn có PriceTier == 2),
  ""selectionReason"": ""Giải thích ngắn gọn 1 câu lý do chọn hoặc lý do loại bỏ các ứng viên khác""
}";

        var userPrompt = $@"THÔNG TIN TÌM KIẾM:
- Đối tượng nhận quà: {targetStr}
- Giới tính đối tượng: {genderStr}
- Dịp tặng: {occasionStr}
- Ngân sách mục tiêu: {budgetStr}
- Cung hoàng đạo: {zodiacStr}
{(themeStr != null ? $"- Chủ đề / Nhu cầu đặc thù: {themeStr}\n* QUAN TRỌNG: Khách đang tìm kiếm quà thuộc chủ đề '{themeStr}', BẮT BUỘC ưu tiên chọn sản phẩm đúng chủ đề/công năng này, LOẠI BỎ các sản phẩm lạc đề!" : "")}

DANH SÁCH ỨNG VIÊN CẦN THẨM ĐỊNH:
{sb}

Hãy thẩm định kỹ lưỡng, loại bỏ các món sai giới tính hoặc sai chủ đề, chọn ra 1 sản phẩm xuất sắc nhất và trả về JSON object.";

        var messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userPrompt }
        };

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var (success, content, _, _) = await CallGroqAsync(messages, jsonMode: true, combinedCts.Token, maxTokens: 800);

            if (success && !string.IsNullOrWhiteSpace(content))
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                Guid? selectedId = null;
                if (root.TryGetProperty("selectedProductId", out var idEl))
                {
                    if (idEl.ValueKind == JsonValueKind.String)
                    {
                        var idStr = idEl.GetString();
                        if (Guid.TryParse(idStr, out var parsedGuid))
                        {
                            selectedId = parsedGuid;
                        }
                    }
                }

                string? advice = null;
                if (root.TryGetProperty("advice", out var advEl) && advEl.ValueKind == JsonValueKind.String)
                {
                    advice = advEl.GetString()?.Trim();
                }

                bool isRelaxed = false;
                if (root.TryGetProperty("isRelaxedBudget", out var relEl) && (relEl.ValueKind == JsonValueKind.True || relEl.ValueKind == JsonValueKind.False))
                {
                    isRelaxed = relEl.GetBoolean();
                }

                string? reason = null;
                if (root.TryGetProperty("selectionReason", out var reasEl) && reasEl.ValueKind == JsonValueKind.String)
                {
                    reason = reasEl.GetString()?.Trim();
                }

                // Kiểm tra tính hợp lệ: ID được chọn phải thuộc danh sách ứng viên
                if (selectedId.HasValue)
                {
                    var matchedCandidate = candidates.FirstOrDefault(c => c.Id == selectedId.Value);
                    if (matchedCandidate != null)
                    {
                        isRelaxed = matchedCandidate.PriceTier == 2;

                        // Làm sạch danh xưng nếu người nhận là cha/mẹ/ông/bà
                        if (!string.IsNullOrWhiteSpace(advice))
                        {
                            var tLower = targetStr.ToLowerInvariant();
                            if (tLower.Contains("bố") || tLower.Contains("ba") || tLower.Contains("cha") || tLower.Contains("ông") || tLower.Contains("bác"))
                            {
                                advice = System.Text.RegularExpressions.Regex.Replace(advice, @"\b(anh ấy|anh)\b", "bố", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            }
                            else if (tLower.Contains("mẹ") || tLower.Contains("má") || tLower.Contains("bà"))
                            {
                                advice = System.Text.RegularExpressions.Regex.Replace(advice, @"\b(cô ấy|chị ấy|chị)\b", "mẹ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            }
                        }

                        return new AiRerankResultDto
                        {
                            SelectedProductId = selectedId,
                            Advice = advice,
                            IsRelaxedBudget = isRelaxed,
                            SelectionReason = reason ?? "Được AI thẩm định và chọn là sản phẩm phù hợp nhất."
                        };
                    }
                }
                else if (root.TryGetProperty("selectedProductId", out var nullIdEl) && nullIdEl.ValueKind == JsonValueKind.Null)
                {
                    // AI chủ động xác định tất cả các món trong mẻ đều không hợp
                    return new AiRerankResultDto
                    {
                        SelectedProductId = null,
                        Advice = null,
                        IsRelaxedBudget = false,
                        SelectionReason = reason ?? "Tất cả các sản phẩm ứng viên đều không phù hợp với tiêu chí giới tính hoặc đối tượng."
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RerankAndAdviseAsync] Exception: {ex.Message}");
        }

        // Heuristic fallback nếu AI gặp sự cố
        // Ưu tiên theo thứ tự candidates (đã được sắp xếp khớp theme và điểm cao nhất lên đầu)
        var fallbackCandidate = candidates.FirstOrDefault();
        if (fallbackCandidate != null)
        {
            var cleanName = fallbackCandidate.Name;
            if (cleanName.Length > 55)
            {
                var splitIdx = cleanName.IndexOfAny(new[] { ',', '-', '|', '–', '/', '•' });
                if (splitIdx > 15)
                {
                    cleanName = cleanName.Substring(0, splitIdx).Trim();
                }
                else
                {
                    var lastSpace = cleanName.LastIndexOf(' ', Math.Min(55, cleanName.Length - 1));
                    if (lastSpace > 15) cleanName = cleanName.Substring(0, lastSpace).Trim();
                }
            }

            var occasionSuffix = string.IsNullOrWhiteSpace(occasion)
                ? "nhân dịp đặc biệt"
                : (occasion.Trim().StartsWith("dịp", StringComparison.OrdinalIgnoreCase)
                    ? $"nhân {occasion.Trim()}"
                    : $"nhân dịp {occasion.Trim()}");

            return new AiRerankResultDto
            {
                SelectedProductId = fallbackCandidate.Id,
                Advice = $"{cleanName} là món quà tinh tế, thiết thực rất thích hợp dành tặng cho {targetStr} {occasionSuffix}.",
                IsRelaxedBudget = fallbackCandidate.PriceTier == 2,
                SelectionReason = "Fallback theo độ phù hợp cơ bản."
            };
        }

        return new AiRerankResultDto
        {
            SelectedProductId = null,
            Advice = null,
            IsRelaxedBudget = false,
            SelectionReason = "Không tìm thấy sản phẩm phù hợp."
        };
    }

    public async Task<ParsedRecommendationIntent> ParsePromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return new ParsedRecommendationIntent();

        var pTrim = prompt.Trim();
        var pLower = pTrim.ToLowerInvariant();

        // 1. Chặn ngay nếu là phép tính toán học hoặc công thức số học thuần tuý (0ms, 0 token)
        // LƯU Ý: Tuyệt đối không chặn các ngày lễ dạng ngày/tháng như 20/10, 8/3, 20/11, 14/2, 1/6, 24/12 hoặc câu có ngữ cảnh quà tặng!
        var hasGiftContextInPrompt = System.Text.RegularExpressions.Regex.IsMatch(pLower,
            @"(quà|tặng|mua|gợi\s*ý|sinh\s*nhật|valentine|giáng\s*sinh|noel|tết|kỷ\s*niệm|tân\s*gia|tốt\s*nghiệp|20/10|8/3|20/11|14/2|1/6|24/12|bạn\s*gái|bạn\s*trai|nam|nữ|nàng|chàng|bố|ba|mẹ|má|sếp|đồng\s*nghiệp|vợ|chồng|người\s*yêu|crush|bé|con|thầy|cô|ngân\s*sách|giá|tiền|k\b|cành|lít|củ|triệu|đ\b)");

        var isDateOnlyPrompt = System.Text.RegularExpressions.Regex.IsMatch(pTrim,
            @"^(0?[1-9]|[12]\d|3[01])\s*/\s*(0?[1-9]|1[0-2])(\s*/\s*\d{2,4})?$");

        var isMathOrCalculationPrompt = !hasGiftContextInPrompt && !isDateOnlyPrompt && (
            (System.Text.RegularExpressions.Regex.IsMatch(pTrim, @"^[\d\s\+\-\*/\=\(\)\.\,\^\%\?]+$") &&
             System.Text.RegularExpressions.Regex.IsMatch(pTrim, @"[\+\*]|\d+\s*[\-\/]\s*\d+")) ||
            System.Text.RegularExpressions.Regex.IsMatch(pLower, @"(giải\s+toán|làm\s+toán|phép\s+tính|bài\s+toán)"));

        if (isMathOrCalculationPrompt)
        {
            return new ParsedRecommendationIntent
            {
                IsValidGiftIntent = false,
                AiMessage = "Dạ GiftFinder là trợ lý AI chuyên gợi ý và tư vấn quà tặng thôi ạ! 🎁 Mình không có tính năng giải toán hay phép tính. Bạn hãy thử hỏi gợi ý quà theo người nhận, dịp tặng hoặc ngân sách nhé!",
                Summary = "Phép tính toán học (ngoài lề)"
            };
        }

        // 2. Chặn ngay các câu quá chung chung / chỉ có 1-2 từ như "quà", "tìm quà", "tư vấn" mà thiếu mọi thông tin cụ thể (0ms, 0 token)
        var genericGiftRegex = new System.Text.RegularExpressions.Regex(
            @"^(tôi\s*muốn\s*|mình\s*muốn\s*|em\s*muốn\s*|cần\s*|hãy\s*|giúp\s*mình\s*|giúp\s*em\s*|cho\s*mình\s*|cho\s*em\s*)?(quà|quà\s*tặng|món\s*quà|tìm\s*quà|tìm\s*quà\s*tặng|tìm\s*món\s*quà|mua\s*quà|mua\s*quà\s*tặng|mua\s*món\s*quà|chọn\s*quà|chọn\s*quà\s*tặng|chọn\s*món\s*quà|tư\s*vấn|tư\s*vấn\s*quà|tư\s*vấn\s*quà\s*tặng|tư\s*vấn\s*chọn\s*quà|tư\s*vấn\s*giúp|gợi\s*ý|gợi\s*ý\s*quà|gợi\s*ý\s*quà\s*tặng|gợi\s*ý\s*giúp|cần\s*tìm\s*quà|cần\s*mua\s*quà|có\s*quà\s*gì|có\s*món\s*gì|mua\s*gì|tặng\s*gì|quà\s*gì|chọn\s*gì|mua\s*quà\s*gì|tặng\s*quà\s*gì)(\s*(ạ|nhé|nha|với|giúp|giúp\s*mình|giúp\s*em|giùm|đi|nào|với\s*ạ))*[.?!,\s]*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (genericGiftRegex.IsMatch(pTrim))
        {
            return new ParsedRecommendationIntent
            {
                IsValidGiftIntent = false,
                AiMessage = "Dạ bạn muốn tìm quà cho ai (ví dụ: bạn gái, bạn trai, bố mẹ, đồng nghiệp...), vào dịp gì (sinh nhật, kỷ niệm, 20/10...) và ngân sách khoảng bao nhiêu để mình gợi ý món quà chuẩn gu nhất nhé! 🎁✨",
                Summary = "Yêu cầu cung cấp thêm thông tin người nhận / dịp tặng / ngân sách",
                SuggestedQuestions = new List<string>
                {
                    "Quà sinh nhật cho bạn gái tầm 500k",
                    "Quà tặng mẹ dịp 20/10 tinh tế, ý nghĩa",
                    "Quà cho đồng nghiệp nam ngân sách 300k"
                }
            };
        }

        // 3. Nhận diện các câu yêu cầu đổi món quà / gợi ý quà khác (tiết kiệm token & tránh AI hiểu nhầm sang 'món ăn')
        var askAnotherRegex = new System.Text.RegularExpressions.Regex(
            @"^(khác|đổi|đổi\s*đi|món\s*khác|món\s*quà\s*khác|sản\s*phẩm\s*khác|sp\s*khác|quà\s*khác|mẫu\s*khác|set\s*khác|cái\s*khác|đổi\s*món|đổi\s*quà|đổi\s*sản\s*phẩm|gợi\s*ý\s*khác|gợi\s*ý\s*món\s*khác|gợi\s*ý\s*sản\s*phẩm\s*khác|đề\s*xuất\s*khác|đề\s*xuất\s*món\s*khác|đề\s*xuất\s*món\s*quà\s*khác|đề\s*xuất\s*sản\s*phẩm\s*khác|đề\s*xuất\s*cái\s*khác|món\s*nào\s*khác|sản\s*phẩm\s*nào\s*khác|xem\s*thêm\s*món|xem\s*sản\s*phẩm\s*khác|thay\s*món|thay\s*sản\s*phẩm|có\s*món\s*nào\s*khác|có\s*sản\s*phẩm\s*nào\s*khác|khác\s*đi|món\s*mới|sản\s*phẩm\s*mới|cho\s*món\s*khác|cho\s*sản\s*phẩm\s*khác|xem\s*món\s*khác|còn\s*món\s*nào\s*khác|còn\s*sản\s*phẩm\s*nào|còn\s*món\s*nào|còn\s*gì\s*nữa|chưa\s*ưng|không\s*thích\s*món\s*này|không\s*hợp)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (askAnotherRegex.IsMatch(pTrim))
        {
            return new ParsedRecommendationIntent
            {
                IsValidGiftIntent = true,
                Summary = "Xem gợi ý món quà khác",
                InterestKeywords = new List<string> { "quà khác", "lựa chọn khác" },
                SuggestedQuestions = new List<string>
                {
                    "Bạn muốn quà có tính thiết thực, tiện dụng?",
                    "Bạn muốn quà mang phong cách lịch lãm, sang trọng?",
                    "Bạn muốn quà có hộp đóng gói kèm thiệp chúc mừng?"
                }
            };
        }

        // 4. Nhận diện các câu hỏi thống kê số lượng kho hàng / quy mô sản phẩm (0ms, 0 Groq token)
        var inventoryStatsRegex = new System.Text.RegularExpressions.Regex(
            @"(có\s*bao\s*nhiêu|bao\s*nhiêu\s*(món|sản\s*phẩm|quà|set)|số\s*lượng\s*(món|sản\s*phẩm|quà)|quy\s*mô\s*(kho|cửa\s*hàng)|kho\s*(hàng\s*)?có\s*nhiều\s*món\s*không|trong\s*kho\s*có\s*(những\s*)?gì|kho\s*có\s*bao\s*nhiêu)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (inventoryStatsRegex.IsMatch(pTrim))
        {
            return new ParsedRecommendationIntent
            {
                IntentType = ChatIntentType.InventoryInquiry,
                IsValidGiftIntent = true,
                ShouldIncludeProduct = false,
                DirectAnswer = "Hiện tại GiftFinder có hơn 5,000 món quà đa dạng, từ những set quà tinh tế cho đến những món quà thiết thực. Bạn có thể cho mình biết bạn muốn tìm quà cho ai, vào dịp nào và ngân sách khoảng bao nhiêu để mình gợi ý món quà phù hợp nhất nhé! 🎁",
                Summary = "Câu hỏi về số lượng món quà trong kho",
                SuggestedQuestions = new List<string>
                {
                    "Quà sinh nhật cho bạn gái tầm 500k",
                    "Quà tặng mẹ dịp 20/10 tinh tế, ý nghĩa",
                    "Quà cho đồng nghiệp nam ngân sách 300k"
                }
            };
        }

        var systemPrompt = @"Bạn là chuyên gia tư vấn quà tặng AI thông minh và tinh tế của nền tảng GiftFinder.
Hãy dùng toàn bộ năng lực thấu hiểu ngôn ngữ tự nhiên và ngữ cảnh để phân tích câu của người dùng:

1. PHÂN LOẠI Ý ĐỊNH NGƯỜI DÙNG (intentType: string):
   Chọn 1 trong các giá trị sau:
   - ""Recommendation"": Người dùng tìm quà hoặc đổi món thông thường (vd: 'quà sinh nhật bạn gái 500k', 'đổi món khác', 'tìm món dưới 300k').
   - ""InventoryInquiry"": Người dùng thắc mắc về kho hàng, số lượng, hoặc câu hỏi tu từ / nghi vấn phủ định về giá hoặc sản phẩm.
     * TÌNH HUỐNG 1 - THỐNG KÊ KHO HÀNG / QUY MÔ SẢN PHẨM (Ví dụ: 'Trong kho có bao nhiêu món quà?', 'Kho có bao nhiêu sản phẩm?', 'Số lượng quà hiện có?'):
       + intentType = ""InventoryInquiry""
       + shouldIncludeProduct = false (TUYỆT ĐỐI KHÔNG KÈM SẢN PHẨM)
       + directAnswer = ""Hiện tại GiftFinder có hơn 5,000 món quà đa dạng, từ những set quà tinh tế cho đến những món quà thiết thực. Bạn có thể cho mình biết bạn muốn tìm quà cho ai, vào dịp nào và ngân sách khoảng bao nhiêu để mình gợi ý món quà phù hợp nhất nhé! 🎁""
       + summary = ""Câu hỏi về số lượng món quà trong kho""
     * TÌNH HUỐNG 2 - CÂU HỎI TU TỪ / NGHI VẤN PHỦ ĐỊNH VỀ GIÁ HOẶC TÌM SẢN PHẨM (Ví dụ: 'Không có món nào trên 500k à?', 'Sao không thấy món trên 500k?', 'Không có quà đắt hơn à?'):
       Đây là câu hỏi người dùng MUỐN TÌM MÓN TRÊN 500K -> BẮT BUỘC ĐẶT:
       + intentType = ""InventoryInquiry""
       + budgetType = ""Above"", minBudget = 500000, maxBudget = null
       + directAnswer = ""Dạ bên mình có các set quà cao cấp trên 500k chứ ạ! Dưới đây là các gợi ý nổi bật và sang trọng nhất để bạn tham khảo nhé:""
       + shouldIncludeProduct = true (BẮT BUỘC KÈM SẢN PHẨM)
       TUYỆT ĐỐI KHÔNG ĐƯỢC đoán ngược thành 'dưới 500k'!
   - ""ConsultingAdvice"": Người dùng xin lời khuyên, hỏi ý kiến, so sánh lựa chọn quà tặng (vd: '30 tuổi nên tặng ví da hay đồng hồ?', 'Mới quen bạn gái nên tặng gì cho tinh tế?').
     * directAnswer: Câu trả lời tư vấn sâu sắc, tâm lý, nhiệt tình (2-3 câu).
     * shouldIncludeProduct: true nếu có thể gợi ý 1 sản phẩm cụ thể, false nếu chỉ trò chuyện lý thuyết.
   - ""ProductDetailQnA"": Người dùng hỏi thông số, tính năng, chất liệu của sản phẩm đang xem (vd: 'Món này có chống nước không?', 'Dây da hay kim loại?', 'Có được đổi trả không?').
     * directAnswer: Câu trả lời ngắn gọn, thực tế về tính năng sản phẩm.
     * shouldIncludeProduct: true.
   - ""ServicePolicy"": Người dùng hỏi chính sách dịch vụ của website (vd: 'Shop có gói quà không?', 'Có viết thiệp mừng không?', 'Giao hàng mất mấy ngày?').
     * directAnswer: ""Dạ hầu hết các set quà trên GiftFinder từ các gian hàng đối tác uy tín đều có hộp đóng gói sang trọng kèm túi xách và hỗ trợ viết thiệp theo yêu cầu bạn nhé! Bạn có thể xem chi tiết trong gian hàng nha.""
     * shouldIncludeProduct: false.
   - ""OffTopic"": Người dùng hỏi kiến thức ngoài lề, trò chuyện vu vơ, giải toán, thời tiết, viết code...

2. ĐÁNH GIÁ TÍNH HỢP LỆ (isValidGiftIntent: boolean):
   - Đặt isValidGiftIntent = true nếu câu thuộc các nhóm ""Recommendation"", ""InventoryInquiry"", ""ConsultingAdvice"", ""ProductDetailQnA"", ""ServicePolicy"".
   - ĐẶC BIỆT LƯU Ý VỀ CÂU QUÁ CHUNG CHUNG / THIẾU THÔNG TIN (Underspecified / Too Generic):
     Nếu câu của người dùng CHỈ CÓ ý định tìm quà chung chung mà HOÀN TOÀN KHÔNG CÓ người nhận, dịp tặng, ngân sách hay tên món đồ cụ thể (Ví dụ: 'quà', 'tìm quà', 'mua quà', 'tư vấn quà', 'tôi muốn mua quà', 'gợi ý quà tặng', 'tặng quà gì', 'có quà gì'...)
     -> BẮT BUỘC ĐẶT: isValidGiftIntent = false!
     -> ĐẶT aiMessage: ""Dạ bạn muốn tìm quà cho ai (ví dụ: bạn gái, bạn trai, bố mẹ, đồng nghiệp...), vào dịp gì (sinh nhật, kỷ niệm, 20/10...) và ngân sách khoảng bao nhiêu để mình gợi ý món quà chuẩn gu nhất nhé! 🎁✨""
     -> ĐẶT suggestedQuestions: [""Quà sinh nhật cho bạn gái tầm 500k"", ""Quà tặng mẹ dịp 20/10 tinh tế, ý nghĩa"", ""Quà cho đồng nghiệp nam ngân sách 300k""]
     -> Summary: ""Yêu cầu cung cấp thêm thông tin người nhận / dịp tặng / ngân sách""
     TUYỆT ĐỐI KHÔNG ĐƯỢC đặt isValidGiftIntent = true cho câu quá chung chung như vậy, vì hệ thống sẽ lấy bừa sản phẩm ngẫu nhiên để gợi ý gây trải nghiệm xấu cho người dùng!
     * LƯU Ý QUAN TRỌNG: Khi người dùng ĐÃ NÊU RÕ người nhận (ví dụ: 'quà cho ông bà', 'quà cho mẹ', 'quà cho bố', 'quà cho thầy cô'...) -> ĐÂY LÀ CÂU HỢP LỆ ĐẦY ĐỦ NGƯỜI NHẬN, BẮT BUỘC ĐẶT isValidGiftIntent = true!

   - LƯU Ý ĐẶC BIỆT VỀ TỪ 'MÓN' TRONG TIẾNG VIỆT:
     Trong ngữ cảnh website GiftFinder, từ 'món' trong các câu như 'món khác', 'đổi món', 'món nào khác', 'gợi ý món khác', 'xem món khác', 'món quà khác' LUÔN LUÔN là 'MÓN QUÀ' (GIFT ITEM), TUYỆT ĐỐI KHÔNG PHẢI là 'món ăn' (Food/Dish)! Khi người dùng nói 'món khác', 'đổi món', 'quà khác', 'cái khác', 'khác đi'... họ đang yêu cầu đổi sang một món quà khác -> BẮT BUỘC ĐẶT isValidGiftIntent = true! TUYỆT ĐỐI KHÔNG ĐƯỢC trả lời rằng họ đang tìm món ăn hay công thức nấu ăn!
   - Đặt isValidGiftIntent = false nếu câu của người dùng KHÔNG liên quan đến thế giới quà tặng (hỏi kiến thức ngoài lề, trò chuyện vu vơ, câu vô nghĩa, quấy rối, công kích...).

3. PHẢN HỒI THÔNG MINH (aiMessage & directAnswer: string hoặc null):
   - Nếu isValidGiftIntent = false: Điền aiMessage (1-2 câu đối đáp khéo léo).
   - Nếu thuộc InventoryInquiry, ConsultingAdvice, ServicePolicy: Điền directAnswer giải thích chu đáo, tự nhiên.

4. TRÍCH XUẤT TIÊU CHÍ (Nếu isValidGiftIntent = true):
   - relationship: CHỈ điền khi người dùng NÊU RÕ mối quan hệ / đối tượng cụ thể trong câu (ví dụ: 'bạn gái', 'bạn trai', 'đồng nghiệp', 'sếp', 'bố', 'mẹ', 'ông bà', 'vợ', 'chồng', 'con', 'cháu', 'crush', 'thầy cô', 'bạn thân'...). BẮT BUỘC DÙNG TIẾNG VIỆT CHUẨN MỰC ('bố', 'mẹ', 'bạn trai', 'bạn gái'...), TUYỆT ĐỐI KHÔNG dùng tiếng Anh ('father', 'mother', 'baby'...). NẾU người dùng chỉ nói chung chung là 'cho nam', 'cho nữ', hoặc không nêu rõ mối quan hệ -> BẮT BUỘC để relationship = null. Tuyệt đối KHÔNG tự ý suy đoán mối quan hệ.
   - targetGender: 1 trong các giá trị ['Male', 'Female', 'Unisex']:
     + 'Male': nếu người nhận là nam giới (bạn trai, đồng nghiệp nam, sếp nam, bố, ông, anh trai, em trai, chồng...).
     + 'Female': nếu người nhận là nữ giới (bạn gái, đồng nghiệp nữ, sếp nữ, mẹ, bà, chị gái, em gái, vợ...).
     + 'Unisex': ông bà, thầy cô, đồng nghiệp chung chung, bạn bè không rõ giới tính, hoặc không nhắc đến giới tính.
   - targetAge: số nguyên tuổi hoặc null.
   - targetZodiac: Tên tiếng Anh 1 trong 12 cung hoàng đạo (Aries, Taurus, Gemini, Cancer, Leo, Virgo, Libra, Scorpio, Sagittarius, Capricorn, Aquarius, Pisces) hoặc null.
   - minBudget: số nguyên VNĐ hoặc null.
   - maxBudget: số nguyên VNĐ hoặc null.
   - budgetType: 1 trong các giá trị [""Above"", ""Below"", ""Around"", ""Range""] hoặc null (nếu người dùng không nhắc đến giá tiền).
   QUY TẮC PHÂN TÍCH NGỮ NGHĨA GIÁ TIỀN TIẾNG VIỆT BẮT BUỘC:
     * NGUYÊN TẮC ƯU TIÊN MỐC GIÁ MỚI NHẤT (QUAN TRỌNG NHẤT):
       Nếu trong câu có nhiều mốc ngân sách được nhắc đến hoặc có dấu hiệu cập nhật ngân sách (ví dụ: '500k, đổi sang trên 500k' hoặc 'dưới 500k, giá trên 500k'):
       BẮT BUỘC ÁP DỤNG MỐC GIÁ MỚI NHẤT Ở CUỐI CÂU ('trên 500k' -> minBudget = 500000, maxBudget = null, budgetType = ""Above"").
       TUYỆT ĐỐI KHÔNG ĐƯỢC lấy mốc giá cũ ở đầu câu, và TUYỆT ĐỐI KHÔNG ĐƯỢC biến nó thành 'Around' hay khoảng giá!
     1. Trường hợp 'Trên / Hơn / Từ... trở lên' (budgetType = ""Above""):
        - Ví dụ: 'quà trên 200k', 'hơn 500 cành', 'từ 1 triệu trở lên', 'trên 2 lít'
        => minBudget = giá trị (200000), maxBudget = null, budgetType = ""Above"".
     2. Trường hợp 'Dưới / Tối đa / Đổ lại' (budgetType = ""Below""):
        - Ví dụ: 'quà dưới 200k', 'tối đa 5 lít', 'dưới 1 củ', '300k đổ lại'
        => minBudget = null, maxBudget = giá trị (200000), budgetType = ""Below"".
     3. Trường hợp 'Tầm / Khoảng / Loanh quanh' (budgetType = ""Around""):
        - Ví dụ: 'quà tầm 200k', 'khoảng 3 lít', 'loanh quanh 500 cành'
        => Tự động tính khoảng dung sai hợp lý (minBudget = ~75% mức giá, maxBudget = ~130% mức giá).
        Ví dụ tầm 200k => minBudget = 150000, maxBudget = 260000, budgetType = ""Around"".
     4. Trường hợp khoảng giá 'Từ... đến...' (budgetType = ""Range""):
        - Ví dụ: 'từ 200k đến 500k', 'khoảng 1 đến 2 củ'
        => minBudget = 200000, maxBudget = 500000, budgetType = ""Range"".
     5. Quy đổi từ lóng tiền tệ Việt Nam:
        + 'lốp', 'lít', 'cành' = TRĂM NGHÌN (100.000 VNĐ). (VD: '3 lốp' = 300000, '5 cành' = 500000, '2 lít' = 200000).
        + 'củ', 'chai', 'm', 'triệu' = TRIỆU (1.000.000 VNĐ). (VD: '2 củ' = 2000000, '1 củ rưỡi' = 1500000).
        + 'k' = NGHÌN (1.000 VNĐ). (VD: '200k' = 200000, '50k' = 50000).
     6. Trường hợp từ ngữ định tính:
        + 'đắt tiền', 'cao cấp', 'sang trọng', 'đắt hẳn', 'hàng hiệu', 'xịn' => minBudget = 800000, maxBudget = null, budgetType = ""Above"".
        + 'rẻ thôi', 'bình dân', 'tiết kiệm', 'hạt dẻ' => minBudget = null, maxBudget = 250000, budgetType = ""Below"".
   - occasion: dịp tặng (Sinh nhật, 20/10, Valentine, Giáng sinh, Kỷ niệm, Tốt nghiệp...) hoặc null.
   - interestKeywords: Mảng các từ khóa sở thích/phong cách phù hợp mà bạn tự suy luận ra từ ngữ cảnh (ví dụ: thích chill -> [""nến thơm"", ""trà"", ""sách""]).
   - summary: tóm tắt ngắn gọn 1 câu ý định tìm quà tự nhiên và mượt mà bằng tiếng Việt (Ví dụ: 'Tìm quà sinh nhật cho bạn gái tầm 500k mang phong cách tinh tế, sang trọng', TUYỆT ĐỐI KHÔNG ghép chuỗi thô thiển bằng các dấu phẩy lặp đi lặp lại; nếu có cung hoàng đạo thì luôn dùng tên tiếng Việt như Sư Tử, Bọ Cạp, Bạch Dương... thay vì tiếng Anh; quan hệ dùng tiếng Việt như Bố, Mẹ; đúng ngân sách quy đổi).
   - suggestedQuestions: [3 câu gợi ý tinh chỉnh tìm quà tiếp theo mang tính thực tế cao mà NGƯỜI DÙNG có thể bấm chọn ngay (viết theo vai người dùng gửi cho AI, TUYỆT ĐỐI KHÔNG dùng câu hỏi chất vấn ngược người dùng, TUYỆT ĐỐI KHÔNG lặp lại tiêu chí hoặc câu hỏi mà người dùng vừa yêu cầu trong prompt hiện tại. CHỈ gợi ý các tiêu chí phổ biến có thật trong kho quà: 'Tìm quà thiên về thiết thực, tiện dụng', 'Gợi ý quà mang phong cách lịch lãm, sang trọng', 'Đổi sang mức giá trên 500k', 'Quà có hộp đóng gói kèm thiệp chúc mừng', 'Quà chăm sóc sức khỏe, thư giãn'). TUYỆT ĐỐI KHÔNG tự bịa các chủ đề hiếm hoặc không có trong kho như 'phong thủy', 'tâm linh', 'truyền thống phong thủy' trừ khi người dùng chủ động yêu cầu].

Xuất ra dưới dạng JSON thuần túy duy nhất theo cấu trúc sau:
{
  ""intentType"": ""Recommendation"" | ""InventoryInquiry"" | ""ConsultingAdvice"" | ""ProductDetailQnA"" | ""ServicePolicy"" | ""OffTopic"",
  ""isValidGiftIntent"": true hoặc false,
  ""directAnswer"": ""câu trả lời trực tiếp chu đáo (nếu là InventoryInquiry, ConsultingAdvice, ProductDetailQnA, ServicePolicy)"",
  ""shouldIncludeProduct"": true hoặc false,
  ""aiMessage"": ""lời nhắn từ chối lịch thiệp (chỉ khi isValidGiftIntent = false)"",
  ""relationship"": ""bạn gái"" | ""đồng nghiệp nam"" | null,
  ""targetGender"": ""Male"" | ""Female"" | ""Unisex"",
  ""targetAge"": số nguyên hoặc null,
  ""targetZodiac"": ""Aries"" ... hoặc null,
  ""minBudget"": số nguyên VNĐ hoặc null,
  ""maxBudget"": số nguyên VNĐ hoặc null,
  ""budgetType"": ""Above"" | ""Below"" | ""Around"" | ""Range"" hoặc null,
  ""occasion"": ""sinh nhật"" ... hoặc null,
  ""interestKeywords"": [""từ khóa 1"", ""từ khóa 2""],
  ""summary"": ""tóm tắt 1 câu ngắn gọn"",
  ""suggestedQuestions"": [""3 câu gợi ý tinh chỉnh cho người dùng""]
}";

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

                if (root.TryGetProperty("intentType", out var itEl) && itEl.ValueKind == JsonValueKind.String)
                {
                    var itStr = itEl.GetString();
                    if (!string.IsNullOrEmpty(itStr) && Enum.TryParse<ChatIntentType>(itStr, true, out var parsedType))
                    {
                        intent.IntentType = parsedType;
                    }
                }

                if (root.TryGetProperty("directAnswer", out var daEl) && daEl.ValueKind == JsonValueKind.String)
                    intent.DirectAnswer = daEl.GetString();

                if (root.TryGetProperty("shouldIncludeProduct", out var sipEl) && (sipEl.ValueKind == JsonValueKind.True || sipEl.ValueKind == JsonValueKind.False))
                    intent.ShouldIncludeProduct = sipEl.GetBoolean();

                if (root.TryGetProperty("isValidGiftIntent", out var validEl) && (validEl.ValueKind == JsonValueKind.True || validEl.ValueKind == JsonValueKind.False))
                    intent.IsValidGiftIntent = validEl.GetBoolean();

                // Nếu là các nhóm ý định hỏi đáp/tư vấn quà tặng hợp lệ -> luôn coi là hợp lệ (không bị guardrail chặn nhầm)
                if (intent.IntentType == ChatIntentType.InventoryInquiry ||
                    intent.IntentType == ChatIntentType.ConsultingAdvice ||
                    intent.IntentType == ChatIntentType.ProductDetailQnA ||
                    intent.IntentType == ChatIntentType.ServicePolicy)
                {
                    intent.IsValidGiftIntent = true;
                }

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

                if (root.TryGetProperty("targetGender", out var genEl) && genEl.ValueKind == JsonValueKind.String)
                    intent.TargetGender = genEl.GetString();

                // Chỉ fallback xác định giới tính khi AI chưa nhận diện được
                if (string.IsNullOrEmpty(intent.TargetGender))
                {
                    pLower = prompt.ToLowerInvariant();
                    if (pLower.Contains(" nam") || pLower.Contains("bạn trai") || pLower.Contains("cho chàng") || pLower.Contains("cho bố") || pLower.Contains("anh trai") || pLower.Contains("chồng") || pLower.Contains("ông nội") || pLower.Contains("ông ngoại"))
                    {
                        intent.TargetGender = "Male";
                        if (intent.Relationship == "đồng nghiệp")
                        {
                            intent.Relationship = "đồng nghiệp nam";
                        }
                    }
                    else if (pLower.Contains(" nữ") || pLower.Contains("bạn gái") || pLower.Contains("cho nàng") || pLower.Contains("cho mẹ") || pLower.Contains("chị gái") || pLower.Contains("em gái") || pLower.Contains("vợ") || pLower.Contains("bà nội") || pLower.Contains("bà ngoại"))
                    {
                        intent.TargetGender = "Female";
                        if (intent.Relationship == "đồng nghiệp")
                        {
                            intent.Relationship = "đồng nghiệp nữ";
                        }
                    }
                }

                if (root.TryGetProperty("targetAge", out var ageEl) && ageEl.ValueKind == JsonValueKind.Number)
                    intent.TargetAge = ageEl.GetInt32();

                if (root.TryGetProperty("minBudget", out var minBudgetEl) && minBudgetEl.ValueKind == JsonValueKind.Number)
                    intent.MinBudget = minBudgetEl.GetInt64();

                if (root.TryGetProperty("maxBudget", out var maxBudgetEl) && maxBudgetEl.ValueKind == JsonValueKind.Number)
                    intent.MaxBudget = maxBudgetEl.GetInt64();

                if (root.TryGetProperty("budgetType", out var bTypeEl) && bTypeEl.ValueKind == JsonValueKind.String)
                    intent.BudgetType = bTypeEl.GetString();

                // Fallback nếu AI trả về min/max nhưng chưa gán budgetType
                if (string.IsNullOrEmpty(intent.BudgetType))
                {
                    if (intent.MinBudget.HasValue && !intent.MaxBudget.HasValue) intent.BudgetType = "Above";
                    else if (!intent.MinBudget.HasValue && intent.MaxBudget.HasValue) intent.BudgetType = "Below";
                    else if (intent.MinBudget.HasValue && intent.MaxBudget.HasValue) intent.BudgetType = "Range";
                }

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

                // Bảo đảm tuyệt đối cho câu hỏi nghi vấn/phủ định về kho hàng (vd: "Không có món nào trên 500k à?")
                var negativeInventoryMatch = System.Text.RegularExpressions.Regex.Match(
                    prompt.ToLowerInvariant(),
                    @"(không\s*có|sao\s*không\s*có|sao\s*không\s*thấy|chẳng\s*có|chả\s*có)\s*(món|quà|set|cái nào)?\s*(nào)?\s*(trên|hơn|từ)\s*(\d+)\s*(k|cành|lít|lốp|củ|triệu|tr)?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (negativeInventoryMatch.Success)
                {
                    intent.IntentType = ChatIntentType.InventoryInquiry;
                    intent.BudgetType = "Above";
                    if (long.TryParse(negativeInventoryMatch.Groups[5].Value, out var nVal))
                    {
                        var unit = negativeInventoryMatch.Groups[6].Value.ToLowerInvariant();
                        long budget = (unit == "k") ? nVal * 1000 :
                                      (unit == "cành" || unit == "lít" || unit == "lốp") ? nVal * 100000 :
                                      (unit == "củ" || unit == "triệu" || unit == "tr") ? nVal * 1000000 : 
                                      (nVal < 1000 ? nVal * 1000 : nVal);
                        intent.MinBudget = budget;
                        intent.MaxBudget = null;
                    }
                    intent.ShouldIncludeProduct = true;
                    intent.IsValidGiftIntent = true;
                    if (string.IsNullOrWhiteSpace(intent.DirectAnswer))
                    {
                        intent.DirectAnswer = $"Dạ bên mình có các set quà cao cấp trên {(intent.MinBudget ?? 500000):N0}đ rất sang trọng và tinh tế chứ ạ! Dưới đây là gợi ý nổi bật nhất để bạn tham khảo nhé:";
                    }
                }

                // Bảo đảm nhận diện câu hỏi thông số sản phẩm đang xem (Contextual Product Q&A)
                var prodQnAMatch = System.Text.RegularExpressions.Regex.IsMatch(
                    prompt.ToLowerInvariant(),
                    @"(món\s*này|sản\s*phẩm\s*này|cái\s*này|em\s*này)\s*(có\s*chống\s*nước|chống\s*nước\s*không|bằng\s*gì|chất\s*liệu|dây\s*da|dây\s*thép|bảo\s*hành|mấy\s*món|gồm\s*những\s*gì|kích\s*thước|size)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (prodQnAMatch)
                {
                    intent.IntentType = ChatIntentType.ProductDetailQnA;
                    intent.ShouldIncludeProduct = true;
                    intent.IsValidGiftIntent = true;
                }

                return intent;
            }
            else
            {
                // Fallback khi AI gặp lỗi (429, timeout, network): Không chặn người dùng mà bóc tách từ khóa cơ bản từ prompt
                return CreateFallbackIntent(prompt, $"Hệ thống đang gợi ý dựa trên từ khóa do AI tạm thời bận ({statusCode})");
            }
        }
        catch (Exception ex)
        {
            return CreateFallbackIntent(prompt, $"Hệ thống đang gợi ý dựa trên từ khóa ({ex.Message})");
        }
    }

    private ParsedRecommendationIntent CreateFallbackIntent(string prompt, string? notice = null)
    {
        var p = prompt.Trim();
        var pLower = p.ToLowerInvariant();

        // 0. Chặn ngay nếu là câu quá chung chung / thiếu thông tin cụ thể (chỉ có từ "quà", "tìm quà", "tư vấn"...)
        var genericGiftRegex = new System.Text.RegularExpressions.Regex(
            @"^(tôi\s*muốn\s*|mình\s*muốn\s*|em\s*muốn\s*|cần\s*|hãy\s*|giúp\s*mình\s*|giúp\s*em\s*|cho\s*mình\s*|cho\s*em\s*)?(quà|quà\s*tặng|món\s*quà|tìm\s*quà|tìm\s*quà\s*tặng|tìm\s*món\s*quà|mua\s*quà|mua\s*quà\s*tặng|mua\s*món\s*quà|chọn\s*quà|chọn\s*quà\s*tặng|chọn\s*món\s*quà|tư\s*vấn|tư\s*vấn\s*quà|tư\s*vấn\s*quà\s*tặng|tư\s*vấn\s*chọn\s*quà|tư\s*vấn\s*giúp|gợi\s*ý|gợi\s*ý\s*quà|gợi\s*ý\s*quà\s*tặng|gợi\s*ý\s*giúp|cần\s*tìm\s*quà|cần\s*mua\s*quà|có\s*quà\s*gì|có\s*món\s*gì|mua\s*gì|tặng\s*gì|quà\s*gì|chọn\s*gì|mua\s*quà\s*gì|tặng\s*quà\s*gì)(\s*(ạ|nhé|nha|với|giúp|giúp\s*mình|giúp\s*em|giùm|đi|nào|với\s*ạ))*[.?!,\s]*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (genericGiftRegex.IsMatch(p))
        {
            return new ParsedRecommendationIntent
            {
                IsValidGiftIntent = false,
                AiMessage = "Dạ bạn muốn tìm quà cho ai (ví dụ: bạn gái, bạn trai, bố mẹ, đồng nghiệp...), vào dịp gì (sinh nhật, kỷ niệm, 20/10...) và ngân sách khoảng bao nhiêu để mình gợi ý món quà chuẩn gu nhất nhé! 🎁✨",
                Summary = "Yêu cầu cung cấp thêm thông tin người nhận / dịp tặng / ngân sách",
                SuggestedQuestions = new List<string>
                {
                    "Quà sinh nhật cho bạn gái tầm 500k",
                    "Quà tặng mẹ dịp 20/10 tinh tế, ý nghĩa",
                    "Quà cho đồng nghiệp nam ngân sách 300k"
                }
            };
        }

        // 1. Kiểm tra xem có bất kỳ từ khóa liên quan đến quà tặng / người nhận / dịp / ngân sách / món đồ hay không
        var hasGiftContext = System.Text.RegularExpressions.Regex.IsMatch(pLower, @"(quà|tặng|mua|gợi\s*ý|sinh\s*nhật|valentine|giáng\s*sinh|noel|tết|kỷ\s*niệm|tân\s*gia|tốt\s*nghiệp|20/10|8/3|20/11|14/2|1/6|24/12|bạn\s*gái|bạn\s*trai|nam|nữ|nàng|chàng|bố|ba|mẹ|má|sếp|đồng\s*nghiệp|vợ|chồng|anh\s*trai|em\s*trai|chị\s*gái|em\s*gái|bé|con|crush|người\s*yêu|ngân\s*sách|giá|tiền|k\b|cành|lít|củ|triệu|đ\b|nến thơm|nước hoa|son|ví|thắt lưng|trang sức|dây chuyền|đồng hồ|hoa|bình giữ nhiệt|ly|cốc|áo|váy|khăn|túi|balo|tai nghe)");

        var isDateOnlyInChat = System.Text.RegularExpressions.Regex.IsMatch(p.Trim(),
            @"^(0?[1-9]|[12]\d|3[01])\s*/\s*(0?[1-9]|1[0-2])(\s*/\s*\d{2,4})?$");

        // 2. Kiểm tra nếu là câu hỏi toán học hoặc công thức thuần tuý (không phải ngày lễ như 20/10, 8/3...)
        var isMathOrCalculationChat = !hasGiftContext && !isDateOnlyInChat && (
            (System.Text.RegularExpressions.Regex.IsMatch(p, @"^[\d\s\+\-\*/\=\(\)\.\,\^\%\?]+$") &&
             System.Text.RegularExpressions.Regex.IsMatch(p, @"[\+\*]|\d+\s*[\-\/]\s*\d+")) ||
            System.Text.RegularExpressions.Regex.IsMatch(pLower, @"(giải\s+toán|làm\s+toán|phép\s+tính|bài\s+toán)"));

        if (isMathOrCalculationChat)
        {
            return new ParsedRecommendationIntent
            {
                IsValidGiftIntent = false,
                AiMessage = "Dạ GiftFinder là trợ lý AI chuyên tư vấn và gợi ý quà tặng thôi ạ! 🎁 Mình không hỗ trợ giải toán hay phép tính được. Bạn có thể cho mình biết người nhận, dịp tặng hoặc mức ngân sách để mình chọn quà nhé!",
                Summary = "Phép tính toán học"
            };
        }

        // 3. Kiểm tra nếu là câu chào hỏi đơn thuần không có ngữ cảnh quà tặng
        var isGreeting = System.Text.RegularExpressions.Regex.IsMatch(pLower, @"^(hello|hi|chào|xin chào|alo|hey|ê|ơi|test|cảm ơn|thanks|bye|tạm biệt)(\s|[!.,?]|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (isGreeting && !hasGiftContext)
        {
            return new ParsedRecommendationIntent
            {
                IsValidGiftIntent = false,
                AiMessage = "👋 Chào bạn! Mình là AI Gift Concierge. Bạn muốn tìm quà cho ai (bạn gái, bạn trai, bố mẹ, sếp...), vào dịp nào và ngân sách khoảng bao nhiêu?",
                Summary = "Chào hỏi"
            };
        }

        if (!hasGiftContext)
        {
            // Không chứa bất kỳ từ khóa quà tặng nào -> Đánh dấu không hợp lệ để KHÔNG bao giờ tìm sản phẩm bừa bãi trong database!
            return new ParsedRecommendationIntent
            {
                IsValidGiftIntent = false,
                AiMessage = "Dạ GiftFinder là trợ lý AI chuyên tư vấn và gợi ý quà tặng thôi ạ! 🎁 Bạn có thể chia sẻ người nhận, dịp tặng hoặc mức ngân sách để mình hỗ trợ chọn món quà ưng ý nhất nhé!",
                Summary = p
            };
        }

        // 4. Nếu CÓ ngữ cảnh quà tặng -> Bóc tách tiêu chí cơ bản để gợi ý chính xác
        var intent = new ParsedRecommendationIntent
        {
            IsValidGiftIntent = true,
            Summary = p,
            AiMessage = notice
        };

        // Giới tính & quan hệ
        if (System.Text.RegularExpressions.Regex.IsMatch(pLower, @"(nam|bạn trai|cho chàng|cho bố|cho ba|anh trai|em trai|chồng|sếp nam)"))
        {
            intent.TargetGender = "Male";
            if (pLower.Contains("bạn trai") || pLower.Contains("cho chàng")) intent.Relationship = "bạn trai";
            else if (pLower.Contains("chồng")) intent.Relationship = "chồng";
            else if (pLower.Contains("bố") || pLower.Contains("ba")) intent.Relationship = "bố";
            else if (pLower.Contains("anh trai")) intent.Relationship = "anh trai";
            else if (pLower.Contains("sếp")) intent.Relationship = "sếp nam";
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(pLower, @"(nữ|bạn gái|cho nàng|cho mẹ|cho má|chị gái|em gái|vợ|sếp nữ)"))
        {
            intent.TargetGender = "Female";
            if (pLower.Contains("bạn gái") || pLower.Contains("cho nàng")) intent.Relationship = "bạn gái";
            else if (pLower.Contains("vợ")) intent.Relationship = "vợ";
            else if (pLower.Contains("mẹ") || pLower.Contains("má")) intent.Relationship = "mẹ";
            else if (pLower.Contains("chị gái")) intent.Relationship = "chị gái";
            else if (pLower.Contains("sếp")) intent.Relationship = "sếp nữ";
        }
        else if (pLower.Contains("ông bà"))
        {
            intent.TargetGender = "Unisex";
            intent.Relationship = "ông bà";
        }
        else if (pLower.Contains("thầy cô") || pLower.Contains("thầy") || pLower.Contains("cô giáo"))
        {
            intent.TargetGender = "Unisex";
            intent.Relationship = "thầy cô";
        }
        else if (pLower.Contains("đồng nghiệp"))
        {
            intent.TargetGender = "Unisex";
            intent.Relationship = "đồng nghiệp";
        }
        else if (pLower.Contains("sếp"))
        {
            intent.TargetGender = "Unisex";
            intent.Relationship = "sếp";
        }
        else if (pLower.Contains("bạn thân"))
        {
            intent.TargetGender = "Unisex";
            intent.Relationship = "bạn thân";
        }

        // Dịp tặng
        if (pLower.Contains("sinh nhật")) intent.Occasion = "Sinh nhật";
        else if (pLower.Contains("giáng sinh") || pLower.Contains("noel")) intent.Occasion = "Giáng sinh";
        else if (pLower.Contains("20/10") || pLower.Contains("20-10")) intent.Occasion = "20/10";
        else if (pLower.Contains("valentine")) intent.Occasion = "Valentine";
        else if (pLower.Contains("tân gia")) intent.Occasion = "Tân gia";
        else if (pLower.Contains("tết")) intent.Occasion = "Tết";
        else if (pLower.Contains("kỷ niệm")) intent.Occasion = "Kỷ niệm";

        // Ngân sách cơ bản
        var budgetMatch = System.Text.RegularExpressions.Regex.Match(pLower, @"(\d+)\s*(k|cành|lít|lốp|củ|triệu|tr)");
        if (budgetMatch.Success && long.TryParse(budgetMatch.Groups[1].Value, out var num))
        {
            var unit = budgetMatch.Groups[2].Value;
            long budget = (unit == "k") ? num * 1000 :
                          (unit == "cành" || unit == "lít" || unit == "lốp") ? num * 100000 :
                          (unit == "củ" || unit == "triệu" || unit == "tr") ? num * 1000000 : num;

            if (pLower.Contains("dưới") || pLower.Contains("tối đa") || pLower.Contains("đổ lại"))
            {
                intent.MaxBudget = budget;
                intent.BudgetType = "Below";
            }
            else if (pLower.Contains("trên") || pLower.Contains("hơn") || pLower.Contains("từ"))
            {
                intent.MinBudget = budget;
                intent.BudgetType = "Above";
            }
            else
            {
                intent.MinBudget = (long)(budget * 0.75);
                intent.MaxBudget = (long)(budget * 1.3);
                intent.BudgetType = "Around";
            }
        }

        // Tinh lọc từ khóa sở thích/sản phẩm (loại bỏ các hư từ không mang nghĩa quà tặng và các từ quan hệ/dịp lễ đã được trích xuất)
        var stopWords = new HashSet<string>
        {
            "quà", "cho", "tầm", "giá", "khoảng", "dưới", "trên", "món", "gợi", "ý", "tìm", "mua", "tặng",
            "với", "mức", "nào", "được", "không", "một", "nhé", "ạ", "ơi", "hello", "hi", "và", "của", "là", "thì",
            "bạn", "gái", "trai", "nam", "nữ", "sinh", "nhật", "mẹ", "bố", "ba", "má", "sếp", "đồng", "nghiệp"
        };
        var rawWords = pLower.Split(new[] { ' ', ',', '.', ';', '?', '!', ':', '-', '+', '=', '/', '\\', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                             .Where(w => w.Length > 1 && !stopWords.Contains(w) && !long.TryParse(w, out _))
                             .Take(15)
                             .ToList();

        intent.InterestKeywords = rawWords;
        return intent;
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
- giftAdvice: 1-2 câu tư vấn giá trị quà tặng truyền cảm hứng bằng tiếng Việt lịch thiệp.
- zodiacRationale: 1-2 câu giải thích tại sao món quà này lại hợp với các cung hoàng đạo đã chọn (ví dụ: phong cách, tính cách cung hoàng đạo).";

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

                if (root.TryGetProperty("zodiacRationale", out var ratEl) && ratEl.ValueKind == JsonValueKind.String)
                    result.ZodiacRationale = ratEl.GetString();
                else
                    result.ZodiacRationale = result.GiftAdvice;

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
            GiftAdvice = $"{productName} là một món quà ý nghĩa, tinh tế và thiết thực để gửi gắm tình cảm.",
            ZodiacRationale = $"{productName} mang phong cách tinh tế, ấm cúng, rất phù hợp với gu thẩm mỹ và tính cách của Cự Giải và Thiên Bình."
        };
    }

    /// <summary>
    /// Trả lời câu hỏi thắc mắc của người dùng về một sản phẩm cụ thể đang xem (công năng, chất liệu, tính phù hợp...)
    /// </summary>
    public async Task<string> AnswerProductInquiryAsync(
        string productName,
        string? productDescription,
        long productPrice,
        string userQuestion,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = @"Bạn là AI Concierge - chuyên viên tư vấn quà tặng cao cấp và chu đáo của GiftFinder.
Khách hàng đang xem một sản phẩm quà tặng cụ thể và đặt câu hỏi thắc mắc về sản phẩm đó (ví dụ: công năng, khả năng chống nước, chất liệu dây/vỏ, độ tuổi phù hợp, phụ kiện đi kèm...).
Nhiệm vụ của bạn:
1. Đọc kỹ tên sản phẩm, giá tiền và phần mô tả sản phẩm được cung cấp.
2. Trả lời trực tiếp, rõ ràng, trung thực và thân thiện (2-3 câu).
   - Nếu thông tin có rõ trong tên hoặc mô tả (ví dụ: chống nước 3ATM, hợp kim, dạ quang, dây thép...), hãy khẳng định chắc chắn và giải thích thêm ứng dụng thực tế tiện lợi cho người nhận.
   - Nếu thông tin không được ghi rõ, hãy nhận định khách quan dựa trên loại sản phẩm thực tế, đồng thời khuyên người dùng kiểm tra thêm khi mở gian hàng hoặc nhắn tin cho shop.
3. Giữ thái độ lịch thiệp, ấm áp (dùng 'Dạ...', 'bạn nhé!').
Xuất câu trả lời dưới dạng text thuần túy (Plain text), không dùng markdown bôi đậm quá mức, không xuất JSON.";

        var userContent = $@"SẢN PHẨM ĐANG XEM:
- Tên: {productName}
- Giá: {productPrice:N0} VNĐ
- Mô tả / Thông số: {(string.IsNullOrWhiteSpace(productDescription) ? "Chưa có mô tả chi tiết" : productDescription)}

CÂU HỎI CỦA KHÁCH HÀNG:
""{userQuestion}""";

        var messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userContent }
        };

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var (success, rawText, statusCode, errorMsg) = await CallGroqAsync(messages, jsonMode: false, linkedCts.Token);
            if (success && !string.IsNullOrWhiteSpace(rawText))
            {
                return rawText.Trim().Trim('\"');
            }
        }
        catch
        {
            // Fallback khi AI lỗi
        }

        return $"Dạ sản phẩm \"{productName}\" (giá khoảng {productPrice:N0}đ) là một lựa chọn rất được ưa chuộng và có độ hoàn thiện tốt. Bạn có thể bấm 'Mở gian hàng mua ngay' hoặc 'Chi tiết' để xem đầy đủ thông số kỹ thuật và tư vấn trực tiếp từ nhà bán hàng nhé!";
    }
}
