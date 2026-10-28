# 🎨 TÀI LIỆU HƯỚNG DẪN THIẾT KẾ FIGMA & KIẾN TRÚC PHÂN QUYỀN (GIFTFINDER)

Tài liệu này tổng hợp toàn bộ **Cấu trúc phân quyền (Roles)**, **Danh mục màn hình cần thiết kế**, **Các trạng thái giao diện (UI States)** và **Quy chuẩn Design System** của GiftFinder để bạn dễ dàng triển khai thiết kế trên Figma.

---

## 👥 1. Cấu trúc 3 Role trong Hệ thống

Hệ thống GiftFinder sử dụng cơ chế **Role-Based Access Control (RBAC)** với 3 nhóm người dùng tương ứng với backend:

```mermaid
flowchart TD
    Guest["Khách vãng lai (Guest)"] -->|Đăng ký / Đăng nhập| Auth["Hệ thống Xác thực (JWT Auth)"]
    Auth -->|Role: Member (1)| Member["👤 Member (Người mua quà)"]
    Auth -->|Role: Merchant (2)| Merchant["🏪 Merchant (Đối tác bán hàng)"]
    Auth -->|Role: Admin (0)| Admin["🛡️ Admin (Quản trị viên)"]
```

| Role | Tên hiển thị | Mã Role (Backend) | Đối tượng sử dụng | Quyền hạn và Tính năng chính |
| :--- | :--- | :---: | :--- | :--- |
| **Guest** | Khách vãng lai | — | Người dùng chưa đăng nhập | - Xem Landing Page, tìm kiếm quà bằng AI, duyệt danh mục.<br>- Click link mua quà chuyển hướng sang Shopee / TikTok Shop.<br>- Giỏ hàng tạm thời (Local Cart). |
| **Member** | Thành viên mua quà | `1` | Người dùng cá nhân tìm mua quà | - Lưu danh sách quà tặng yêu thích (**Wishlist**).<br>- Đặt lịch nhắc ngày kỷ niệm / sinh nhật (**Reminder Dates**).<br>- Đăng ký theo dõi cảnh báo khi quà giảm giá (**Price Tracking**).<br>- Gói thành viên Premium (VNPAY 29k/tháng) mở khóa gợi ý AI không giới hạn. |
| **Merchant** | Đối tác bán hàng | `2` | Chủ shop quà tặng thủ công / decor / hoa tươi | - Đăng ký mở gian hàng đối tác (**Merchant Application**).<br>- Đăng bán sản phẩm quà tặng trực tiếp (chờ Admin duyệt).<br>- Xem **Dashboard Thống kê**: Tổng quà, lượt click mua hàng từ khách.<br>- Mua gói **Đẩy Top quà tặng nổi bật (Featured Placement)** 49k/tuần hoặc 199k/tháng qua cổng **VNPAY**. |
| **Admin** | Quản trị hệ thống | `0` | Ban quản trị GiftFinder | - **Kiểm duyệt quà tặng** từ các Shop đối tác (`Phê duyệt` hoặc `Từ chối` kèm lý do).<br>- Quản lý tài khoản vi phạm (Khóa/Mở khóa tài khoản).<br>- Kích hoạt thủ công tiến trình **Crawler đồng bộ giá sàn Shopee / TikTok**.<br>- Xem báo cáo doanh thu đẩy tin & tổng quan hệ thống. |

---

## 🖥️ 2. Danh mục các Màn hình cần thiết kế trên Figma

### 1. Màn hình Xác thực (Auth Screens)
- **Bố cục (Layout)**: Split Screen tỉ lệ 50:50 (hoặc 55:45):
  - **Cột trái (Branding Poster)**: Ảnh bìa hộp quà cao cấp full-bleed, overlay gradient tối, câu slogan ("*Mỗi món quà là một cách nói 'Tôi quan tâm'*") và trích dẫn đánh giá 5★ của khách hàng.
  - **Cột phải (Form Box)**: Nền trắng sạch sẽ, gồm:
    - Nút `← Về trang chủ` góc trên bên trái.
    - Logo GiftFinder căn giữa.
    - Tiêu đề: *"Chào mừng trở lại"* (Login) hoặc *"Tạo tài khoản mới"* (Register).
    - Các Input: Họ tên (nếu đăng ký), Email, Mật khẩu (kèm nút Ẩn/Hiện mắt), Checkbox Ghi nhớ đăng nhập, Link Quên mật khẩu.
    - Nút bấm chính `Button.rose` (Đăng nhập / Đăng ký).
    - Link chuyển đổi giữa 2 tab: *"Chưa có tài khoản? Đăng ký miễn phí"*.

> Màn hình Auth hoạt động **độc lập (Standalone Screen)**: Không hiển thị Promo Bar, không có Header tìm kiếm, không có Giỏ hàng và không có Chatbot AI nổi ở góc dưới.

---

### 2. Header & Trạng thái Người dùng (Header User States)
Trên thanh điều hướng chính (`Site Header`), góc bên phải có 2 trạng thái:
- **Trạng thái Chưa đăng nhập**:
  - Nút `Đăng nhập` (Icon User + Viền xám `Button.secondary`).
- **Trạng thái Đã đăng nhập**:
  - Một **User Pill** bo tròn: Avatar tròn (chữ cái đầu tên) + Họ tên + Badge vai trò (`Admin` nền đen, `Shop` nền tím indigo, `Member` nền hồng rose).
  - Khi click vào mở **Dropdown Menu**:
    - Header dropdown: Họ tên in đậm + Email màu xám.
    - Các menu item tương ứng theo Role:
      - Nếu là Admin: `🛡️ Quản trị hệ thống (Admin Console)`
      - Nếu là Merchant: `🏪 Kênh Người Bán (Shop Portal)` · `➕ Đăng quà mới`
      - Nếu là Member: `♡ Quà đã yêu thích` · `⏰ Lịch nhắc kỷ niệm`
    - Dòng kẻ ngang phân cách.
    - Nút màu đỏ: `Đăng xuất`.

---

### 3. Kênh Người Bán (Merchant Portal)
Dành cho đối tác Shop bán hàng, gồm 2 view:
1. **View 1: Khi chưa là Shop (Landing quảng bá mở gian hàng)**
   - Banner mời gọi hợp tác bán quà.
   - Thống kê lợi ích: AI tìm đúng khách, nhận tiền qua VNPAY, đẩy Top hiển thị.
   - 3 bước bắt đầu (Tạo tài khoản -> Đăng quà -> Nhận đơn).
   - Modal Form đăng ký: Tên shop, Họ tên đại diện, Số điện thoại, Email, Mật khẩu, Link Shopee/TikTok/Fanpage.
2. **View 2: Khi đã đăng nhập Shop (Merchant Dashboard)**
   - Header: Tên gian hàng + Badge `ĐỐI TÁC CHÍNH THỨC` + Nút `+ Đăng quà mới`.
   - **4 Thẻ số liệu (Stats Cards)**:
     1. 🎁 Tổng quà của Shop
     2. 👁️ Lượt click mua quà
     3. ⭐ Quà Featured đang đẩy Top
     4. ⏳ Quà đang chờ Admin duyệt
   - **Danh sách quà tặng của Shop**: Thẻ quà hiển thị ảnh, tên, giá, số lượt click, huy hiệu trạng thái (`Đã duyệt` - Xanh lá, `Chờ duyệt` - Xám, `Từ chối` - Đỏ) và nút `⚡ Đẩy Top Featured (VNPAY)`.
   - **Modal Đăng quà mới**: Nhập tên quà, giá bán, giá gốc, link sản phẩm sàn, link ảnh, mô tả ngắn.

---

### 4. Bảng điều khiển Quản Trị Hệ Thống (Admin Console)
Dành riêng cho Quản trị viên (`admin@giftfinder.vn`):
- **Header**: Tiêu đề "Bảng điều khiển Quản trị viên" + Nút `🔄 Đồng bộ giá Shopee / TikTok` + Nút `← Xem website`.
- **4 Thẻ chỉ số hệ thống**:
  1. 📦 Tổng sản phẩm trong Database: `243+` (116 Shopee · 124 TikTok · 3 Shop)
  2. ⏳ Sản phẩm chờ duyệt
  3. 💳 Cổng thanh toán VNPAY: Hoạt động (Sandbox HMAC-SHA512)
  4. 🤖 Crawler giá & Affiliate: Sẵn sàng
- **Bảng / Lưới duyệt sản phẩm chờ**:
  - Hiển thị từng món quà kèm ảnh, tên, giá bán, nguồn shop.
  - 2 nút hành động: `✓ Phê duyệt` (màu xanh lá) và `✕ Từ chối` (màu đỏ kèm popup nhập lý do).
  - Empty State khi không có hàng chờ: Minh họa hộp quà xanh và thông báo *"Toàn bộ sản phẩm đã được kiểm duyệt an toàn"*.

---

### 5. Bộ sưu tập Yêu thích & Tiện ích Member (Member Portal)
- Danh sách quà đã bấm tim yêu thích (`WishlistGrid`).
- Nút xóa khỏi danh sách, lọc sắp xếp theo giá / đánh giá.
- Thẻ kích hoạt chuông cảnh báo khi quà giảm giá (`Price Drop Alert`).

---

## 🎨 3. Quy chuẩn Design System (Design Tokens)

| Thành phần | Mã màu / Giá trị | Ứng dụng |
| :--- | :--- | :--- |
| **Màu chủ đạo (Rose)** | `#e11d48` (Rose 600), Gradient `#e11d48` → `#f43f5e` | Nút kêu gọi hành động (CTA), giá khuyến mãi, tim yêu thích, badge hot. |
| **Màu Chữ & Đen (Ink)** | `#0f172a` (Slate 900), Promo bar: `#111827` | Tiêu đề chính, thanh thông báo trên cùng, nút bấm Admin. |
| **Màu Tím (Indigo)** | `#4f46e5` (Indigo 600) | Nhãn Merchant, gợi ý AI, viền tìm kiếm thông minh. |
| **Màu Xanh lá (Green)** | `#16a34a` (Green 600), nền `#ecfdf5` | Sản phẩm đã duyệt (Approved), trạng thái hoạt động. |
| **Màu Đường viền (Line)** | `#e2e8f0` (Border), `#f1f5f9` (Subtle) | Viền card sản phẩm, viền input, đường kẻ chia tách. |
| **Bo góc (Border Radius)** | `10px` - `12px` (Inputs & Buttons), `16px` (Cards), `20px` (Modals) | Giữ form dáng mềm mại, thanh lịch, hiện đại. |
| **Typography** | Font: `Be Vietnam Pro` (400, 500, 600, 700, 800) | Chuẩn tiếng Việt có dấu, độ hiển thị sắc nét trên màn hình Retina/4K. |

