# GiftFinder - Hệ Thống Gợi Ý Quà Tặng Thông Minh & Thương Mại Điện Tử

> **Trạng thái dự án:** Sẵn sàng 100% cho phát triển Frontend & Backend.  
> **Tài liệu tiến độ & Bối cảnh kỹ thuật chi tiết:** Xem tại [PROJECT_STATUS.md](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/PROJECT_STATUS.md).

---

## 📁 Cấu Trúc Dự Án (Monorepo)

```text
GiftFinder/
├── backend/                       # .NET 8 Web API (Clean Architecture)
│   ├── GiftFinder.API/            # Controllers, Middlewares, DI configuration
│   ├── GiftFinder.Application/    # CQRS MediatR Commands, Queries, DTOs
│   ├── GiftFinder.Domain/         # Core Entities, Enums, Business Rules
│   ├── GiftFinder.Infrastructure/ # EF Core PostgreSQL, VNPay, Brevo SMTP, Groq AI
│   └── GiftFinder.slnx            # Solution backend độc lập
│
├── frontend/                      # React 19 + TypeScript + Vite + Tailwind CSS
│   ├── src/
│   │   ├── App.tsx                # Giao diện chính (đồng bộ từ Figma Make)
│   │   ├── index.css              # Tailwind CSS styles
│   │   └── main.tsx               # React DOM bootstrap
│   ├── vite.config.ts             # Dev proxy chuyển tiếp /api -> localhost:5151
│   └── package.json
│
├── GiftFinder.slnx                # Root Solution
├── PROJECT_STATUS.md              # Báo cáo tiến độ 31 Use Cases
└── README.md
```

---

## 🚀 Hướng Dẫn Khởi Chạy

### 1. Chạy Backend (.NET 8 Web API)
```bash
# Di chuyển vào backend và chạy API
cd backend
dotnet run --project GiftFinder.API
```
* **Swagger UI:** `http://localhost:5151/swagger`
* **Cổng API:** `http://localhost:5151`

### 2. Chạy Frontend (React + Vite)
```bash
# Di chuyển vào frontend và khởi chạy máy chủ phát triển
cd frontend
npm install   # nếu cần cài đặt mới
npm run dev
```
* **Local Web Server:** `http://localhost:3000`
* Tự động proxy các request `/api/*` tới Backend `http://localhost:5151`.
