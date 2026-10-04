using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;

namespace GiftFinder.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static void SeedData(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            if (!context.Products.Any())
            {
                var mockProduct1 = new Product(
                    name: "Hộp quà sinh nhật (Nến thơm + Hoa khô)",
                    price: 250000,
                    originalUrl: "https://shopee.vn/hop-qua",
                    source: ProductSource.Shopee,
                    externalProductId: "MOCK001",
                    imageUrl: "https://picsum.photos/200",
                    originalPrice: 350000
                );
                mockProduct1.GetType().GetProperty("Status")!.SetValue(mockProduct1, ProductStatus.Approved);

                var mockProduct2 = new Product(
                    name: "Tai nghe Bluetooth chống ồn",
                    price: 750000,
                    originalUrl: "https://shopee.vn/tai-nghe",
                    source: ProductSource.Shopee,
                    externalProductId: "MOCK002",
                    imageUrl: "https://picsum.photos/200",
                    originalPrice: 990000
                );
                mockProduct2.GetType().GetProperty("Status")!.SetValue(mockProduct2, ProductStatus.Approved);

                context.Products.AddRange(mockProduct1, mockProduct2);
                context.SaveChanges();
            }
        }
    }
}
