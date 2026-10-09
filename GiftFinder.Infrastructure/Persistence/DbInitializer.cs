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
            // Để trống: Kho sản phẩm sẽ hoàn toàn do quản trị viên nhập link thực tế qua Swagger / API
        }
    }
}
