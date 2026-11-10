using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GiftFinder.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static void SeedData(IServiceProvider serviceProvider)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.ExecuteSqlRaw(@"
                    ALTER TABLE ""Products"" ALTER COLUMN ""OriginalUrl"" TYPE text;
                    ALTER TABLE ""Products"" ALTER COLUMN ""AffiliateUrl"" TYPE text;
                    ALTER TABLE ""Products"" ALTER COLUMN ""ImageUrl"" TYPE text;
                    ALTER TABLE ""ClickTrackings"" ALTER COLUMN ""TrackingUrl"" TYPE text;

                    ALTER TABLE ""Merchants"" ADD COLUMN IF NOT EXISTS ""PickupProvince"" character varying(100) NOT NULL DEFAULT 'Hà Nội';
                    ALTER TABLE ""Merchants"" ADD COLUMN IF NOT EXISTS ""PickupAddress"" character varying(300) NULL;
                    ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""StockQuantity"" integer NOT NULL DEFAULT 50;

                    CREATE TABLE IF NOT EXISTS ""MerchantOrders"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""OrderCode"" character varying(50) NOT NULL,
                        ""MerchantId"" uuid NOT NULL REFERENCES ""Merchants""(""Id"") ON DELETE CASCADE,
                        ""BuyerUserId"" uuid NULL REFERENCES ""Users""(""Id"") ON DELETE SET NULL,
                        ""ReceiverName"" character varying(150) NOT NULL,
                        ""ReceiverPhone"" character varying(25) NOT NULL,
                        ""ReceiverProvince"" character varying(100) NOT NULL,
                        ""ReceiverDistrict"" character varying(100) NOT NULL,
                        ""ReceiverAddress"" character varying(350) NOT NULL,
                        ""DeliveryNote"" character varying(500) NULL,
                        ""HasGiftWrap"" boolean NOT NULL DEFAULT false,
                        ""GiftMessage"" character varying(500) NULL,
                        ""HidePriceOnPackage"" boolean NOT NULL DEFAULT false,
                        ""Subtotal"" bigint NOT NULL DEFAULT 0,
                        ""ShippingFee"" bigint NOT NULL DEFAULT 0,
                        ""GiftWrapFee"" bigint NOT NULL DEFAULT 0,
                        ""DiscountAmount"" bigint NOT NULL DEFAULT 0,
                        ""TotalAmount"" bigint NOT NULL DEFAULT 0,
                        ""PaymentMethod"" character varying(30) NOT NULL DEFAULT 'COD',
                        ""ShippingMethod"" character varying(30) NOT NULL DEFAULT 'Standard',
                        ""ShippingCarrier"" character varying(100) NULL,
                        ""TrackingCode"" character varying(100) NULL,
                        ""Status"" character varying(30) NOT NULL DEFAULT 'Pending',
                        ""PackedAt"" timestamp with time zone NULL,
                        ""ShippedAt"" timestamp with time zone NULL,
                        ""CompletedAt"" timestamp with time zone NULL,
                        ""CancelReason"" character varying(500) NULL,
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        ""UpdatedAt"" timestamp with time zone NULL,
                        ""IsDeleted"" boolean NOT NULL DEFAULT false
                    );

                    CREATE TABLE IF NOT EXISTS ""MerchantOrderItems"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""MerchantOrderId"" uuid NOT NULL REFERENCES ""MerchantOrders""(""Id"") ON DELETE CASCADE,
                        ""ProductId"" uuid NOT NULL REFERENCES ""Products""(""Id"") ON DELETE RESTRICT,
                        ""ProductName"" character varying(250) NOT NULL,
                        ""ProductImageUrl"" text NULL,
                        ""Variant"" character varying(100) NOT NULL DEFAULT 'Tiêu chuẩn',
                        ""UnitPrice"" bigint NOT NULL DEFAULT 0,
                        ""Quantity"" integer NOT NULL DEFAULT 1,
                        ""LineTotal"" bigint NOT NULL DEFAULT 0,
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        ""UpdatedAt"" timestamp with time zone NULL,
                        ""IsDeleted"" boolean NOT NULL DEFAULT false
                    );

                    UPDATE ""Products""
                    SET ""OriginalUrl"" = 'https://giftfinder.vn/product/' || ""Id""::text
                    WHERE ""Source"" = 'DirectMerchant' AND ""OriginalUrl"" LIKE '%shopee.vn%';
                ");

                var hasher = scope.ServiceProvider.GetService<GiftFinder.Application.Common.Interfaces.IPasswordHasher>();
                if (hasher != null && !context.Users.Any(u => u.Role == GiftFinder.Domain.Enums.UserRole.Admin))
                {
                    var admin = new GiftFinder.Domain.Entities.User(
                        "admin@giftfinder.vn",
                        hasher.HashPassword("Admin@123456"),
                        "System Administrator",
                        GiftFinder.Domain.Enums.UserRole.Admin
                    );
                    context.Users.Add(admin);
                    context.SaveChanges();
                }

                if (hasher != null && !context.Users.Any(u => u.Role == GiftFinder.Domain.Enums.UserRole.Merchant))
                {
                    var merchantUser = new GiftFinder.Domain.Entities.User(
                        "shopdemo@giftfinder.vn",
                        hasher.HashPassword("Password@123"),
                        "Chủ Shop Handmade",
                        GiftFinder.Domain.Enums.UserRole.Merchant
                    );
                    context.Users.Add(merchantUser);
                    var merchantProfile = new GiftFinder.Domain.Entities.Merchant(
                        merchantUser.Id,
                        "Tiệm Quà & Nến Thơm Mộc",
                        "shopdemo@giftfinder.vn",
                        "0909123456",
                        "GPKD-8888",
                        "Hà Nội",
                        "Số 18 Ngõ 45 Trần Thái Tông, Quận Cầu Giấy, Hà Nội"
                    );
                    context.Merchants.Add(merchantProfile);
                    context.SaveChanges();
                }

                if (hasher != null && !context.Users.Any(u => u.Role == GiftFinder.Domain.Enums.UserRole.Member))
                {
                    var member = new GiftFinder.Domain.Entities.User(
                        "memberdemo@giftfinder.vn",
                        hasher.HashPassword("Password@123"),
                        "Nguyễn Văn Thành Viên",
                        GiftFinder.Domain.Enums.UserRole.Member
                    );
                    context.Users.Add(member);
                    context.SaveChanges();
                }

                if (!context.Tags.Any())
                {
                    var defaultTags = new System.Collections.Generic.List<GiftFinder.Domain.Entities.Tag>
                    {
                        // Occasions
                        new("Sinh nhật", "sinh-nhat", GiftFinder.Domain.Enums.TagType.Occasion),
                        new("Valentine", "valentine", GiftFinder.Domain.Enums.TagType.Occasion),
                        new("20/10", "20-10", GiftFinder.Domain.Enums.TagType.Occasion),
                        new("8/3", "8-3", GiftFinder.Domain.Enums.TagType.Occasion),
                        new("Giáng sinh", "giang-sinh", GiftFinder.Domain.Enums.TagType.Occasion),
                        new("Kỷ niệm", "ky-niem", GiftFinder.Domain.Enums.TagType.Occasion),
                        new("Tết", "tet", GiftFinder.Domain.Enums.TagType.Occasion),
                        new("Tốt nghiệp", "tot-nghiep", GiftFinder.Domain.Enums.TagType.Occasion),
                        new("Tân gia", "tan-gia", GiftFinder.Domain.Enums.TagType.Occasion),

                        // Hobbies
                        new("Công nghệ", "cong-nghe", GiftFinder.Domain.Enums.TagType.Hobby),
                        new("Decor", "decor", GiftFinder.Domain.Enums.TagType.Hobby),
                        new("Nến thơm & Chill", "nen-thom-chill", GiftFinder.Domain.Enums.TagType.Hobby),
                        new("Thời trang", "thoi-trang", GiftFinder.Domain.Enums.TagType.Hobby),
                        new("Mỹ phẩm & Làm đẹp", "my-pham-lam-dep", GiftFinder.Domain.Enums.TagType.Hobby),
                        new("Đọc sách", "doc-sach", GiftFinder.Domain.Enums.TagType.Hobby),
                        new("Âm nhạc", "am-nhac", GiftFinder.Domain.Enums.TagType.Hobby),
                        new("Nấu ăn", "nau-an", GiftFinder.Domain.Enums.TagType.Hobby),
                        new("Thể thao & Du lịch", "the-thao-du-lich", GiftFinder.Domain.Enums.TagType.Hobby),

                        // Recipients
                        new("Bạn gái", "ban-gai", GiftFinder.Domain.Enums.TagType.Recipient),
                        new("Bạn trai", "ban-trai", GiftFinder.Domain.Enums.TagType.Recipient),
                        new("Bố mẹ", "bo-me", GiftFinder.Domain.Enums.TagType.Recipient),
                        new("Đồng nghiệp", "dong-nghiep", GiftFinder.Domain.Enums.TagType.Recipient),
                        new("Người yêu", "nguoi-yeu", GiftFinder.Domain.Enums.TagType.Recipient),
                        new("Bạn bè", "ban-be", GiftFinder.Domain.Enums.TagType.Recipient)
                    };

                    context.Tags.AddRange(defaultTags);
                    context.SaveChanges();
                }

                // Đảm bảo Shop Demo có trạng thái Verified và bộ dữ liệu mẫu đa dạng (Approved, Featured, Pending, Rejected)
                var demoMerchant = context.Merchants.FirstOrDefault(m => m.ContactEmail == "shopdemo@giftfinder.vn")
                                   ?? context.Merchants.FirstOrDefault();
                if (demoMerchant != null)
                {
                    if (!demoMerchant.IsVerified || string.IsNullOrWhiteSpace(demoMerchant.PickupAddress))
                    {
                        demoMerchant.Verify();
                        demoMerchant.UpdateProfile(
                            demoMerchant.ShopName,
                            demoMerchant.ContactEmail,
                            demoMerchant.ContactPhone,
                            demoMerchant.BusinessLicense,
                            string.IsNullOrWhiteSpace(demoMerchant.PickupProvince) ? "Hà Nội" : demoMerchant.PickupProvince,
                            string.IsNullOrWhiteSpace(demoMerchant.PickupAddress) ? "Số 18 Ngõ 45 Trần Thái Tông, Quận Cầu Giấy, Hà Nội" : demoMerchant.PickupAddress
                        );
                        context.SaveChanges();
                    }

                    if (!context.Products.Any(p => p.MerchantId == demoMerchant.Id))
                    {
                        var allTags = context.Tags.ToList();
                        var tagSinhNhat = allTags.FirstOrDefault(t => t.Slug == "sinh-nhat")?.Id;
                        var tagBanGai = allTags.FirstOrDefault(t => t.Slug == "ban-gai")?.Id;
                        var tagNenThom = allTags.FirstOrDefault(t => t.Slug == "nen-thom-chill")?.Id;
                        var tagDecor = allTags.FirstOrDefault(t => t.Slug == "decor")?.Id;

                        var p1 = new GiftFinder.Domain.Entities.Product(
                            "Set Quà Nến Thơm Tinh Dầu Thiên Nhiên & Hoa Khô Vintage",
                            459000,
                            string.Empty,
                            GiftFinder.Domain.Enums.ProductSource.DirectMerchant,
                            null,
                            "https://images.unsplash.com/photo-1603006905003-be475563bc59?auto=format&fit=crop&w=900&q=80",
                            550000,
                            demoMerchant.Id,
                            45);
                        p1.SetDescription("Hộp quà thủ công gồm hũ nến thơm sáp đậu nành 200g hương gỗ đàn hương ấm áp, bó hoa khô baby Pháp và thiệp viết tay sang trọng.");
                        p1.SetSuitableZodiacs(new[] { GiftFinder.Domain.Enums.ZodiacSign.Taurus, GiftFinder.Domain.Enums.ZodiacSign.Libra, GiftFinder.Domain.Enums.ZodiacSign.Pisces });
                        p1.Approve();
                        p1.ActivateFeatured(GiftFinder.Domain.Enums.FeaturedPlanType.Weekly, DateTime.UtcNow);
                        if (tagSinhNhat.HasValue) p1.AddTag(tagSinhNhat.Value);
                        if (tagBanGai.HasValue) p1.AddTag(tagBanGai.Value);
                        if (tagNenThom.HasValue) p1.AddTag(tagNenThom.Value);

                        var p2 = new GiftFinder.Domain.Entities.Product(
                            "Hộp Quà Ly Gốm Thủ Công Khắc Tên Theo Yêu Cầu",
                            320000,
                            string.Empty,
                            GiftFinder.Domain.Enums.ProductSource.DirectMerchant,
                            null,
                            "https://images.unsplash.com/photo-1514228742587-6b1558fcca3d?auto=format&fit=crop&w=900&q=80",
                            390000,
                            demoMerchant.Id,
                            38);
                        p2.SetDescription("Ly gốm Bát Tràng men hỏa biến chế tác thủ công độc bản, kèm đế lót gỗ thông và muỗng đồng cổ điển.");
                        p2.SetSuitableZodiacs(new[] { GiftFinder.Domain.Enums.ZodiacSign.Virgo, GiftFinder.Domain.Enums.ZodiacSign.Capricorn, GiftFinder.Domain.Enums.ZodiacSign.Cancer });
                        p2.Approve();
                        if (tagSinhNhat.HasValue) p2.AddTag(tagSinhNhat.Value);
                        if (tagDecor.HasValue) p2.AddTag(tagDecor.Value);

                        var p3 = new GiftFinder.Domain.Entities.Product(
                            "Bộ Khuếch Tán Tinh Dầu Hoa Hồng Pháp Cao Cấp 120ml",
                            580000,
                            string.Empty,
                            GiftFinder.Domain.Enums.ProductSource.DirectMerchant,
                            null,
                            "https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?auto=format&fit=crop&w=900&q=80",
                            680000,
                            demoMerchant.Id,
                            25);
                        p3.SetDescription("Tinh dầu khuếch tán que mây lưu hương 60 ngày, thiết kế lọ thủy tinh pha lê sang trọng thích hợp tặng tân gia, sinh nhật.");
                        p3.SetSuitableZodiacs(new[] { GiftFinder.Domain.Enums.ZodiacSign.Leo, GiftFinder.Domain.Enums.ZodiacSign.Libra });
                        if (tagNenThom.HasValue) p3.AddTag(tagNenThom.Value);
                        if (tagBanGai.HasValue) p3.AddTag(tagBanGai.Value);

                        var p4 = new GiftFinder.Domain.Entities.Product(
                            "Set Trà Hoa Thảo Mộc Hữu Cơ & Mật Ong Rừng Tây Bắc",
                            285000,
                            string.Empty,
                            GiftFinder.Domain.Enums.ProductSource.DirectMerchant,
                            null,
                            "https://images.unsplash.com/photo-1544787219-7f47ccb76574?auto=format&fit=crop&w=900&q=80",
                            350000,
                            demoMerchant.Id,
                            30);
                        p4.SetDescription("Set 6 vị trà hoa dưỡng nhan kết hợp hũ mật ong rừng nguyên chất 150ml, món quà sức khỏe tinh tế cho người thân.");
                        p4.SetSuitableZodiacs(new[] { GiftFinder.Domain.Enums.ZodiacSign.Cancer, GiftFinder.Domain.Enums.ZodiacSign.Virgo });
                        p4.Reject("Vui lòng bổ sung thông tin hạn sử dụng và nguồn gốc xuất xứ trong phần mô tả chi tiết.");
                        if (tagSinhNhat.HasValue) p4.AddTag(tagSinhNhat.Value);

                        context.Products.AddRange(p1, p2, p3, p4);
                        context.SaveChanges();
                    }

                    // Khởi tạo dữ liệu mẫu Đơn hàng COD & Lượt Click theo ngày cho Shop Demo nếu chưa có đơn hàng nào
                    var merchantProducts = context.Products
                        .Where(p => p.MerchantId == demoMerchant.Id)
                        .OrderBy(p => p.CreatedAt)
                        .ToList();

                    if (merchantProducts.Count > 0 && !context.MerchantOrders.Any(o => o.MerchantId == demoMerchant.Id))
                    {
                        var prodA = merchantProducts[0];
                        var prodB = merchantProducts.Count > 1 ? merchantProducts[1] : merchantProducts[0];
                        var utcNow = DateTime.UtcNow;

                        // Đơn 1: Đã giao thành công (Cách đây 5 ngày - Cùng tỉnh Hà Nội - Hỏa tốc 2h)
                        var o1 = new GiftFinder.Domain.Entities.MerchantOrder(
                            "GF-261102-4812",
                            demoMerchant.Id,
                            null,
                            "Nguyễn Minh Anh",
                            "0912345678",
                            "Hà Nội",
                            "Quận Cầu Giấy",
                            "Tòa nhà Indochina Plaza, 241 Xuân Thủy, Dịch Vọng Hậu",
                            "Giao giờ hành chính giúp mình nhé",
                            true,
                            "Chúc mừng sinh nhật tuổi 24 thật rạng rỡ và hạnh phúc nhé Minh Anh! 💕",
                            true,
                            prodA.Price,
                            50000,
                            30000,
                            0,
                            GiftFinder.Domain.Enums.ShippingMethodType.Express2H
                        );
                        o1.AddItem(prodA.Id, prodA.Name, prodA.ImageUrl, prodA.Price, 1, "Hộp quà tiêu chuẩn");
                        o1.MarkAsShipping("Ahamove Hỏa Tốc 2H", "AHA-882910");
                        o1.MarkAsCompleted(utcNow.AddDays(-5));

                        // Đơn 2: Đã giao thành công (Cách đây 3 ngày - Liên tỉnh TP.HCM)
                        var o2 = new GiftFinder.Domain.Entities.MerchantOrder(
                            "GF-261104-7391",
                            demoMerchant.Id,
                            null,
                            "Trần Hoàng Nam",
                            "0987654321",
                            "TP. Hồ Chí Minh",
                            "Quận 1",
                            "Số 68 Nguyễn Huệ, Phường Bến Nghé",
                            "Gọi trước khi giao 15 phút",
                            true,
                            "Chúc hai bạn kỷ niệm 2 năm ngày yêu thật ấm áp!",
                            true,
                            prodB.Price * 2,
                            0, // Freeship đơn >= 500k
                            30000,
                            50000,
                            GiftFinder.Domain.Enums.ShippingMethodType.Standard
                        );
                        o2.AddItem(prodB.Id, prodB.Name, prodB.ImageUrl, prodB.Price, 2, "Cặp đôi khắc tên");
                        o2.MarkAsShipping("Giao Hàng Nhanh (GHN)", "GHN-592014");
                        o2.MarkAsCompleted(utcNow.AddDays(-3));

                        // Đơn 3: Đã giao thành công (Hôm qua - Nội thành Hà Nội)
                        var o3 = new GiftFinder.Domain.Entities.MerchantOrder(
                            "GF-261106-9104",
                            demoMerchant.Id,
                            null,
                            "Lê Thu Hà",
                            "0934567890",
                            "Hà Nội",
                            "Quận Đống Đa",
                            "Số 102 Thái Hà, Phường Trung Liệt",
                            null,
                            false,
                            null,
                            false,
                            prodA.Price,
                            25000,
                            0,
                            0,
                            GiftFinder.Domain.Enums.ShippingMethodType.Standard
                        );
                        o3.AddItem(prodA.Id, prodA.Name, prodA.ImageUrl, prodA.Price, 1, "Tiêu chuẩn");
                        o3.MarkAsShipping("Giao Hàng Tiết Kiệm (GHTK)", "GHTK-330192");
                        o3.MarkAsCompleted(utcNow.AddDays(-1));

                        // Đơn 4: Đang giao hàng (Hôm nay - Liên tỉnh Đà Nẵng)
                        var o4 = new GiftFinder.Domain.Entities.MerchantOrder(
                            "GF-261107-2048",
                            demoMerchant.Id,
                            null,
                            "Phạm Quốc Bảo",
                            "0978123456",
                            "Đà Nẵng",
                            "Quận Hải Châu",
                            "Số 36 Bạch Đằng, Phường Thạch Thang",
                            "Quà tặng bạn gái, nhờ shop bọc chống sốc kỹ giúp mình",
                            true,
                            "Gửi ngàn yêu thương đến người con gái anh thương nhất! 🌹",
                            true,
                            prodB.Price,
                            35000,
                            30000,
                            0,
                            GiftFinder.Domain.Enums.ShippingMethodType.Standard
                        );
                        o4.AddItem(prodB.Id, prodB.Name, prodB.ImageUrl, prodB.Price, 1, "Khắc tên theo yêu cầu");
                        o4.MarkAsShipping("Viettel Post", "VTP-774829");

                        // Đơn 5: Đơn mới chờ xác nhận (Hôm nay - Nội thành Hà Nội - Hỏa tốc 2h)
                        var o5 = new GiftFinder.Domain.Entities.MerchantOrder(
                            "GF-261107-6689",
                            demoMerchant.Id,
                            null,
                            "Vũ Ngọc Ánh",
                            "0966889900",
                            "Hà Nội",
                            "Quận Ba Đình",
                            "Số 54 Liễu Giai, Phường Cống Vị",
                            "Cần giao hỏa tốc kịp bữa tiệc tối nay ạ!",
                            true,
                            "Chúc cô giáo 20/11 luôn mạnh khỏe, tràn đầy nhiệt huyết và niềm vui!",
                            true,
                            prodA.Price + prodB.Price,
                            50000,
                            30000,
                            0,
                            GiftFinder.Domain.Enums.ShippingMethodType.Express2H
                        );
                        o5.AddItem(prodA.Id, prodA.Name, prodA.ImageUrl, prodA.Price, 1, "Tiêu chuẩn");
                        o5.AddItem(prodB.Id, prodB.Name, prodB.ImageUrl, prodB.Price, 1, "Tiêu chuẩn");

                        context.MerchantOrders.AddRange(o1, o2, o3, o4, o5);
                        context.SaveChanges();

                        // Điều chỉnh CreatedAt của các đơn hàng mẫu rải đều trong 7 ngày qua để Biểu đồ tăng trưởng hiển thị đẹp mắt
                        context.Database.ExecuteSqlInterpolated($@"
                            UPDATE ""MerchantOrders"" SET ""CreatedAt"" = {utcNow.AddDays(-5)} WHERE ""Id"" = {o1.Id};
                            UPDATE ""MerchantOrders"" SET ""CreatedAt"" = {utcNow.AddDays(-3)} WHERE ""Id"" = {o2.Id};
                            UPDATE ""MerchantOrders"" SET ""CreatedAt"" = {utcNow.AddDays(-1)} WHERE ""Id"" = {o3.Id};
                            UPDATE ""MerchantOrders"" SET ""CreatedAt"" = {utcNow.AddHours(-3)} WHERE ""Id"" = {o4.Id};
                            UPDATE ""MerchantOrders"" SET ""CreatedAt"" = {utcNow.AddMinutes(-25)} WHERE ""Id"" = {o5.Id};
                        ");
                    }
                }
            }
            catch
            {
                // Bỏ qua nếu đang chạy môi trường không hỗ trợ raw sql
            }
        }
    }
}
