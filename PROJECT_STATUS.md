# TIẾN ĐỘ DỰ ÁN GIFTFINDER
*(Cập nhật lần cuối: Xem lịch sử file)*

File này được tạo ra để ghi nhớ những gì chúng ta đã làm, giúp các phiên làm việc sau không bị đứt đoạn.

## 🟢 NHỮNG TÍNH NĂNG ĐÃ HOÀN THÀNH (DONE)

### 1. Kiến trúc & Database
- Khởi tạo xong khung dự án chuẩn **Clean Architecture** (Domain, Application, Infrastructure, API).
- Cấu hình Entity Framework Core kết nối với cơ sở dữ liệu **PostgreSQL**.
- Khởi tạo thành công các bảng dữ liệu (Users, Products, Tags, Wishlists...).

### 2. Tính năng Xác thực - Authentication (UC-13)
- Viết thành công luồng Đăng nhập / Đăng ký bằng Email & Mật khẩu (CQRS với MediatR).
- Mật khẩu được băm bảo mật bằng thuật toán **BCrypt**.
- Hệ thống sinh và xác thực **JWT Token** đã hoạt động hoàn hảo.
- Đã test thành công trên Swagger (có ổ khóa Authorize).

### 3. Cập nhật Cấu trúc cho AI Cung Hoàng Đạo (Zodiac Add-on - Tầng Domain)
- Tạo Enum `ZodiacSign.cs` (Đầy đủ 12 cung hoàng đạo).
- Sửa bảng `Product`: Bổ sung cột tùy chọn `SuitableZodiacs` để gán nhãn cung hoàng đạo cho quà tặng.
- Viết `ZodiacCalculator.cs`: Thuật toán tự động chuyển đổi Ngày/Tháng sinh thành Cung Hoàng Đạo.
- Sửa `ReminderDate.cs`: Tự động nhận diện nếu sự kiện là "Sinh nhật" thì tự gán Cung hoàng đạo cho người nhận quà.

---

## ⏳ NHỮNG VIỆC CẦN LÀM TIẾP THEO (TO-DO)

Khi quay lại dự án, hãy bắt đầu làm từ trên xuống dưới theo danh sách này:

- [x] **1. Cập nhật Database:** Đã đẩy cấu trúc Cung Hoàng Đạo lên PostgreSQL.
- [x] **2. Tích hợp AI (Tầng Infrastructure):** Đã hoàn thiện `GeminiAiService` và tích hợp API Key thành công.
- [x] **3. Viết API Gợi ý (UC-01 & Mở rộng):** Đã chạy mượt mà API `GET /api/recommendations` kết hợp AI chém gió siêu mượt.
- [ ] **4. Quản lý ngày kỷ niệm (UC-15):** Viết các API Thêm/Sửa/Xóa `ReminderDate` (ngày sinh nhật của người thân).
- [ ] **5. Tích hợp Đăng nhập Google (OAuth):** (Tùy chọn) Bổ sung luồng đăng nhập nhanh bằng Google.
