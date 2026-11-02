using System;
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
                        "GPKD-8888"
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

                // Đảm bảo Shop Demo có trạng thái Verified và bộ dữ liệu mẫu đa dạng (Approved, Featured, Pending, Rejected) nếu chưa có sản phẩm nào
                var demoMerchant = context.Merchants.FirstOrDefault(m => m.ContactEmail == "shopdemo@giftfinder.vn");
                if (demoMerchant != null)
                {
                    if (!demoMerchant.IsVerified)
                    {
                        demoMerchant.Verify();
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
                            "https://shopee.vn/set-qua-nen-thom-moc-vintage",
                            GiftFinder.Domain.Enums.ProductSource.DirectMerchant,
                            null,
                            "https://images.unsplash.com/photo-1603006905003-be475563bc59?auto=format&fit=crop&w=900&q=80",
                            550000,
                            demoMerchant.Id);
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
                            "https://shopee.vn/ly-gom-thu-cong-khac-ten",
                            GiftFinder.Domain.Enums.ProductSource.DirectMerchant,
                            null,
                            "https://images.unsplash.com/photo-1514228742587-6b1558fcca3d?auto=format&fit=crop&w=900&q=80",
                            390000,
                            demoMerchant.Id);
                        p2.SetDescription("Ly gốm Bát Tràng men hỏa biến chế tác thủ công độc bản, kèm đế lót gỗ thông và muỗng đồng cổ điển.");
                        p2.SetSuitableZodiacs(new[] { GiftFinder.Domain.Enums.ZodiacSign.Virgo, GiftFinder.Domain.Enums.ZodiacSign.Capricorn, GiftFinder.Domain.Enums.ZodiacSign.Cancer });
                        p2.Approve();
                        if (tagSinhNhat.HasValue) p2.AddTag(tagSinhNhat.Value);
                        if (tagDecor.HasValue) p2.AddTag(tagDecor.Value);

                        var p3 = new GiftFinder.Domain.Entities.Product(
                            "Bộ Khuếch Tán Tinh Dầu Hoa Hồng Pháp Cao Cấp 120ml",
                            580000,
                            "https://shopee.vn/bo-khuech-tan-tinh-dau-phap",
                            GiftFinder.Domain.Enums.ProductSource.DirectMerchant,
                            null,
                            "https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?auto=format&fit=crop&w=900&q=80",
                            680000,
                            demoMerchant.Id);
                        p3.SetDescription("Tinh dầu khuếch tán que mây lưu hương 60 ngày, thiết kế lọ thủy tinh pha lê sang trọng thích hợp tặng tân gia, sinh nhật.");
                        p3.SetSuitableZodiacs(new[] { GiftFinder.Domain.Enums.ZodiacSign.Leo, GiftFinder.Domain.Enums.ZodiacSign.Libra });
                        if (tagNenThom.HasValue) p3.AddTag(tagNenThom.Value);
                        if (tagBanGai.HasValue) p3.AddTag(tagBanGai.Value);

                        var p4 = new GiftFinder.Domain.Entities.Product(
                            "Set Trà Hoa Thảo Mộc Hữu Cơ & Mật Ong Rừng Tây Bắc",
                            285000,
                            "https://shopee.vn/set-tra-hoa-thao-moc-mat-ong",
                            GiftFinder.Domain.Enums.ProductSource.DirectMerchant,
                            null,
                            "https://images.unsplash.com/photo-1544787219-7f47ccb76574?auto=format&fit=crop&w=900&q=80",
                            350000,
                            demoMerchant.Id);
                        p4.SetDescription("Set 6 vị trà hoa dưỡng nhan kết hợp hũ mật ong rừng nguyên chất 150ml, món quà sức khỏe tinh tế cho người thân.");
                        p4.SetSuitableZodiacs(new[] { GiftFinder.Domain.Enums.ZodiacSign.Cancer, GiftFinder.Domain.Enums.ZodiacSign.Virgo });
                        p4.Reject("Vui lòng bổ sung thông tin hạn sử dụng và nguồn gốc xuất xứ trong phần mô tả chi tiết.");
                        if (tagSinhNhat.HasValue) p4.AddTag(tagSinhNhat.Value);

                        context.Products.AddRange(p1, p2, p3, p4);
                        context.SaveChanges();
                    }
                }
            }
            catch
            {
                // Bỏ qua nếu cột đã là text hoặc đang chạy môi trường không hỗ trợ raw sql
            }
        }
    }
}
