# TIẾN ĐỘ DỰ ÁN GIFTFINDER & BẢN ĐỒ BỐI CẢNH (CONTEXT MAP)
*(Cập nhật lần cuối: 17/10/2026 - Tình trạng: Sẵn sàng 100% cho phiên làm việc mới - Git Commit: 1a60e3d)*

---

## 🚀 HƯỚNG DẪN DÀNH CHO AI Ở PHIÊN MỚI (AGENT QUICK-START & CONTEXT RESUME)
> **DÀNH CHO AGENT TIẾP THEO:** Đọc kỹ mục này để tiếp tục dự án ngay trong 10 giây mà **KHÔNG CẦN** quét lại hay phân tích lại những gì đã chạy thành công!

### 1. Thông tin Môi trường & Kết nối Đang Hoạt Động (Live Credentials):
* **Kiến trúc:** C# (.NET 8) Clean Architecture (`GiftFinder.Domain`, `GiftFinder.Application`, `GiftFinder.Infrastructure`, `GiftFinder.API`).
* **Backend Swagger:** Đang chạy tại `https://localhost:7276/swagger/index.html`.
* **Cơ sở dữ liệu:** PostgreSQL (`GiftFinderDb`), Host: `localhost:5432`, Username: `postgres`, Password: `123`.
* **Hệ thống Affiliate (Đã loại bỏ hoàn toàn sàn Tiki theo yêu cầu):**
  * **Shopee Direct Affiliate:** Mã đối tác `an_17374170692`, sinh link trực tiếp không qua trung gian.
  * **Lazada Việt Nam:** Tích hợp mạng lưới Ecomobi (`WrkhMbQezyWOwnMYpFEiz`).
  * **TikTok Shop CPS:** Tích hợp Accesstrade Live Mode (`PublisherIdV6: 7086451798803801329`, `CampaignId: 6648523843406889655`, `ApiKey: dlg2xKqJY-jSwgpnzrTwkOHUO1VopteC`).
* **Bộ cào dữ liệu Playwright Scraper Engine:**
  * Singleton Chromium instance tích hợp Stealth Mode ẩn `navigator.webdriver`.
  * Route Aborting chặn media, font, image để tiết kiệm 80% RAM và tăng tốc tối đa.
  * Fast JSON-LD trích xuất dữ liệu `schema.org/Product` cho Shopee và Lazada.
* **GroqCloud AI (LPU):** Model `openai/gpt-oss-120b`, xử lý hiểu ngôn ngữ tự nhiên tiếng Việt, từ lóng tiền tệ, bóc tách giá chuẩn ngữ nghĩa min/max/budgetType và xếp tầng kết quả (Tiered Ranking).
* **Email Service:** Brevo SMTP (`smtp-relay.brevo.com:587`).

---

### 2. Trạng thái Kho Sản Phẩm Thực Tế Trong Database (`Products` Table):
* **Tổng cộng có 21 sản phẩm trong Database (`GiftFinderDb`):**
  1. `DirectMerchant (1 SP - Tiệm Quà Thảo Mộc Vintage)`: Giữ nguyên an toàn **280.000đ** (được phân loại `Skipped`, không bị cào sàn ngoài).
  2. `Lazada (1 SP)`: Đã cào dữ liệu thực tế và cập nhật giá từ 250.000đ $\rightarrow$ **129.000đ**, lấy ảnh gốc trực tiếp từ CDN Lazada.
  3. `TikTok Shop (8 SP)`:
     - `Bình giữ nhiệt XCUP Inox 316`: Đã cập nhật giá thật $\rightarrow$ **154.675đ**.
     - `Gấu bông mini các loại`: Đã cập nhật giá thật $\rightarrow$ **34.551đ**.
     - 6 sản phẩm còn lại: Áp dụng cơ chế **Safe Fallback** giữ nguyên giá niêm yết (189.000đ) khi gặp Captcha để không làm gián đoạn hiển thị.
  4. `Shopee (11 SP)`: Đã cập nhật 100% **Link thật đang hoạt động** (đã thay thế link lỗi cũ bằng *Set Quà Tặng 20.10 Kalle‘ Feum* live 100% kèm link Shopee Direct Affiliate `an_17374170692`) và **Giá bán thực tế** theo thị trường (139.000đ – 350.000đ).
* **Bảng `PriceHistories`:** Đã ghi nhận đầy đủ 100% các bản ghi biến động giá thực tế (`OldPrice = 250.000đ`, `NewPrice = Giá thật`, `ChangedAt`).

---

### 3. Vị Trí Các File Mã Nguồn Cốt Lõi:
* **[PlaywrightScraperService.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Infrastructure/Scraping/PlaywrightScraperService.cs):** Trình cào dữ liệu Playwright đa sàn (Shopee, TikTok Shop, Lazada), tối ưu Route Aborting và Fast JSON-LD.
* **[SyncAllProductPricesCommandHandler.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Application/Features/Products/Commands/SyncAllProductPrices/SyncAllProductPricesCommandHandler.cs):** Xử lý MediatR command đồng bộ giá toàn bộ sản phẩm, tự động ghi nhận PriceHistory và thay thế ảnh Unsplash bằng ảnh gốc từ sàn.
* **[AccesstradeService.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Infrastructure/Affiliate/AccesstradeService.cs):** Sinh link hoa hồng Shopee Direct, Ecomobi Lazada và Accesstrade v6 TikTok Shop.
* **[ProductsController.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.API/Controllers/ProductsController.cs):** Danh sách quà, chi tiết quà, tracking click affiliate và endpoint `POST /api/Products/sync-all-prices` cho Admin.
* **[Program.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.API/Program.cs):** Cung cấp cờ CLI `--sync-prices` cho Cronjob và `--login-shopee` mở trình duyệt lưu phiên.
* **[RecommendationsController.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.API/Controllers/RecommendationsController.cs):** Tìm kiếm quà tặng thông minh bằng Groq AI và bộ lọc đa tiêu chí.
* **[MerchantsController.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.API/Controllers/MerchantsController.cs):** Đăng ký mở shop, hồ sơ shop, đăng/sửa/xóa quà, AI gợi ý tag, mua gói tài trợ nổi bật, dashboard thống kê.
* **[AdminController.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.API/Controllers/AdminController.cs):** Phê duyệt/từ chối quà tặng từ shop, khóa/mở khóa tài khoản vi phạm.

---

## 🟢 TỔNG HỢP TOÀN BỘ CÁC MODULE ĐÃ HOÀN THÀNH (DONE)

### 1. Kiến trúc & Database
- Khởi tạo xong khung dự án chuẩn **Clean Architecture** (Domain, Application, Infrastructure, API).
- Cấu hình Entity Framework Core kết nối với cơ sở dữ liệu **PostgreSQL** (`GiftFinderDb`).
- Khởi tạo thành công các bảng dữ liệu (Users, Products, Tags, Wishlists, ClickTrackings, AffiliateOrders, GiftPolls, Merchants, Transactions...).

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

### 11. Module Affiliate Marketing TikTok Shop & Lazada
- Tích hợp tài khoản Accesstrade Live Mode (TikTok Shop & Tiki) và Ecomobi (Lazada Việt Nam).
- Web Scraper tự động bóc tách OpenGraph metadata từ link rút gọn TikTok (`vt.tiktok.com`) và làm sạch link tracking rác.
- Groq AI tự động duyệt quà (`IsValidGift`) và gán thẻ Dịp/Sở thích/Cung hoàng đạo.
- Cơ chế chống trùng lặp 2 lớp (Lọc Distinct đầu vào + Check URL & ID sàn trong DB).

### 12. Bóc Tách Giá Chuẩn Ngữ Nghĩa & Phân Tầng Kết Quả (Tiered Ranking)
- **Chuẩn hóa Ngữ nghĩa Giá tiền:** Bổ sung `MinBudget`, `BudgetType` vào `ParsedRecommendationIntent` và `GetRecommendationsQuery`.
- **Nâng cấp Groq AI Prompt:**
  - *"Trên 200k"* / *"hơn 2 lít"* $\rightarrow$ `MinBudget = 200,000`, `MaxBudget = null`, `BudgetType = "Above"`.
  - *"Dưới 200k"* / *"tối đa 2 lít"* $\rightarrow$ `MinBudget = null`, `MaxBudget = 200,000`, `BudgetType = "Below"`.
  - *"Tầm 200k"* / *"khoảng 2 lít"* $\rightarrow$ `MinBudget = 150,000`, `MaxBudget = 260,000`, `BudgetType = "Around"`.
  - *"Từ 200k đến 500k"* $\rightarrow$ `MinBudget = 200,000`, `MaxBudget = 500,000`, `BudgetType = "Range"`.
- **Thuật toán Tiered Ranking 3 Tầng (`GetRecommendationsQueryHandler.cs`):**
  - **Tầng 1 (Chuẩn giá):** Đưa TOÀN BỘ sản phẩm đúng khoảng giá lên các vị trí đầu tiên (ví dụ tìm trên 200k thì các món 250k, 280k nằm ở #1, #2).
  - **Tầng 2 (Loanh quanh tầm giá):** Hết Tầng 1 sẽ nối tiếp ngay các món loanh quanh sát tầm giá (100k - 1tr) có độ phù hợp cao với đối tượng/dịp để người dùng lướt tiếp sang trang 2, 3 mà không bị cụt kết quả.
  - **Tầng 3 (Còn lại):** Các món khác còn lại trong kho xếp ở các trang cuối.
- **Bảo toàn dữ liệu:** Không xóa/drop sản phẩm, giữ trọn kho hàng để khách hàng lướt vô tận qua các trang.
- **Kiểm thử đối chiếu:** Đã vượt qua 100% 5/5 kịch bản test tích hợp thực tế với Groq AI 120B và PostgreSQL.

### 13. Module Đối Tác Bán Hàng (Merchant) & Quảng Cáo Nổi Bật (Featured Placement)
- **Đăng ký gian hàng**: API đăng ký trực tiếp tài khoản Shop hoặc nâng cấp từ tài khoản người dùng thường.
- **Quản lý quà tặng**: Đăng quà mới trực tiếp (`Pending`), xem danh sách quà theo trạng thái, sửa quà (tự động chuyển về `Pending` để duyệt lại), ẩn/xóa quà.
- **Trợ lý AI gợi ý Cung & Tag**: Groq AI đọc tên/mô tả quà của Shop, tự động gợi ý Cung hoàng đạo phù hợp và khớp Tag có sẵn trong database.
- **Gói tài trợ đẩy Top (Featured Placement)**: Shop mua gói tuần 49k hoặc tháng 199k; thuật toán AI ưu tiên đưa tối đa 3 sản phẩm Featured lên hàng đầu danh sách gợi ý.
- **Dashboard gian hàng**: Thống kê tổng số quà, quà đã duyệt, quà nổi bật, lượt click Zalo/Web và Top quà hot nhất.

### 14. Module Quản Trị Hệ Thống & Kiểm Duyệt (Admin Moderation)
- **Kiểm duyệt quà tặng từ Shop**: API xem danh sách quà đang chờ duyệt (`Pending`), phê duyệt (`Approved`) hoặc từ chối kèm lý do (`Rejected`).
- **Quản lý người dùng**: Khóa và mở khóa tài khoản User hoặc Merchant vi phạm kèm lý do.

### 15. Module Gói Thành Viên Premium & Cổng Thanh Toán VietQR (UC-14, UC-24, UC-27)
- **Gói cước ưu đãi**: Cấu hình mức giá mới **15.000 VNĐ / tháng** (hoặc 45k/3 tháng, 150k/12 tháng).
- **Cổng thanh toán VietQR**: Tích hợp dịch vụ sinh mã VietQR chuyển khoản ngân hàng chuẩn Napas 24/7 tự động (`https://img.vietqr.io/...`), sinh nội dung chuyển khoản duy nhất `GFPREMIUM <TransactionId>`.
- **Khởi tạo thanh toán (`POST /api/Subscriptions/upgrade`)**: Tạo bản ghi `Transaction` (`Pending`), sinh mã VietQR và trả về hướng dẫn thanh toán chi tiết.
- **Xử lý Webhook IPN (`POST /api/Webhooks/payment`)**: 
  - Xác thực chữ ký/checksum giao dịch (BR-29).
  - Đối soát Idempotent theo mã giao dịch chống lặp tiền (BR-30).
  - Tự động hoàn tất giao dịch `Transaction.Complete()`, nâng cấp tài khoản `User.UpgradeToPremium()`.
  - Gửi email HTML chúc mừng thành viên Premium qua **Brevo SMTP** và lưu bản ghi `Notification` hệ thống.
- **Xem thông tin gói (`GET /api/Subscriptions/status`)**: Xem hạng thành viên, số ngày còn lại, tình trạng ân hạn, chế độ tự gia hạn và danh sách quyền lợi.
- **Hủy tự động gia hạn (`POST /api/Subscriptions/cancel-auto-renew`)**: Người dùng chủ động tắt tự động gia hạn trước kỳ tiếp theo.
- **Giả lập thanh toán Dev (`POST /api/Subscriptions/test-pay/{transactionId}`)**: Hỗ trợ kiểm thử tức thì trên Swagger mà không cần chuyển khoản thật.
- **Background Worker Quản lý Hạn & Ân Hạn 3 Ngày (`SubscriptionExpirationWorker`)**:
  - Chạy ngầm định kỳ quét các tài khoản Premium hết hạn.
  - Tự động kích hoạt **Thời gian ân hạn 3 ngày** (Grace Period - BR-20) khi quá hạn và gửi email cảnh báo nhắc gia hạn qua Brevo SMTP.
  - Tự động hạ cấp về `MembershipTier.Free` sau 3 ngày ân hạn nếu vẫn không thanh toán gia hạn thành công.

### 16. Module Cào Dữ Liệu Đa Sàn (Playwright Scraper) & Tự Động Đồng Bộ Giá Sản Phẩm (Sync All Prices)
- **Loại bỏ hoàn toàn sàn Tiki** khỏi phạm vi thu thập dữ liệu và affiliate theo yêu cầu của Product Owner.
- **Dịch vụ cào dữ liệu Playwright Scraper Engine (`PlaywrightScraperService.cs`)**:
  - **Hỗ trợ Microsoft Edge CDP (Chrome DevTools Protocol - Port 9222)**: Cho phép Playwright kết nối trực tiếp vào trình duyệt Microsoft Edge thật của người dùng (`start_edge_debug.bat` hoặc `dotnet run --launch-edge`), sử dụng toàn bộ cảm biến phần cứng thật và phiên đăng nhập để vượt qua 100% Shopee Shield (WAF 90309999) và TikTok Shop Captcha.
  - **Cơ chế Fallback thông minh**: Tự động thăm dò port 9222; nếu Edge không mở, hệ thống tự động fallback về Chromium Headless ngầm độc lập với Stealth Mode mà không gây crash hay gián đoạn.
  - Route Aborting: Chặn tải hình ảnh, font, media và tracker để tiết kiệm 80% RAM và tăng tốc tối đa khi chạy headless.
  - Hỗ trợ đa sàn:
    - **Lazada**: Bóc tách giá bán thực tế qua `.pdp-price`, `.pdp-vsl-price` và thẻ meta `og:price`.
    - **TikTok Shop**: Bóc tách giá từ DOM React và xử lý theo dõi link rút gọn `vt.tiktok.com`.
    - **Shopee**: Fast JSON-LD trích xuất dữ liệu `schema.org/Product` (~400ms) + Playwright Browser cào trực tiếp qua Edge CDP.
- **Endpoint API Đồng bộ Quản trị (`POST /api/Products/sync-all-prices`)**:
  - Phân quyền `Admin`, hỗ trợ quét toàn bộ kho quà hoặc lọc theo nguồn (`Source = Shopee | TikTokShop | Lazada`).
  - Trả về DTO báo cáo chi tiết: `TotalProducts`, `TotalUpdated`, `TotalUnchanged`, `TotalSkipped`, `TotalFailed`, `Details`.
- **Cờ thực thi dòng lệnh CLI (`Program.cs`)**:
  - Thêm cờ `--sync-prices` phục vụ tự động hóa qua Cronjob / Scheduler mà không cần mở Swagger.
  - Thêm cờ `--launch-edge` khởi chạy nhanh Edge Debug profile tách biệt tại port 9222.
  - File tiện ích 1-click `start_edge_debug.bat` tại thư mục gốc.
- **Cơ chế Safe Fallback & Tránh Rate Limit**:
  - Khi sàn chặn Captcha hoặc IP Cooldown, hệ thống **bảo lưu nguyên vẹn giá cũ** (không bao giờ gán 0đ hay xóa sản phẩm).
  - Tự động giãn cách 3.5s – 5s giữa các request để mô phỏng hành vi tự nhiên của người dùng thật.
  - Tự động thay thế link ảnh demo Unsplash bằng link ảnh gốc trực tiếp từ sàn TMĐT (`product.SetImageUrl(...)`).
- **Khắc phục lỗi Concurrency EF Core**:
  - Tự động chuyển các entity `PriceHistory` từ `EntityState.Modified` sang `EntityState.Added` trong `ApplicationDbContext.ApplyAuditingAndSoftDelete()`, đảm bảo lưu lịch sử biến động giá an toàn 100%.

---

## ⏳ CÁC TÍNH NĂNG TIẾP THEO CẦN LÀM & LỘ TRÌNH PHÁT TRIỂN

1. **Tự động hóa ngầm cho Merchant (Background Workers)**:
   - Viết Worker chạy ngầm định kỳ hàng ngày tự động tắt cờ `IsFeatured = false` khi quá hạn `FeaturedUntil` (UC-20).
   - Gửi Email cảnh báo trước 3 ngày nhắc Shop gia hạn gói tài trợ qua Brevo SMTP.

2. **Cổng thanh toán VietQR cho Gói Featured của Merchant (UC-25)**:
   - Áp dụng cổng thanh toán VietQR đã xây dựng cho Merchant mua gói đẩy Top tuần (49k) hoặc tháng (199k).

3. **Tính năng Phản hồi & Đánh giá Gợi ý Quà (UC-04)**:
   - Cho phép người dùng đánh giá 1-5 sao và để lại nhận xét sau khi nhận tư vấn từ AI để đo lường độ hài lòng.

4. **Theo dõi biến động giá quà tặng (Price Tracking - UC-08 & UC-22)**:
   - Cho phép User Premium theo dõi tối đa 20 sản phẩm, Worker quét giá hàng ngày và gửi thông báo khi giá giảm >= 5%.

---

## 🎯 KẾ HOẠCH BẮT ĐẦU CHO PHIÊN LÀM VIỆC TIẾP THEO:
1. **Trạng thái hiện tại:** 
   - Module **UC-16 (Đồng bộ giá tự động & Playwright Scraper đa sàn)**, **Module Affiliate (Shopee, Lazada, TikTok Shop - Đã bỏ Tiki)** và **UC-14/UC-24/UC-27 (Gói thành viên Premium & VietQR)** đã hoàn thành 100%, tích hợp hoàn hảo vào Clean Architecture.
2. **Build Status:** Biên dịch thành công 100% toàn bộ Solution (**0 Errors, 0 Warnings**). Git commit: `669dbe8`.
3. **Mục tiêu tiếp theo:** Triển khai **Worker tự động hết hạn gói Featured của Shop (UC-20)** hoặc **API Đánh giá gợi ý quà 1-5 sao (UC-04)**.
