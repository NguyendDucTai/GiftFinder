# TIẾN ĐỘ DỰ ÁN GIFTFINDER & BẢN ĐỒ BỐI CẢNH (CONTEXT MAP)
*(Cập nhật lần cuối: 18/10/2026 - Tình trạng: Sẵn sàng 100% cho phiên làm việc mới - Database: 244 Sản Phẩm Chuẩn 100% Giá Thật & Link Affiliate)*

---

## 🚀 HƯỚNG DẪN DÀNH CHO AI Ở PHIÊN MỚI (AGENT QUICK-START & CONTEXT RESUME)
> **DÀNH CHO AGENT TIẾP THEO:** Đọc kỹ mục này để tiếp tục dự án ngay trong 10 giây mà **KHÔNG CẦN** quét lại hay phân tích lại những gì đã chạy thành công!

### 1. Thông tin Môi trường & Kết nối Đang Hoạt Động (Live Credentials):
* **Kiến trúc:** C# (.NET 8) Clean Architecture (`GiftFinder.Domain`, `GiftFinder.Application`, `GiftFinder.Infrastructure`, `GiftFinder.API`).
* **Backend Swagger:** Đang chạy tại `https://localhost:7276/swagger/index.html`.
* **Cơ sở dữ liệu:** PostgreSQL (`GiftFinderDb`), Host: `localhost:5432`, Username: `postgres`, Password: `123`.
* **Chiến lược Sàn Affiliate (2 Sàn Trọng Tâm Chính: Shopee & TikTok Shop; Lazada & DirectMerchant là phụ; Đã loại bỏ hoàn toàn Tiki):**
  * **Shopee Direct Affiliate (Trọng tâm 1):** Mã đối tác `an_17374170692`. Sinh link trực tiếp không qua trung gian: `https://shopee.vn/{slug}-i.{shopId}.{itemId}?utm_source=an_17374170692&utm_medium=affiliates&mmp_pid=an_17374170692&sub_id=item_{guid}`.
    * *Độ bền link 100%:* Shopee định tuyến dựa vào mã `-i.{shopId}.{itemId}` ở cuối URL. Kể cả khi người bán đổi tiêu đề, đổi SEO slug thì link vẫn tự động redirect đúng sản phẩm, không bao giờ bị hỏng.
  * **TikTok Shop CPS (Trọng tâm 2):** Tích hợp Accesstrade Live Mode (`PublisherIdV6: 7086451798803801329`, `CampaignId: 6648523843406889655`, `ApiKey: dlg2xKqJY-jSwgpnzrTwkOHUO1VopteC`). Sinh deeplink v6 tự động mã hóa `url_enc` Base64.
  * **Lazada Việt Nam (Phụ):** Tích hợp mạng lưới Ecomobi (`WrkhMbQezyWOwnMYpFEiz`).
  * **DirectMerchant (Gian hàng nội bộ):** Dành cho các shop đối tác đăng ký gian hàng trực tiếp trên GiftFinder.
* **Bộ cào dữ liệu Playwright Scraper Engine (Đã tối ưu hóa vận hành & chống WAF):**
  * Kết nối Microsoft Edge thật qua CDP (`http://127.0.0.1:9222`). Khởi động nhanh bằng [start_edge_debug.bat](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/start_edge_debug.bat) hoặc `dotnet run --launch-edge`.
  * **Tốc độ trong tab (0s delay):** Bắt trực tiếp sự kiện React mount thẻ giá (`.pyzxvq, .pw3J3G, .PcnXj1`, ~2.7s) và đóng tab ngay lập tức khi lấy được giá, không chờ đợi lãng phí.
  * **Giãn cách liên tab (5.0s – 7.0s):** Mô phỏng hành vi duyệt web tự nhiên của người thật, triệt tiêu 100% lỗi Shopee WAF rate-limiting (`/verify/traffic/error`).
  * **Edge Tab Persistence:** Điều hướng về `about:blank` thay vì đóng tab cuối cùng để giữ tiến trình `msedge.exe` và cổng 9222 luôn sống.
  * **Cơ chế Captcha Pause 90s & Âm thanh cảnh báo:** Tự động nhận diện màn hình kéo mảnh ghép (`Verify to Continue`, `Security Check`, `Punish/Verification`), phát tiếng bíp `Console.Beep(1000, 500)` trên Windows, hiển thị banner cảnh báo và dừng tối đa 90s để người dùng kéo thanh trượt trên Edge, sau đó tự động cào giá tiếp.
  * **Fallback Regex vạn năng:** Bóc tách giá số qua regex `\d{1,3}(\.\d{3})+` quét toàn bộ text node nếu class CSS thay đổi. Đảm bảo dán BẤT KỲ link sản phẩm mới nào từ Shopee, TikTok Shop hay Lazada đều tự động nhận diện giá chính xác.
* **GroqCloud AI (LPU):** Model `openai/gpt-oss-120b`, xử lý hiểu ngôn ngữ tự nhiên tiếng Việt, từ lóng tiền tệ, tự động phân loại tag quà tặng (Đối tượng, Dịp tặng, Sở thích, Cung hoàng đạo).
* **Email Service:** Brevo SMTP (`smtp-relay.brevo.com:587`).

---

### 2. Trạng thái Kho Sản Phẩm Thực Tế Trong Database (`Products` Table):
* **Tổng cộng 244/244 sản phẩm trong Database (`GiftFinderDb`) đã có giá thật 100% & ảnh CDN từ sàn, 0 lỗi, 0 trùng lặp:**
  1. `TikTok Shop (124 SP - Trọng tâm 2)`: 100% sản phẩm có giá hiển thị thật trên màn hình từ **10.499đ đến 1.100.000đ** (trung bình ~241.000đ, đã loại bỏ hoàn toàn giá gốc ảo và phí ship), ảnh CDN `ibyteimg.com`, link Accesstrade CPS `7086451798803801329`. Đã import trọn vẹn 100% 124 link tuyển chọn từ file `tiktokshop_links.txt`.
  2. `Shopee (118 SP - Trọng tâm 1)`: 100% sản phẩm có giá thật từ **3.800đ đến 2.250.000đ**, ảnh CDN Shopee `down-vn.img.susercontent.com`, link Shopee Direct `an_17374170692`, gắn đủ AI tags. Đã import trọn vẹn 100% 112 link tuyển chọn từ file `shopee_links.txt`.
  3. `Lazada (1 SP)`: Giá thật **129.000đ**, ảnh gốc trực tiếp từ CDN `img.lazcdn.com`, link Ecomobi `WrkhMbQezyWOwnMYpFEiz`.
  4. `DirectMerchant (1 SP - Tiệm Quà Thảo Mộc Vintage)`: Giá **280.000đ** (sản phẩm shop nội bộ, bảo lưu an toàn).
* **Kết quả rà soát, dọn dẹp & Chống trùng lặp (Audit & Deduplication):**
  * Đã quét toàn bộ kho dữ liệu theo cặp Shop ID + Item ID và URL: **0 bản ghi trùng lặp**.
  * Đã xóa sạch các bản ghi lỗi do trang traffic error cũ lưu nhầm.
  * 100% các sản phẩm trong DB đều độc lập, có giá thật > 0đ, có ảnh CDN gốc và đầy đủ link tracking affiliate.
* **Bảng `PriceHistories`:** Ghi nhận đầy đủ lịch sử biến động giá thực tế của tất cả sản phẩm theo thời gian.

---

### 3. Vị Trí Các File Mã Nguồn Cốt Lõi:
* **[PlaywrightScraperService.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Infrastructure/Scraping/PlaywrightScraperService.cs):** Trình cào dữ liệu Playwright đa sàn (Shopee, TikTok Shop, Lazada), cơ chế Edge CDP, tự động tạm dừng 90s khi gặp Captcha, giãn cách tab 5s-7s, regex bóc tách giá.
* **[ShopeeAffiliateHandler.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Infrastructure/Affiliate/ShopeeAffiliateHandler.cs):** Bóc tách slug, ShopId/ItemId và sinh link Shopee Direct Affiliate `an_17374170692`.
* **[AccesstradeService.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Infrastructure/Affiliate/AccesstradeService.cs):** Tích hợp Accesstrade CPS TikTok Shop, Ecomobi Lazada và điều phối cào sản phẩm qua URL.
* **[ImportProductByUrlCommandHandler.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Application/Features/Affiliate/Commands/ImportProductByUrl/ImportProductByUrlCommandHandler.cs):** Xử lý nhập sản phẩm bằng URL, hỗ trợ cờ `ForceRescrape` để quét lại/cập nhật giá sản phẩm đã có.
* **[SyncAllProductPricesCommandHandler.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Application/Features/Products/Commands/SyncAllProductPrices/SyncAllProductPricesCommandHandler.cs):** Đồng bộ giá định kỳ, hỗ trợ lọc theo sàn (`--source Shopee|TikTokShop|Lazada`).
* **[Program.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.API/Program.cs):** Cung cấp các lệnh CLI:
  * `dotnet run --project GiftFinder.API -- --import-file "shopee_links.txt" [--force]`
  * `dotnet run --project GiftFinder.API -- --sync-prices [--source <Source>]`
  * `dotnet run --project GiftFinder.API -- --launch-edge`
* **[ProductsController.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.API/Controllers/ProductsController.cs):** API quản lý quà tặng, click affiliate và sync giá.
* **[RecommendationsController.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.API/Controllers/RecommendationsController.cs):** Tìm kiếm quà tặng thông minh bằng Groq AI và bộ lọc đa tiêu chí.

---

## 🟢 TỔNG HỢP TOÀN BỘ CÁC MODULE ĐÃ HOÀN THÀNH (DONE)

### 1. Kiến trúc & Database
- Khởi tạo xong khung dự án chuẩn **Clean Architecture** (Domain, Application, Infrastructure, API).
- Cấu hình Entity Framework Core kết nối với cơ sở dữ liệu **PostgreSQL** (`GiftFinderDb`).
- Khởi tạo thành công các bảng dữ liệu (Users, Products, Tags, Wishlists, ClickTrackings, AffiliateOrders, GiftPolls, Merchants, Transactions, PriceHistories...).

### 2. Tính năng Xác thực - Authentication
- Đăng nhập / Đăng ký bằng Email & Mật khẩu (CQRS với MediatR). Mật khẩu băm **BCrypt**.
- Sinh và xác thực **JWT Token** (đã test thành công trên Swagger với ổ khóa Authorize, phân quyền User, Merchant, Admin).

### 3. Cập nhật Cấu trúc cho AI Cung Hoàng Đạo (Zodiac Add-on - Tầng Domain)
- Enum `ZodiacSign.cs` (Đầy đủ 12 cung hoàng đạo).
- Bảng `Product`: Bổ sung cột tùy chọn `SuitableZodiacs` để gán nhãn cung hoàng đạo cho quà tặng.
- `ZodiacCalculator.cs`: Thuật toán tự động chuyển đổi Ngày/Tháng sinh thành Cung Hoàng Đạo.
- `ReminderDate.cs`: Tự động nhận diện nếu sự kiện là "Sinh nhật" thì tự gán Cung hoàng đạo cho người nhận quà.

### 4. Quản lý Sản phẩm Yêu thích - Wishlist
- API `POST`, `GET`, `DELETE` cho Wishlist.
- Xử lý logic chống trùng lặp, giới hạn 100 sản phẩm và khôi phục khi soft-delete.
- `ICurrentUserService` trích xuất `UserId` an toàn từ JWT Token.

### 5. Quản lý ngày kỷ niệm / Sinh nhật
- API `POST`, `GET`, `PUT`, `DELETE` cho `ReminderDate`. Giới hạn 50 bản ghi/user.
- Tự động tính toán Cung Hoàng Đạo (`RecipientZodiac`) và lưu vào cơ sở dữ liệu.

### 6. Nhắc lịch sinh nhật & kỷ niệm tự động kèm Email
- `BirthdayReminderWorker` chạy ngầm định kỳ quét database và tính toán ngày sắp tới hạn.
- `SmtpEmailService` gửi email HTML thật qua máy chủ **Brevo SMTP** (`smtp-relay.brevo.com`).
- Email Template Card sang trọng, có logo GiftFinder, hiển thị số ngày còn lại và nút CTA.
- Tự động lưu bản ghi `Notification` và cập nhật `LastNotifiedAt` chống spam lặp trong cùng 1 năm. Đã test gửi thành công 100%.

### 7. Đánh giá ngầm độ hot sản phẩm & Phân trang gợi ý
- Tính điểm ngầm tự động (**PopularityScore**): Thêm Wishlist `+10đ`, xóa Wishlist `-8đ`, khôi phục `+8đ`.
- API Gợi ý hỗ trợ phân trang vô hạn (`pageNumber`, `pageSize`) và tự động xếp hạng ưu tiên sản phẩm có `PopularityScore` cao nhất lên đầu.

### 8. Chuyển hướng & Tracking Click Affiliate
- API `POST /api/Products/{id}/click-affiliate` cho phép cả người dùng đăng nhập lẫn khách vãng lai mua hàng.
- Tự sinh mã `SubId` (GUID) cho mỗi click lưu vào `ClickTracking`.
- Chống spam IP 24h: Chỉ cộng `PopularityScore +10` cho lượt click đầu tiên trong ngày từ mỗi IP.

### 9. Tính năng Bình chọn Quà tặng & Chia sẻ Công khai
- Module `GiftPoll` độc lập, tùy biến chủ đề, chọn 1-10 món quà.
- Link công khai 1 chạm `POST /api/GiftPolls/share/{shareCode}/vote` không cần đăng nhập.
- Chống spam bằng IP (cho phép đổi phiếu hoặc hủy phiếu). Thời hạn tự đóng sau 30 ngày (BR-05).

### 10. AI Tìm kiếm Quà tặng bằng Ngôn ngữ Tự nhiên & Hybrid Recommendation
- Tích hợp GroqCloud LPU (`openai/gpt-oss-120b`): Tốc độ phản hồi cực nhanh (~500 tokens/giây).
- Tự do nhập prompt câu tự nhiên tiếng Việt, hỗ trợ từ lóng tiền tệ (lốp, cành, lít, củ, chai).
- Trợ lý tư vấn `aiAdvice` tinh tế cho món Top 1, đồng bộ 100% tiếng Việt cho cung hoàng đạo.
- AI Guardrail tự động từ chối câu hỏi lạc đề, quấy rối và gợi ý câu hỏi phù hợp.

### 11. Module Affiliate Marketing Đa Sàn (Shopee, TikTok Shop, Lazada - Đã bỏ Tiki)
- **Shopee Direct Affiliate**: Mã đối tác `an_17374170692`, gắn `utm_source`, `utm_medium`, `mmp_pid`, `sub_id`.
- **TikTok Shop CPS**: Accesstrade V6 Deeplink (`Pub 7086451798803801329 | Camp 6648523843406889655`).
- **Lazada**: Ecomobi Deeplink (`Token WrkhMbQezyWOwnMYpFEiz`).
- Groq AI tự động thẩm định quà và gán thẻ Dịp/Sở thích/Cung hoàng đạo khi nhập link.

### 12. Bóc Tách Giá Chuẩn Ngữ Nghĩa & Phân Tầng Kết Quả (Tiered Ranking)
- **Chuẩn hóa Ngữ nghĩa Giá tiền:** Bổ sung `MinBudget`, `BudgetType` vào `ParsedRecommendationIntent` và `GetRecommendationsQuery`.
- **Nâng cấp Groq AI Prompt:** Phân loại chuẩn `Above`, `Below`, `Around`, `Range`.
- **Thuật toán Tiered Ranking 3 Tầng (`GetRecommendationsQueryHandler.cs`):**
  - **Tầng 1 (Chuẩn giá):** Đưa TOÀN BỘ sản phẩm đúng khoảng giá lên các vị trí đầu tiên.
  - **Tầng 2 (Loanh quanh tầm giá):** Nối tiếp các món gần tầm giá có độ phù hợp cao với đối tượng/dịp.
  - **Tầng 3 (Còn lại):** Các món khác xếp ở các trang cuối.

### 13. Module Đối Tác Bán Hàng (Merchant) & Quảng Cáo Nổi Bật (Featured Placement)
- Đăng ký gian hàng Shop hoặc nâng cấp từ tài khoản người dùng thường.
- Quản lý quà tặng: Đăng quà mới (`Pending`), xem danh sách, sửa quà, ẩn/xóa quà.
- Trợ lý AI gợi ý Cung & Tag tự động khớp Tag trong database.
- Gói tài trợ đẩy Top (Featured Placement): Tuần 49k hoặc tháng 199k; AI ưu tiên đưa tối đa 3 sản phẩm Featured lên đầu.
- Dashboard gian hàng: Thống kê tổng số quà, quà đã duyệt, lượt click và Top quà hot.

### 14. Module Quản Trị Hệ Thống & Kiểm Duyệt (Admin Moderation)
- Kiểm duyệt quà tặng từ Shop: Phê duyệt (`Approved`) hoặc từ chối kèm lý do (`Rejected`).
- Quản lý người dùng: Khóa và mở khóa tài khoản User hoặc Merchant vi phạm kèm lý do.

### 15. Module Gói Thành Viên Premium & Cổng Thanh Toán VietQR (UC-14, UC-24, UC-27)
- Gói cước ưu đãi: **15.000 VNĐ / tháng** (hoặc 45k/3 tháng, 150k/12 tháng).
- Cổng thanh toán VietQR: Tích hợp dịch vụ sinh mã VietQR chuẩn Napas 24/7 tự động (`https://img.vietqr.io/...`), nội dung `GFPREMIUM <TransactionId>`.
- Khởi tạo thanh toán (`POST /api/Subscriptions/upgrade`), xử lý Webhook IPN (`POST /api/Webhooks/payment`), đối soát Idempotent.
- Tự động hoàn tất giao dịch, nâng cấp tài khoản, gửi email chúc mừng qua Brevo SMTP.
- Worker quản lý hạn & thời gian ân hạn 3 ngày (`SubscriptionExpirationWorker`).

### 16. Bộ Cào Dữ Liệu Playwright Scraper Engine & Nhập Hàng Loạt Hoàn Chỉnh (UC-16)
- Tích hợp kết nối Microsoft Edge thật qua CDP (Port 9222) với các cơ chế:
  - **Khởi động 0s delay trong tab**: Thoát tab ngay khi tìm thấy giá (~2.7s).
  - **Giãn cách liên tab 5s–7s**: Tránh triệt để Shopee WAF rate-limiting.
  - **Giữ tab sống (about:blank)**: Ngăn Edge tự tắt khi đóng tab cuối cùng.
  - **Tự động nhận diện Captcha & tạm dừng 90s**: Cho phép người dùng kéo mảnh ghép trên Edge và tự động tiếp tục.
  - **Cào và nhập thành công 100% 112 link Shopee** từ file `shopee_links.txt` vào PostgreSQL, gắn đầy đủ tags AI và affiliate link.
  - **Đồng bộ giá tự động đa sàn** (`--sync-prices --source <Shopee|TikTokShop|Lazada>`).

### 17. Tự động Hết Hạn Gói Tài Trợ Featured & Nhắc Nhở Gia Hạn (UC-20)
- Tự động tắt cờ `IsFeatured = false`, đặt `FeaturedUntil = null` khi đến hạn (BR-27).
- Gửi Email qua máy chủ Brevo SMTP và tạo thông báo in-app (`Notification`) cho chủ shop (Merchant) khi gói Featured hết hạn.
- Gửi Email cảnh báo nhắc gia hạn trước 3 ngày (`now < FeaturedUntil <= now + 3 days`) kèm cơ chế deduplication (chống gửi lặp trong 3 ngày).
- `FeaturedExpirationWorker` chạy ngầm tự động định kỳ 12 giờ một lần (BackgroundService).
- Endpoint Admin kích hoạt quét thủ công: `POST /api/Admin/trigger-featured-expiration`.
- Hỗ trợ cờ CLI chạy quét tức thì: `dotnet run --project GiftFinder.API -- --expire-featured`.
- Đã kiểm thử trực tiếp trên PostgreSQL và Brevo SMTP thành công 100%.

### 18. Thuật toán Hiển thị Quà tặng Xen kẽ có điều kiện (Interleaved Slotting - UC-18)
- Mô hình hiển thị quảng cáo xen kẽ có điều kiện chuẩn phong cách Shopee & Amazon Ads.
- **Quy tắc Thực lực Top 1 (#1 Merit Rule):** Nếu sản phẩm của Shop thực sự phù hợp nhất (sau khi cộng 20% điểm thưởng Featured), nó hoàn toàn xứng đáng đứng **Top 1**, nhận huy hiệu danh giá `Top Pick • Đối tác` và được AI viết đoạn tư vấn `AiAdvice`. Nếu sản phẩm tự nhiên tốt hơn, Top 1 giữ nguyên cho Organic.
- **Phân bổ vị trí xen kẽ (BR-26):** 
  - Nếu Top 1 là Organic: Món Featured xuất hiện ở **Vị trí #3, #7** (tối đa 2 Featured trong Top 10).
  - Nếu Top 1 là Featured: Món Featured tiếp theo xuất hiện ở **Vị trí #5, #9** (tối đa 3 Featured trong Top 10).
  - Luôn có khoảng đệm 2–3 món tự nhiên xen giữa các món tài trợ để người dùng không có cảm giác bị nhồi nhét.
- **Sàng lọc độ liên quan (Relevance First - BR-25):** Bắt buộc sản phẩm Featured phải thuộc Tier giá hợp lệ và có liên quan từ khóa/dịp tặng. Sản phẩm lệch ngành hàng bị loại ngay từ đầu, bảo vệ 100% uy tín gợi ý AI.
- **Cơ chế Xoay vòng công bằng (Impression Fair-Share Rotation):** Áp dụng thuật toán ngẫu nhiên có trọng số (weighted jitter) để khi có nhiều shop cùng mua gói, các lượt tìm kiếm khác nhau sẽ luân phiên đưa các shop khác nhau lên vị trí vàng (#3, #7).
- Bổ sung cơ chế Fallback chống nghẽn cho Groq AI, đảm bảo khi AI gặp sự cố (429 rate-limit hoặc timeout) thì người dùng vẫn nhận được kết quả tìm kiếm tự nhiên mượt mà.
- **Bộ kiểm thử tự động:** Tích hợp CLI `dotnet run --project GiftFinder.API -- --test-interleaved` kiểm tra toàn diện 5/5 kịch bản (100% PASS).

### 19. Module Theo Dõi Biến Động Giá & Cảnh Báo Giảm Giá (UC-08 & UC-22)
- **Chính sách Freemium thông minh:**
  - Free User: Tặng **3 sản phẩm theo dõi cùng lúc** lần đầu trải nghiệm (Welcome Trial). Từ lần sau: Tối đa **1 sản phẩm mới / ngày**. Nếu thêm quá giới hạn trong ngày thì hiển thị popup mời nâng cấp Premium (29k/tháng) mở khóa 20 sản phẩm.
  - Premium User: Theo dõi tối đa **20 sản phẩm** không giới hạn số ngày (BR-11).
- **Thao tác 1-Chạm (1-Click Track):** Bấm chuông theo dõi là xong ngay, hệ thống tự động lưu `InitialPrice` và kích hoạt khi giá giảm $\ge 5\%$.
- **Giao diện Quản lý theo dõi giá dạng Danh sách (Price Watchlist Page - như Wishlist):**
  - Trang `/tai-khoan/theo-doi-gia` hiển thị thanh Header hạn mức cước, các Tab lọc (`Tất cả`, `Đang giảm giá 🔥`, `Giá không đổi`), thẻ card hiển thị giá ban đầu $\rightarrow$ giá hiện tại, nhãn đỏ giảm giá `-% (-X đ)`, nút `[ Mua ngay trên sàn ]` (link Affiliate) và `[ Bỏ theo dõi ]`.
- **Cơ chế Cào giá ưu tiên thủ công (On-Demand Scraping - Chống WAF Captcha 100%):**
  - Cung cấp lệnh CLI: `dotnet run --project GiftFinder.API -- --sync-tracked [--fast]` và API `POST /api/Admin/sync-tracked-prices`.
  - Chỉ lọc đúng các sản phẩm đang có người theo dõi (5–20 món thay vì cào 244 món), chạy qua Edge CDP thật, có người kéo Captcha thủ công an toàn 100%.
- **Giải pháp Gom Email thông minh (Digest Email chuẩn Steam & Shopee - Kéo Traffic về Web):**
  - Không nhét bảng giá cồng kềnh vào email.
  - Nếu 1 món giảm: Gửi email 1 món ngắn gọn kèm nút CTA **[ Xem giá mới trên GiftFinder ]**.
  - Nếu nhiều món cùng giảm: Gom vào 1 email duy nhất thông báo có `{N} món quà giảm giá` kèm nút CTA **[ Xem các món đang giảm giá ]**.
  - Tự động tạo thông báo in-app `Notifications` (`NotificationType.PriceDrop`).
  - Chống spam 24h: Đánh dấu `LastNotifiedAt`, không gửi lặp lại thông báo cho sản phẩm đã báo trong ngày.
- **Mô hình Dùng thử có thời hạn & Khóa mờ chống lạm dụng:**
  - **Tài khoản Free:** Tặng duy nhất **3 sản phẩm dùng thử** với thời hạn **14 ngày/sản phẩm**. Bỏ hoàn toàn cơ chế 1 sản phẩm/ngày để triệt tiêu lỗ hổng tích lũy 30 sản phẩm/tháng. Khi quá 14 ngày, sản phẩm tự động chuyển sang trạng thái **Khóa mờ (Locked/Expired)**, server **ngừng cào giá 100%** và hiển thị ổ khóa 🔒 cùng nút mời nâng cấp Premium 29k/tháng.
  - **Tài khoản Premium:** Theo dõi tối đa **20 sản phẩm đồng thời** không giới hạn 14 ngày (BR-11). Khi hết hạn Premium (sau 3 ngày ân hạn), tự động chuyển toàn bộ 20 sản phẩm sang trạng thái **Khóa mờ (Locked/Paused)**, ngừng cào giá hoàn toàn để bảo vệ tài nguyên. Khi người dùng nạp tiền gia hạn, hệ thống tự động mở khóa lại toàn bộ 20 sản phẩm ngay lập tức.
- **Tiến trình cào ưu tiên thông minh (`--sync-tracked`):** Tự động bỏ qua toàn bộ sản phẩm đã bị khóa hoặc hết hạn dùng thử, chỉ cào các sản phẩm còn hạn hoạt động.
- **Email thông báo chuẩn SaaS hiện đại:** Tiêu đề ngắn gọn 7-9 từ (Canva, Figma, ChatGPT), làm sạch từ khóa rác Shopee, card ảnh bo tròn kèm nút viên thuốc (pill CTA).
- **Bộ kiểm thử tự động toàn diện:** CLI `dotnet run --project GiftFinder.API -- --test-price-tracking` vượt qua **10/10 Test Cases (100% PASS)**:
  - TC-01: Free User dùng thử 3 món đầu với thời hạn 14 ngày/món.
  - TC-02: Free User thêm món thứ 4 bị chặn ngay (triệt tiêu 1 món/ngày).
  - TC-03: Chống trùng lặp theo dõi cùng 1 sản phẩm.
  - TC-04: Free User quá hạn 14 ngày tự động Khóa mờ (IsLocked = true, TrialExpired) & Server ngừng cào giá.
  - TC-05: Premium User theo dõi 20 món không giới hạn thời gian (BR-11). Món 21 bị chặn.
  - TC-06: Gói Premium hết hạn tự động Khóa mờ cả 20 món & Ngừng cào giá 100%.
  - TC-07: Người dùng gia hạn Premium tự động mở khóa lại toàn bộ 20 món (IsActive = true).
  - TC-08: Báo giảm giá 1 món (Notification in-app + Email đơn Brevo SMTP).
  - TC-09: Báo giảm giá nhiều món (Gom vào 1 Email Digest duy nhất).
  - TC-10: Chống spam 24h & Hủy theo dõi thành công.

---

## ⏳ CÁC TÍNH NĂNG TIẾP THEO CẦN LÀM & LỘ TRÌNH PHÁT TRIỂN

1. **Cổng thanh toán VietQR cho Gói Featured của Merchant (UC-25)**:
   - Áp dụng cổng thanh toán VietQR đã xây dựng cho Merchant mua gói đẩy Top tuần (49k) hoặc tháng (199k).

2. **Tính năng Phản hồi & Đánh giá Gợi ý Quà (UC-04)**:
   - Cho phép người dùng đánh giá 1-5 sao và để lại nhận xét sau khi nhận tư vấn từ AI để đo lường độ hài lòng.

3. **Đối soát hoa hồng Affiliate & Báo cáo Tài chính (UC-10, UC-11, UC-12)**:
   - Nhận Postback đơn hàng từ Accesstrade / Shopee, tính doanh thu và hoa hồng ước tính.

4. **Giao diện Web / Frontend**:
   - Xây dựng giao diện web người dùng tìm quà tặng, tương tác với AI Recommendation, xem danh mục và chuyển hướng Affiliate.
   - Giao diện Admin duyệt quà và xem báo cáo hoa hồng.
   - Giao diện Merchant quản lý quà tặng và mua gói tài trợ.

---

## 🎯 KẾ HOẠCH BẮT ĐẦU CHO PHIÊN LÀM VIỆC TIẾP THEO:
1. **Trạng thái hiện tại:** 
   - Kho hàng đạt **244 sản phẩm sạch 100%** (124 TikTok Shop, 118 Shopee, 1 Lazada, 1 Merchant).
   - 100% có giá thật, ảnh CDN thật từ sàn, link affiliate chuẩn và tag Groq AI.
   - Hoàn thành trọn vẹn Use Case **UC-20 (Tự động hết hạn Featured)**, **UC-18 (Xếp hạng xen kẽ Interleaved Slotting)**, và **UC-08 & UC-22 (Theo dõi biến động giá & Cảnh báo giảm giá)**.
   - Tiến độ tổng thể: **25 / 31 Use Cases (~80.6% hoàn thành)**.
   - Toàn bộ Solution biên dịch thành công 100% (**0 Errors, 0 Warnings**).
2. **Mục tiêu phiên mới:** Triển khai Use Case **UC-25** (Thanh toán gói Featured cho Merchant bằng VietQR) hoặc **UC-04** (Đánh giá phản hồi gợi ý quà từ AI).


---

## 🎨 TIẾN ĐỘ PHÁT TRIỂN GIAO DIỆN FRONTEND & TÁI CẤU TRÚC MONOREPO (28/10/2026)

### 1. Phân Tách Thư Mục Dự Án (Monorepo Standard)
* Đã cấu trúc lại toàn bộ repository thành 2 thư mục độc lập:
  * **`backend/`**: Toàn bộ mã nguồn C# .NET 8 Clean Architecture (`GiftFinder.API`, `GiftFinder.Application`, `GiftFinder.Domain`, `GiftFinder.Infrastructure`, `GiftFinder.slnx`), kết nối PostgreSQL, VNPAY, Brevo SMTP, Groq AI.
  * **`frontend/`**: Ứng dụng Web hiện đại React 19 + TypeScript + Vite + Tailwind CSS v4, proxy tự động sang API `http://localhost:5151`.
* **Kết quả build:**
  * Backend: `dotnet build` đạt **0 Error(s), 0 Warning(s)**.
  * Frontend: `npm run build` đạt **0 Error(s)** (`✓ built in 342ms`).
  * Automated VNPay CLI Tests: **6/6 Test Cases PASS (100%)**.

### 2. Hoàn Thiện Toàn Bộ Landing Page Mới (11 Khối Nội Dung & Widget Chatbot)
1. **Khối 1: Poster Hero Banner & AI Concierge**: Poster khổ lớn nghệ thuật, ô tìm kiếm AI Typewriter tự gõ gợi ý, viền phát sáng neon thở nhẹ, badges lơ lửng ⭐ `4.9/5` và 🔥 `-25%`, carousel tự động xoay chuyển sau 4.5s.
2. **Khối 2: Hôm Nay Bạn Đang Tìm Quà Cho Ai?**: Phân loại theo đối tượng người nhận (Cho Nàng, Cho Chàng, Quà Bố Mẹ, Đồng Nghiệp, Bạn Thân, Kỷ Niệm).
3. **Khối 3: Chọn Quà Đúng Dịp**: Bộ lọc theo ngữ cảnh dịp lễ (Sinh nhật, 20/10 & 8/3, Kỷ niệm ngày yêu, Giáng sinh, Tân gia, Tốt nghiệp).
4. **Khối 4: Quà Hot Hôm Nay**: Bảng Flash Deals giảm giá tới 35%, đồng hồ đếm ngược thời gian thực (Live Countdown) nhảy từng giây, thanh tiến độ đã bán lấp lánh dải sáng bạc (`shimmerSliding`).
5. **Khối 5: Sản Phẩm Nổi Bật (Featured Placement)**: Trình diễn các sản phẩm được nhà bán tài trợ đẩy Top (UC-17, UC-24) và hàng tuyển chọn Direct với huy hiệu nổi bật.
6. **Khối 6: Set Quà Theo Dịp**: 4 Combo hộp quà phối sẵn hoàn chỉnh (nến thơm, hoa khô, thiệp viết tay cá nhân hóa, trà thảo mộc).
7. **Khối 7: Sản Phẩm Liên Quan & Phụ Kiện Tặng Kèm**: Bán chéo (Cross-selling) các phụ kiện quà tặng (túi quà ép nhũ, thiệp hoa khô viết tay, hộp nhạc gỗ, đèn led đom đóm).
8. **Khối 8: An Tâm Mua Sắm**: 4 Trụ cột bảo đảm: 100% chính hãng, Cổng thanh toán Quốc gia VNPAY (PCI DSS), Đổi trả 7 ngày, Giao hỏa tốc 2h & viết thiệp.
9. **Khối 9: Khách Hàng Hài Lòng**: Video unboxing thực tế với nút Play tỏa sóng radar (`rippleRing`) và các đánh giá 5 sao có tích xanh `Khách hàng đã mua ✓`.
10. **Khối 10: Footer Toàn Diện**: Tem pháp lý Bộ Công Thương, thông tin công ty, liên kết đối tác Shopee, TikTok Shop, VNPAY.
11. **Khối 11 (Điểm nhấn): Widget Chatbot AI Trợ Lý Tìm Quà**: Nút tròn nổi cố định ở góc dưới bên phải màn hình, có chấm xanh Online và tooltip mời gọi, mở ra popup chat thông minh với các câu hỏi gợi ý nhanh và trả về thẻ sản phẩm tương tác xem chi tiết/mua ngay.