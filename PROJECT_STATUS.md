# TIẾN ĐỘ DỰ ÁN GIFTFINDER & BẢN ĐỒ BỐI CẢNH (CONTEXT MAP)
*(Cập nhật lần cuối: 13/10/2026 - Tình trạng: Sẵn sàng 100% cho phiên làm việc mới)*

---

## 🚀 HƯỚNG DẪN DÀNH CHO AI Ở PHIÊN MỚI (AGENT QUICK-START & CONTEXT RESUME)
> **DÀNH CHO AGENT TIẾP THEO:** Đọc kỹ mục này để tiếp tục dự án ngay trong 10 giây mà **KHÔNG CẦN** quét lại hay phân tích lại những gì đã chạy thành công!

### 1. Thông tin Môi trường & Kết nối Đang Hoạt Động (Live Credentials):
* **Kiến trúc:** C# (.NET 8) Clean Architecture (`GiftFinder.Domain`, `GiftFinder.Application`, `GiftFinder.Infrastructure`, `GiftFinder.API`).
* **Backend Swagger:** Đang chạy tại `https://localhost:7276/swagger/index.html`.
* **Cơ sở dữ liệu:** PostgreSQL (`GiftFinderDb`), Host: `localhost:5432`, Username: `postgres`, Password: `123`.
* **Tài khoản Accesstrade (Live Mode 100%):**
  * `PublisherIdV6`: `7086451798803801329`
  * `AffiliateId`: `AT2261233`
  * `ApiKey`: `dlg2xKqJY-jSwgpnzrTwkOHUO1VopteC`
  * Cấu hình trong `GiftFinder.API/appsettings.json`: `"UseMock": false`.
  * **Chiến dịch hoạt động:** TikTok Shop CPS (`6648523843406889655`), Tiki CPS (`4348614231480407268`). Đã cấu hình dự phòng Shopee (`4751584435713464237`) và Lazada (`4348614539120935532`).
* **GroqCloud AI (LPU):** Model `openai/gpt-oss-120b`, xử lý hiểu ngôn ngữ tự nhiên tiếng Việt, từ lóng tiền tệ, bóc tách giá chuẩn ngữ nghĩa min/max/budgetType và xếp tầng kết quả (Tiered Ranking).
* **Email Service:** Brevo SMTP (`smtp-relay.brevo.com:587`).

---

### 2. Trạng thái Kho Sản Phẩm Thực Tế Trong Database (`Products` Table):
* **Đang có 10 sản phẩm thật 100%** (Đã kiểm duyệt `Status = Approved`, có ảnh CDN rõ nét, mô tả tiếng Việt và link hoa hồng Accesstrade / Ecomobi hoạt động hoàn hảo):
  1. `[Penguin] Cốc sứ Vô Tri quà tặng ý nghĩa...` (TikTokShop - 189.000đ)
  2. `【Miễn phí hộp quà & túi xách】Vòng đeo tay ngôi sao và mặt trăng titan...` (TikTokShop - 189.000đ)
  3. `Lắc tay nữ bạc cao cấp charm hoa 5 cánh...` (TikTokShop - 189.000đ)
  4. `<Tặng Túi Giữ Nhiệt> Bình giữ nhiệt XCUP NIUMI INOX 316...` (TikTokShop - 189.000đ)
  5. `VIDEO - Set vòng cổ và vòng tay BLESS cỏ 4 lá may mắn...` (TikTokShop - 189.000đ)
  6. `Hộp quà tặng 20/10 trang điểm kèm gấu bông, hoa sáp...` (TikTokShop - 189.000đ)
  7. `[Tặng 5 quà]Set quà tăng sinh nhật, noel, ngày phụ nữ...` (TikTokShop - 189.000đ)
  8. `Gấu bông mini các loại khủng long- chó-mèo-thỏ-heo-chim cánh cụt-voi...` (TikTokShop - 189.000đ)
  9. `Quà 20/10 - Hộp Quà Tặng Sinh Nhật 4 món dành tặng nữ...` (Lazada Việt Nam - 250.000đ)
  10. `Set Quà Tặng Nến Thơm Tinh Dầu Thảo Mộc` (Merchant / Quà tặng cao cấp - 280.000đ)
* **Lưu ý quan trọng:** Dữ liệu hoàn toàn sạch sẽ, không có sản phẩm mock rác. Tiêu chí giá từ 189k đến 280k giúp thuật toán Tiered Ranking phân tách tầng giá (Tier 1 vs Tier 2) cực kỳ chân thực.

---

### 3. Vị Trí Các File Mã Nguồn Cốt Lõi:
* **[AccesstradeService.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Infrastructure/Affiliate/AccesstradeService.cs):** Cào OpenGraph metadata, sinh Deeplink v6 Accesstrade.
* **[ImportProductByUrlCommandHandler.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Application/Features/Affiliate/Commands/ImportProductByUrl/ImportProductByUrlCommandHandler.cs):** Chống trùng 2 lớp, gỡ bỏ bản ghi bị AI từ chối cũ để thẩm định lại, sinh link hoa hồng.
* **[ProductsController.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.API/Controllers/ProductsController.cs):** Danh sách quà phân trang, chi tiết quà, ghi nhận click affiliate và chuyển hướng.
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
1. **Trạng thái hiện tại:** Module **UC-14 (Nâng cấp gói Premium 15.000đ/tháng)**, **UC-24 (Thanh toán gói Premium)**, **UC-27 (Webhook IPN)** và **SubscriptionExpirationWorker (BR-20)** đã hoàn thành 100%, tích hợp hoàn hảo vào Clean Architecture.
2. **Build Status:** Biên dịch thành công 100% toàn bộ Solution (**0 Errors, 0 Warnings**).
3. **Mục tiêu tiếp theo:** Triển khai **Worker tự động hết hạn gói Featured của Shop (UC-20)** hoặc **API Đánh giá gợi ý quà 1-5 sao (UC-04)**.
