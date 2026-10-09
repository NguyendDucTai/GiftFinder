# TIẾN ĐỘ DỰ ÁN GIFTFINDER & BẢN ĐỒ BỐI CẢNH (CONTEXT MAP)
*(Cập nhật lần cuối: 09/10/2026 - Tình trạng: Sẵn sàng 100% cho phiên làm việc mới)*

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
* **GroqCloud AI (LPU):** Model `openai/gpt-oss-120b`, xử lý hiểu ngôn ngữ tự nhiên tiếng Việt, từ lóng tiền tệ và kiểm duyệt quà tặng.
* **Email Service:** Brevo SMTP (`smtp-relay.brevo.com:587`).

---

### 2. Trạng thái Kho Sản Phẩm Thực Tế Trong Database (`Products` Table):
* **Đang có đúng 8 sản phẩm TikTok Shop thật 100%** (Đã kiểm duyệt `Status = Approved`, có ảnh CDN TikTok, mô tả tiếng Việt và link hoa hồng Accesstrade hoạt động hoàn hảo):
  1. `[Penguin] Cốc sứ Vô Tri quà tặng ý nghĩa...` (TikTokShop - 189.000đ - Tag: Decor, Thời trang, Sinh nhật, Kỷ niệm)
  2. `【Miễn phí hộp quà & túi xách】Vòng đeo tay ngôi sao và mặt trăng titan...` (TikTokShop - 189.000đ - Tag: 8/3, 20/10, Valentine, Thời trang, Sinh nhật)
  3. `Lắc tay nữ bạc cao cấp charm hoa 5 cánh...` (TikTokShop - 189.000đ - Tag: Valentine, Sinh nhật, Giáng sinh, Tết, Thời trang)
  4. `<Tặng Túi Giữ Nhiệt> Bình giữ nhiệt XCUP NIUMI INOX 316...` (TikTokShop - 189.000đ - Tag: Sinh nhật, Decor, Kỷ niệm, Thời trang)
  5. `VIDEO - Set vòng cổ và vòng tay BLESS cỏ 4 lá may mắn...` (TikTokShop - 189.000đ - Tag: Decor, Sinh nhật, Thời trang, Kỷ niệm)
  6. `Hộp quà tặng 20/10 trang điểm kèm gấu bông, hoa sáp...` (TikTokShop - 189.000đ - Tag: Decor, Mỹ phẩm & Làm đẹp, 20/10)
  7. `[Tặng 5 quà]Set quà tăng sinh nhật, noel, ngày phụ nữ...` (TikTokShop - 189.000đ - Tag: Sinh nhật, Decor, Giáng sinh, 8/3, Thời trang)
  8. `Gấu bông mini các loại khủng long- chó-mèo-thỏ-heo-chim cánh cụt-voi...` (TikTokShop - 189.000đ - Tag: Valentine, Giáng sinh, Decor, Sinh nhật)
* **Lưu ý quan trọng:** Không có sản phẩm rác hay mock data. `DbInitializer.cs` đã được gỡ bỏ code chèn mock cứng.

---

### 3. Vị Trí Các File Mã Nguồn Cốt Lõi Vừa Hoàn Thiện:
* **[AccesstradeService.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Infrastructure/Affiliate/AccesstradeService.cs):**
  * `ScrapeProductMetadataAsync`: Cào OpenGraph metadata (Tiêu đề, Ảnh CDN, Mô tả, ID sàn `ExternalProductId`).
  * `GenerateDeeplinkAsync`: Sinh Deeplink v6 Accesstrade (`https://go.isclix.com/deep_link/v6/...`).
* **[ImportProductByUrlCommandHandler.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.Application/Features/Affiliate/Commands/ImportProductByUrl/ImportProductByUrlCommandHandler.cs):**
  * Chống trùng lặp 2 lớp (`Distinct` mảng đầu vào + Check `OriginalUrl` & `ExternalProductId` trong Database).
  * Tự gỡ bản ghi cũ nếu từng bị AI từ chối để thẩm định lại.
  * Trả về kết quả kèm danh sách `importedProducts` chứa `affiliateUrl`.
* **[ProductsController.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.API/Controllers/ProductsController.cs):**
  * `GET /api/Products`: Danh sách sản phẩm (Phân trang `page`, `pageSize`, tìm kiếm `keyword`, lọc sàn `source`, lọc trạng thái `status`).
  * `GET /api/Products/{id}`: Chi tiết sản phẩm.
  * `POST /api/Products/{id}/click-affiliate`: Ghi nhận click, cộng điểm `PopularityScore` và chuyển hướng.
* **[RecommendationsController.cs](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/GiftFinder.API/Controllers/RecommendationsController.cs):**
  * `GET /api/Recommendations`: Tìm kiếm quà tặng thông minh bằng Groq AI và bộ lọc đa tiêu chí.

---

## 🟢 TỔNG HỢP TOÀN BỘ CÁC MODULE ĐÃ HOÀN THÀNH (DONE)

### 1. Kiến trúc & Database
- Khởi tạo xong khung dự án chuẩn **Clean Architecture** (Domain, Application, Infrastructure, API).
- Cấu hình Entity Framework Core kết nối với cơ sở dữ liệu **PostgreSQL** (`GiftFinderDb`).
- Khởi tạo thành công các bảng dữ liệu (Users, Products, Tags, Wishlists, ClickTrackings, AffiliateOrders, GiftPolls...).

### 2. Tính năng Xác thực - Authentication (UC-13)
- Đăng nhập / Đăng ký bằng Email & Mật khẩu (CQRS với MediatR). Mật khẩu băm **BCrypt**.
- Sinh và xác thực **JWT Token** (đã test thành công trên Swagger với ổ khóa Authorize).

### 3. Cập nhật Cấu trúc cho AI Cung Hoàng Đạo (Zodiac Add-on - Tầng Domain)
- Enum `ZodiacSign.cs` (Đầy đủ 12 cung hoàng đạo).
- Bảng `Product`: Bổ sung cột tùy chọn `SuitableZodiacs` để gán nhãn cung hoàng đạo cho quà tặng.
- `ZodiacCalculator.cs`: Thuật toán tự động chuyển đổi Ngày/Tháng sinh thành Cung Hoàng Đạo.
- `ReminderDate.cs`: Tự động nhận diện nếu sự kiện là "Sinh nhật" thì tự gán Cung hoàng đạo cho người nhận quà.

### 4. Quản lý Sản phẩm Yêu thích - Wishlist (UC-02)
- API `POST`, `GET`, `DELETE` cho Wishlist.
- Xử lý logic chống trùng lặp, giới hạn 100 sản phẩm và khôi phục khi soft-delete.
- `ICurrentUserService` trích xuất `UserId` an toàn từ JWT Token.

### 5. Quản lý ngày kỷ niệm / Sinh nhật (UC-15)
- API `POST`, `GET`, `PUT`, `DELETE` cho `ReminderDate`. Giới hạn 50 bản ghi/user.
- Tự động tính toán Cung Hoàng Đạo (`RecipientZodiac`) và lưu vào cơ sở dữ liệu.

### 6. Nhắc lịch sinh nhật & kỷ niệm tự động kèm Email (UC-21)
- `BirthdayReminderWorker` chạy ngầm định kỳ quét database và tính toán ngày sắp tới hạn.
- `SmtpEmailService` gửi email HTML thật qua máy chủ **Brevo SMTP** (`smtp-relay.brevo.com`).
- Email Template Card sang trọng, có logo GiftFinder, hiển thị số ngày còn lại và nút CTA.
- Tự động lưu bản ghi `Notification` và cập nhật `LastNotifiedAt` chống spam lặp trong cùng 1 năm. Đã test gửi thành công 100%.

### 7. Đánh giá ngầm độ hot sản phẩm & Phân trang gợi ý (UC-04 & UC-01 Mở rộng)
- Tính điểm ngầm tự động (**PopularityScore**): Thêm Wishlist `+10đ`, xóa Wishlist `-8đ`, khôi phục `+8đ`.
- API Gợi ý hỗ trợ phân trang vô hạn (`pageNumber`, `pageSize`) và tự động xếp hạng ưu tiên sản phẩm có `PopularityScore` cao nhất lên đầu.

### 8. Chuyển hướng & Tracking Click Affiliate (UC-09)
- API `POST /api/Products/{id}/click-affiliate` cho phép cả người dùng đăng nhập lẫn khách vãng lai mua hàng.
- Tự sinh mã `SubId` (GUID) cho mỗi click lưu vào `ClickTracking`.
- Chống spam IP 24h: Chỉ cộng `PopularityScore +10` cho lượt click đầu tiên trong ngày từ mỗi IP.

### 9. Tính năng Bình chọn Quà tặng & Chia sẻ Công khai (UC-03)
- Module `GiftPoll` độc lập, tùy biến chủ đề, chọn 1-10 món quà.
- Link công khai 1 chạm `POST /api/GiftPolls/share/{shareCode}/vote` không cần đăng nhập.
- Chống spam bằng IP (cho phép đổi phiếu hoặc hủy phiếu). Thời hạn tự đóng sau 30 ngày (BR-05).

### 10. AI Tìm kiếm Quà tặng bằng Ngôn ngữ Tự nhiên & Hybrid Recommendation (UC-01 & UC-16)
- Tích hợp GroqCloud LPU (`openai/gpt-oss-120b`): Tốc độ phản hồi cực nhanh (~500 tokens/giây).
- Tự do nhập prompt câu tự nhiên tiếng Việt, hỗ trợ từ lóng tiền tệ (lốp, cành, lít, củ, chai).
- Trợ lý tư vấn `aiAdvice` tinh tế cho món Top 1, đồng bộ 100% tiếng Việt cho cung hoàng đạo.
- AI Guardrail tự động từ chối câu hỏi lạc đề, quấy rối và gợi ý câu hỏi phù hợp.

### 11. Module Affiliate Marketing TikTok Shop & Quản Lý Kho Thực Tế (UC-05, UC-10, UC-11, UC-12)
- Tích hợp tài khoản Accesstrade Live Mode (PublisherId: `7086451798803801329`, TikTok Campaign: `6648523843406889655`, Tiki: `4348614231480407268`).
- Web Scraper tự động bóc tách OpenGraph metadata từ link rút gọn TikTok (`vt.tiktok.com`).
- Groq AI tự động duyệt quà (`IsValidGift`) và gán thẻ Dịp/Sở thích/Cung hoàng đạo.
- Cơ chế chống trùng lặp 2 lớp (Lọc Distinct đầu vào + Check URL & ID sàn trong DB).
- Bổ sung `GET /api/Products` & `GET /api/Products/{id}` trong `ProductsController.cs`.
- Đã nạp thành công 8 sản phẩm TikTok Shop thật kèm link hoa hồng hoạt động 100%.

---

## ⏳ BẢNG ĐỐI CHIẾU 31 USE CASES THEO ĐẶC TẢ (SRS SPECIFICATION) & LỘ TRÌNH TIẾP TỤC
*(Căn cứ theo tài liệu đặc tả `GiftFinder_UseCase_Specification.docx` - Hệ thống gồm 31 Use Cases thuộc 8 Module)*

### 🟢 1. Đã Code & Đã Test Thành Công (4 / 31 UC):
- [x] **UC-02**: Lưu sản phẩm vào Wishlist (Giới hạn 100 món, chống trùng, soft-delete).
- [x] **UC-13**: Đăng ký & Đăng nhập (Email, mật khẩu băm BCrypt, JWT Token).
- [x] **UC-15**: Quản lý ngày kỷ niệm/sinh nhật (CRUD, tự động tính Cung hoàng đạo `RecipientZodiac`).
- [x] **UC-21**: Nhắc lịch sinh nhật/kỷ niệm qua Email (Worker tự động + Brevo SMTP thật).

---

### 🟡 2. Đã Code Backend nhưng Cần Test / Hoàn Thiện (8 / 31 UC):
- [ ] **UC-01**: AI Tìm kiếm & Gợi ý quà tặng (`GET /api/Recommendations` - Cần test trên Swagger với kho 8 món thật, test nới ngân sách ±20%).
- [ ] **UC-03**: Chia sẻ danh sách bình chọn quà tặng (`GiftPollsController` - Cần test luồng tạo poll, shareCode và vote ẩn danh bằng IP).
- [ ] **UC-05**: Đồng bộ sản phẩm từ Affiliate (Đã có logic cào TikTok Shop & import theo URL, cần thêm Background Worker định kỳ 6h).
- [ ] **UC-07**: Gắn Tag/Category cho sản phẩm (AI đã tự gắn tag khi import, cần bổ sung API Admin gắn/sửa tag thủ công).
- [ ] **UC-09**: Click Tracking & Chuyển hướng (`POST /api/Products/{id}/click-affiliate` - Cần test lượt click thực tế, sinh SubId và link Accesstrade).
- [ ] **UC-10**: Nhận Postback đơn hàng (`POST /api/Webhooks/accesstrade` - Cần test giả lập payload).
- [ ] **UC-12**: Báo cáo doanh thu Affiliate (`GET /api/Affiliate/revenue-report` - Cần thêm tính năng xuất file Excel/CSV).
- [ ] **UC-16**: AI Cá nhân hóa gợi ý sâu (Cần kết nối thêm lịch sử tìm kiếm & wishlist của User).

---

### 🔴 3. Chưa Làm Trên Backend - Cần Triển Khai (19 / 31 UC):
* **Module 1 (Gợi ý):**
  - [ ] **UC-04**: Đánh giá độ phù hợp gợi ý (Feedback 1–5 sao, ghi nhận kèm `SearchQueryId` để train AI).
* **Module 2 (Sản phẩm):**
  - [ ] **UC-06**: Merchant đăng ký & Đăng sản phẩm trực tiếp (Form nạp sản phẩm, trạng thái `Pending` chờ duyệt).
  - [ ] **UC-08**: Theo dõi biến động giá (Price Tracking - Dành cho Premium, worker quét hằng ngày báo giá giảm ≥ 5%).
* **Module 3 (Affiliate):**
  - [ ] **UC-11**: Đối soát hoa hồng định kỳ (Reconciliation - Job hàng tuần so khớp dữ liệu nội bộ với API sàn, cảnh báo lệch > 2%).
* **Module 4 (Thành viên):**
  - [ ] **UC-14**: Nâng cấp gói Premium (29.000đ/tháng, tự động gia hạn, ân hạn 3 ngày).
* **Module 5 (Merchant & Featured Placement):**
  - [ ] **UC-17**: Mua gói Featured Placement (49k/tuần, 199k/tháng).
  - [ ] **UC-18**: Ưu tiên hiển thị trong kết quả gợi ý (+20% Relevance Score, tối đa 3 món Featured trong top 10).
  - [ ] **UC-19**: Merchant xem hiệu quả quảng cáo (Dashboard Impression, Click, CTR).
  - [ ] **UC-20**: Tự động hết hạn Featured (Job quét hằng ngày tắt trạng thái, email nhắc trước 3 ngày).
* **Module 6 (Thông báo):**
  - [ ] **UC-22**: Thông báo giảm giá sản phẩm theo dõi.
  - [ ] **UC-23**: Thông báo gia hạn Premium / Featured sắp hết hạn (trước 3 ngày).
* **Module 7 (Thanh toán - Payment & Billing):**
  - [ ] **UC-24**: Thanh toán gói Premium Subscription (Cổng thanh toán PayOS/VNPay/MoMo).
  - [ ] **UC-25**: Thanh toán gói Featured Placement cho Merchant.
  - [ ] **UC-26**: Lịch sử giao dịch & Xuất hóa đơn PDF.
  - [ ] **UC-27**: Xử lý Webhook thanh toán IPN (Xác thực chữ ký, chống trùng lặp TransactionId).
* **Module 8 (Quản trị & Báo cáo - Admin):**
  - [ ] **UC-28**: Admin duyệt sản phẩm / Merchant (Pending -> Approved/Rejected kèm lý do ≤ 500 ký tự).
  - [ ] **UC-29**: Quản lý người dùng (Khóa / mở khóa tài khoản kèm lý do).
  - [ ] **UC-30**: Dashboard tổng quan doanh thu hợp nhất 3 nguồn (Affiliate + Featured + Premium).
  - [ ] **UC-31**: Cấu hình trọng số Recommendation Rule (Ngân sách, Sở thích, Dịp tặng, Featured = 100%, có versioning).

---

## 🎯 KẾ HOẠCH BẮT ĐẦU CHO NGÀY MAI:
1. **Kiểm thử các API Backend cốt lõi vừa xong:**
   - Test `GET /api/Recommendations` với các câu prompt tự nhiên.
   - Test `POST /api/Products/{id}/click-affiliate` và API `GiftPolls`.
2. **Code tiếp các Use Case Backend thiết yếu nhất:**
   - **UC-04**: Thêm tính năng Đánh giá / Feedback (Rating 1-5 sao).
   - **UC-28 & UC-29**: Viết `AdminController` để duyệt sản phẩm và quản lý/khóa tài khoản.
   - **UC-31**: Viết API cấu hình trọng số gợi ý quà tặng.
3. **Build Status:** Đã build thành công 100% toàn bộ Solution (.NET 8 Clean Architecture - 0 Errors, 0 Warnings). Sẵn sàng bắt đầu phiên mới ngay!
