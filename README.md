# GiftFinder - Hệ Thống Gợi Ý Quà Tặng Thông Minh & Affiliate Marketing Đa Sàn

> **Trạng thái dự án:** Sẵn sàng 100% cho phát triển Frontend & các Background Workers tiếp theo.  
> **Tài liệu tiến độ & Bối cảnh kỹ thuật chi tiết:** Xem tại [PROJECT_STATUS.md](file:///c:/Users/nguye/Downloads/CSharp/GiftFinder/PROJECT_STATUS.md).

### Tổng quan nhanh:
* **Kiến trúc:** C# (.NET 8) Clean Architecture.
* **Cơ sở dữ liệu:** PostgreSQL (`GiftFinderDb`), 128 sản phẩm sạch 100% (Giá thật, ảnh CDN thật, tag Groq AI, link affiliate chuẩn).
* **2 Sàn Trọng Tâm:** Shopee (Shopee Direct Affiliate `an_17374170692`) & TikTok Shop (Accesstrade CPS).
* **Trình cào dữ liệu:** Microsoft Edge CDP (Port 9222) với Playwright, tối ưu 0s thoát tab, 5-7s giãn cách chống WAF, tự động tạm dừng 90s khi gặp Captcha.