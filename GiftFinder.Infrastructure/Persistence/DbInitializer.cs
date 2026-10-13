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
            }
            catch
            {
                // Bỏ qua nếu cột đã là text hoặc đang chạy môi trường không hỗ trợ raw sql
            }
        }
    }
}
