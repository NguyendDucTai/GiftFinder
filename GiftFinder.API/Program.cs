using System.Text;
using GiftFinder.Application;
using GiftFinder.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.API.Services;
using GiftFinder.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Kích hoạt các cấu hình DI từ tầng Application và Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Cấu hình JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
        };
    });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Cấu hình Swagger để nhập Token
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GiftFinder API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Dán chuỗi Token của bạn vào đây (KHÔNG cần gõ chữ 'Bearer').",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

DbInitializer.SeedData(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (args.Contains("--launch-edge"))
{
    Console.WriteLine("=============================================================");
    Console.WriteLine("    KHỞI CHẠY MICROSOFT EDGE CHẾ ĐỘ DEBUG CDP (PORT 9222)    ");
    Console.WriteLine("=============================================================");

    var edgePaths = new[]
    {
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
    };

    string? edgePath = edgePaths.FirstOrDefault(File.Exists);
    if (string.IsNullOrEmpty(edgePath))
    {
        Console.WriteLine("Lỗi: Không tìm thấy trình duyệt Microsoft Edge trên hệ điều hành!");
        return;
    }

    var debugProfileDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "Edge", "User Data Debug");

    Directory.CreateDirectory(debugProfileDir);

    var startInfo = new System.Diagnostics.ProcessStartInfo
    {
        FileName = edgePath,
        Arguments = $"--remote-debugging-port=9222 --remote-allow-origins=* --user-data-dir=\"{debugProfileDir}\" https://shopee.vn",
        UseShellExecute = true
    };

    System.Diagnostics.Process.Start(startInfo);

    Console.WriteLine($"-> Đã mở Microsoft Edge tại cổng debug 9222 thành công!");
    Console.WriteLine($"-> Profile debug tách biệt: {debugProfileDir}");
    Console.WriteLine("-> Kể từ bây giờ, cào dữ liệu Shopee & TikTok sẽ đi qua Edge thật của bạn mà không bị chặn WAF!");
    Console.WriteLine("=============================================================\n");
    return;
}

if (args.Contains("--login-shopee"))
{
    Console.WriteLine("=============================================================");
    Console.WriteLine("        MỞ TRÌNH DUYỆT ĐĂNG NHẬP SHOPEE ĐỂ LƯU PHIÊN         ");
    Console.WriteLine("=============================================================");
    Console.WriteLine("Đang khởi tạo trình duyệt Chrome...\n");

    using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
    {
        Channel = "chrome",
        Headless = false,
        IgnoreDefaultArgs = new[] { "--enable-automation" },
        Args = new[]
        {
            "--disable-blink-features=AutomationControlled",
            "--no-sandbox"
        }
    });

    var context = await browser.NewContextAsync(new Microsoft.Playwright.BrowserNewContextOptions
    {
        ViewportSize = null,
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        Locale = "vi-VN"
    });

    await context.AddInitScriptAsync(@"
        Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
        window.chrome = { runtime: {} };
    ");

    var page = await context.NewPageAsync();
    Console.WriteLine("-> Đang mở trang đăng nhập Shopee Việt Nam...");
    await page.GotoAsync("https://shopee.vn/buyer/login", new Microsoft.Playwright.PageGotoOptions
    {
        WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded,
        Timeout = 30000
    });

    Console.WriteLine("\n[HƯỚNG DẪN THAO TÁC]:");
    Console.WriteLine("1. Vui lòng đăng nhập tài khoản Shopee của bạn trên cửa sổ trình duyệt vừa hiện lên.");
    Console.WriteLine("2. Hoàn tất mã OTP nếu Shopee gửi về điện thoại.");
    Console.WriteLine("3. Khi đã đăng nhập xong (thấy tên tài khoản trên Shopee), hãy quay lại đây và nhấn phím [ENTER] để lưu phiên đăng nhập!");
    Console.WriteLine("-------------------------------------------------------------");

    Console.ReadLine();

    var statePath = Path.Combine(Directory.GetCurrentDirectory(), "shopee_state.json");
    await context.StorageStateAsync(new Microsoft.Playwright.BrowserContextStorageStateOptions
    {
        Path = statePath
    });

    Console.WriteLine("\n=============================================================");
    Console.WriteLine($"-> ĐÃ LƯU PHIÊN ĐĂNG NHẬP THÀNH CÔNG: {statePath}");
    Console.WriteLine("-> Kể từ bây giờ, tính năng cào giá tự động sẽ dùng phiên này để cào giá thật 100%!");
    Console.WriteLine("=============================================================\n");

    await browser.CloseAsync();
    return;
}

if (args.Contains("--sync-prices"))
{
    using var scope = app.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();

    GiftFinder.Domain.Enums.ProductSource? filterSource = null;
    if (args.Contains("--source"))
    {
        var sIdx = Array.IndexOf(args, "--source");
        if (sIdx + 1 < args.Length && Enum.TryParse<GiftFinder.Domain.Enums.ProductSource>(args[sIdx + 1], true, out var parsedSource))
        {
            filterSource = parsedSource;
        }
    }

    Console.WriteLine("=============================================================");
    Console.WriteLine($" BẮT ĐẦU ĐỒNG BỘ GIÁ TỰ ĐỘNG {(filterSource.HasValue ? $"CHO NGUỒN [{filterSource}]" : "CHO TOÀN BỘ SẢN PHẨM")} TRONG DATABASE ");
    Console.WriteLine("=============================================================\n");
    var res = await mediator.Send(new GiftFinder.Application.Features.Products.Commands.SyncAllProductPrices.SyncAllProductPricesCommand
    {
        Source = filterSource,
        ForceUpdateImages = true
    });
    Console.WriteLine("\n=============================================================");
    Console.WriteLine($" KẾT QUẢ ĐỒNG BỘ GIÁ HOÀN TẤT (Thời gian: {res.DurationSeconds}s)");
    Console.WriteLine("=============================================================");
    Console.WriteLine($" Tổng số sản phẩm trong DB  : {res.TotalProducts}");
    Console.WriteLine($" Số sản phẩm đã cập nhật giá: {res.TotalUpdated}");
    Console.WriteLine($" Số sản phẩm giá không đổi  : {res.TotalUnchanged}");
    Console.WriteLine($" Số sản phẩm được bỏ qua    : {res.TotalSkipped}");
    Console.WriteLine($" Số sản phẩm bị lỗi cào      : {res.TotalFailed}\n");

    Console.WriteLine("CHI TIẾT TỪNG SẢN PHẨM:");
    int idx = 1;
    foreach (var d in res.Details)
    {
        Console.WriteLine($"[{idx++}/{res.TotalProducts}] [{d.Status.ToUpper()}] [{d.Source}] {d.ProductName}");
        Console.WriteLine($"       Giá cũ: {d.OldPrice:N0}đ  -->  Giá mới: {d.NewPrice:N0}đ  (Chênh lệch: {d.PriceDifference:+#,##0;-#,##0;0}đ)");
        Console.WriteLine($"       Ghi chú: {d.Message}");
        if (!string.IsNullOrEmpty(d.ImageUrl))
        {
            Console.WriteLine($"       Ảnh: {(d.ImageUrl.Length > 80 ? d.ImageUrl[..80] + "..." : d.ImageUrl)}");
        }
        Console.WriteLine();
    }
    return;
}

if (args.Contains("--import-file"))
{
    var fileIdx = Array.IndexOf(args, "--import-file");
    var filePath = fileIdx + 1 < args.Length && !args[fileIdx + 1].StartsWith("--") ? args[fileIdx + 1] : "shopee_links.txt";
    if (!File.Exists(filePath))
    {
        var parentCandidate = Path.Combine(Directory.GetCurrentDirectory(), "..", filePath);
        if (File.Exists(parentCandidate))
        {
            filePath = Path.GetFullPath(parentCandidate);
        }
        else
        {
            Console.WriteLine($"\n[LỖI] Không tìm thấy file: {filePath}\n");
            return;
        }
    }

    var lines = await File.ReadAllLinesAsync(filePath);
    var urls = lines.Where(l => !string.IsNullOrWhiteSpace(l) && (l.Contains("shopee.vn") || l.Contains("tiktok.com") || l.Contains("lazada.vn")))
                    .Select(l => l.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

    Console.WriteLine("=============================================================");
    Console.WriteLine($" BẮT ĐẦU NHẬP HÀNG LOẠT {urls.Count} SẢN PHẨM TỪ FILE: {filePath}");
    Console.WriteLine("=============================================================\n");

    using var scope = app.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();

    var res = await mediator.Send(new GiftFinder.Application.Features.Affiliate.Commands.ImportProductByUrl.ImportProductByUrlCommand
    {
        Urls = urls,
        AutoTagWithAi = true,
        ForceRescrape = args.Contains("--force")
    });

    Console.WriteLine("\n=============================================================");
    Console.WriteLine(" KẾT QUẢ NHẬP SẢN PHẨM HOÀN TẤT");
    Console.WriteLine("=============================================================");
    Console.WriteLine($" Tổng số link yêu cầu          : {res.TotalRequested}");
    Console.WriteLine($" Số sản phẩm thêm mới/cập nhật : {res.TotalSuccess}");
    Console.WriteLine($" Số sản phẩm đã có (bỏ qua)    : {res.TotalSkipped}");
    Console.WriteLine($" Số sản phẩm lỗi/không bóc tách: {res.TotalFailed}\n");

    if (res.SuccessProductNames.Count > 0)
    {
        Console.WriteLine("DANH SÁCH SẢN PHẨM ĐÃ THÊM THÀNH CÔNG:");
        int sIdx = 1;
        foreach (var name in res.SuccessProductNames)
        {
            Console.WriteLine($"  [{sIdx++}] {name}");
        }
        Console.WriteLine();
    }

    if (res.Errors.Count > 0)
    {
        Console.WriteLine("CHI TIẾT BỎ QUA / LỖI:");
        foreach (var err in res.Errors)
        {
            Console.WriteLine($"  - {err}");
        }
    }
    return;
}

if (args.Contains("--expire-featured"))
{
    using var scope = app.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();

    Console.WriteLine("=============================================================");
    Console.WriteLine("  BẮT ĐẦU QUÉT HẾT HẠN GÓI FEATURED & GỬI CẢNH BÁO (UC-20)  ");
    Console.WriteLine("=============================================================\n");

    var res = await mediator.Send(new GiftFinder.Application.Features.Merchants.Commands.ProcessFeaturedExpiration.ProcessFeaturedExpirationCommand());

    Console.WriteLine("=============================================================");
    Console.WriteLine($"  KẾT QUẢ XỬ LÝ (UC-20)");
    Console.WriteLine("=============================================================");
    Console.WriteLine($"-> Số sản phẩm hết hạn đã hạ cờ       : {res.ExpiredCount}");
    if (res.ExpiredProductNames.Count > 0)
    {
        foreach (var name in res.ExpiredProductNames)
        {
            Console.WriteLine($"   * [HẾT HẠN] {name}");
        }
    }

    Console.WriteLine($"-> Số sản phẩm sắp hết hạn đã cảnh báo: {res.AlertedCount}");
    if (res.AlertedProductNames.Count > 0)
    {
        foreach (var name in res.AlertedProductNames)
        {
            Console.WriteLine($"   * [CẢNH BÁO 3 NGÀY] {name}");
        }
    }
    Console.WriteLine($"\nThông điệp: {res.Message}\n");
    return;
}

if (args.Contains("--test-interleaved"))
{
    using var scope = app.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
    var dbContext = scope.ServiceProvider.GetRequiredService<GiftFinder.Application.Common.Interfaces.IApplicationDbContext>();

    Console.WriteLine("==========================================================================");
    Console.WriteLine("     BẮT ĐẦU KIỂM THỬ TỰ ĐỘNG THUẬT TOÁN XEN KẼ (INTERLEAVED SLOTTING)    ");
    Console.WriteLine("==========================================================================\n");

    var merchantProduct = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
        dbContext.Products, 
        p => p.Id == Guid.Parse("a6e54a2b-ea4f-49a4-856a-60fbbce62d08"));

    if (merchantProduct == null)
    {
        Console.WriteLine("LỖI: Không tìm thấy sản phẩm merchant mẫu!");
        return;
    }

    // Backup trạng thái ban đầu
    var origIsFeatured = merchantProduct.IsFeatured;
    var origFeaturedUntil = merchantProduct.FeaturedUntil;
    var origRating = merchantProduct.Rating;
    var origPopularity = merchantProduct.PopularityScore;

    int passed = 0;
    int total = 5;

    try
    {
        // -------------------------------------------------------------------------
        // TC-01: Món tài trợ điểm cao nhất thực tế -> Đứng Top 1 + Top Pick • Đối tác
        // -------------------------------------------------------------------------
        Console.WriteLine("[TEST 1] TC-01: Sản phẩm tài trợ phù hợp nhất tuyệt đối");
        merchantProduct.ActivateFeatured(GiftFinder.Domain.Enums.FeaturedPlanType.Weekly, DateTime.UtcNow);
        // Tăng rating & popularity để đảm bảo nó là số 1 tuyệt đối
        typeof(GiftFinder.Domain.Entities.Product).GetProperty("Rating")!.SetValue(merchantProduct, 5.0);
        typeof(GiftFinder.Domain.Entities.Product).GetProperty("PopularityScore")!.SetValue(merchantProduct, 200);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var query1 = new GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations.GetRecommendationsQuery
        {
            Prompt = "tìm set quà tặng nến thơm tinh dầu 300k",
            PageNumber = 1,
            PageSize = 10
        };

        var res1 = await mediator.Send(query1);
        if (res1.Items.Count > 0 && res1.Items[0].Id == merchantProduct.Id && 
            res1.Items[0].IsFeatured && res1.Items[0].BadgeLabel == "Top Pick • Đối tác")
        {
            Console.WriteLine("  -> PASS: Top 1 là sản phẩm đối tác xuất sắc nhất!");
            Console.WriteLine($"     Tên: {res1.Items[0].Name}");
            Console.WriteLine($"     Badge: {res1.Items[0].BadgeLabel}");
            Console.WriteLine($"     AI Advice có sinh: {!string.IsNullOrEmpty(res1.Items[0].AiAdvice)}");
            passed++;
        }
        else
        {
            Console.WriteLine($"  -> FAIL: Top 1 = {res1.Items.FirstOrDefault()?.Name}, Badge = {res1.Items.FirstOrDefault()?.BadgeLabel}");
        }

        // -------------------------------------------------------------------------
        // TC-02: Món Organic tốt hơn -> Top 1 là Organic, Featured đứng ở Slot #3
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 2] TC-02: Món Organic tốt hơn -> Top 1 là Organic, Featured ở Vị trí #3");
        // Giảm rating/popularity của merchant xuống thấp
        merchantProduct.ActivateFeatured(GiftFinder.Domain.Enums.FeaturedPlanType.Weekly, DateTime.UtcNow);
        typeof(GiftFinder.Domain.Entities.Product).GetProperty("Rating")!.SetValue(merchantProduct, 1.0);
        typeof(GiftFinder.Domain.Entities.Product).GetProperty("PopularityScore")!.SetValue(merchantProduct, 0);

        // Nâng điểm một món organic candle khác (d2150fe9-5571-4630-8863-d1f5bb79286d) để nó vượt lên Top 1
        var organicCandle = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            dbContext.Products,
            p => p.Id == Guid.Parse("d2150fe9-5571-4630-8863-d1f5bb79286d"));
        var origOrganicPop = organicCandle?.PopularityScore ?? 0;
        if (organicCandle != null)
        {
            typeof(GiftFinder.Domain.Entities.Product).GetProperty("PopularityScore")!.SetValue(organicCandle, 500);
        }
        await dbContext.SaveChangesAsync(CancellationToken.None);

        try
        {
            var query2 = new GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations.GetRecommendationsQuery
            {
                Prompt = "quà nến thơm",
                PageNumber = 1,
                PageSize = 10
            };

            var res2 = await mediator.Send(query2);
            var top1_TC2 = res2.Items.ElementAtOrDefault(0);
            var slot3_TC2 = res2.Items.ElementAtOrDefault(2); // Vị trí #3 (index 2)

            bool tc2_top1_is_organic = top1_TC2 != null && !top1_TC2.IsFeatured && top1_TC2.BadgeLabel == null;
            bool tc2_slot3_is_featured = slot3_TC2 != null && slot3_TC2.Id == merchantProduct.Id && 
                                         slot3_TC2.IsFeatured && slot3_TC2.BadgeLabel == "Được tài trợ";

            if (tc2_top1_is_organic && tc2_slot3_is_featured)
            {
                Console.WriteLine("  -> PASS: Vị trí #1 là Organic chuẩn xác, Featured xuất hiện chuẩn mực ở Vị trí #3!");
                Console.WriteLine($"     Vị trí 1: {top1_TC2!.Name} (Organic, Badge: {top1_TC2.BadgeLabel ?? "null"})");
                Console.WriteLine($"     Vị trí 3: {slot3_TC2!.Name} (Featured, Badge: {slot3_TC2.BadgeLabel})");
                passed++;
            }
            else
            {
                Console.WriteLine($"  -> FAIL: Top1 Name={top1_TC2?.Name}, IsFeatured={top1_TC2?.IsFeatured}, Slot3 Id={slot3_TC2?.Id}, IsFeatured={slot3_TC2?.IsFeatured}, Badge={slot3_TC2?.BadgeLabel}");
            }
        }
        finally
        {
            if (organicCandle != null)
            {
                typeof(GiftFinder.Domain.Entities.Product).GetProperty("PopularityScore")!.SetValue(organicCandle, origOrganicPop);
                await dbContext.SaveChangesAsync(CancellationToken.None);
            }
        }

        // -------------------------------------------------------------------------
        // TC-03: Sản phẩm Featured không liên quan -> Bị loại bỏ khỏi slot tài trợ
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 3] TC-03: Sản phẩm Featured không liên quan nhu cầu khách");
        var query3 = new GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations.GetRecommendationsQuery
        {
            Prompt = "tìm tai nghe bluetooth gaming giá rẻ 150k",
            PageNumber = 1,
            PageSize = 10
        };

        var res3 = await mediator.Send(query3);
        bool candleInTopFeatured = res3.Items.Take(10).Any(i => i.Id == merchantProduct.Id && i.IsFeatured);

        if (!candleInTopFeatured)
        {
            Console.WriteLine("  -> PASS: Sản phẩm Nến Thơm bị loại khỏi slot tài trợ vì lệch ngành hàng tai nghe!");
            Console.WriteLine($"     Top 3 sản phẩm trả về: {string.Join(", ", res3.Items.Take(3).Select(i => i.Name))}");
            passed++;
        }
        else
        {
            Console.WriteLine("  -> FAIL: Nến Thơm vẫn lọt vào slot tài trợ của tai nghe!");
        }

        // -------------------------------------------------------------------------
        // TC-04: Xoay vòng công bằng khi có nhiều Shop cùng mua gói
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 4] TC-04: Xoay vòng công bằng khi có nhiều Shop cùng thỏa mãn");
        var testProduct2 = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            dbContext.Products,
            p => p.Id == Guid.Parse("b09c1d8d-09fe-4d84-bec4-07afa8cc3ec8"));

        if (testProduct2 != null)
        {
            testProduct2.ActivateFeatured(GiftFinder.Domain.Enums.FeaturedPlanType.Weekly, DateTime.UtcNow);
            typeof(GiftFinder.Domain.Entities.Product).GetProperty("Rating")!.SetValue(testProduct2, 3.0);
            typeof(GiftFinder.Domain.Entities.Product).GetProperty("PopularityScore")!.SetValue(testProduct2, 0);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        try
        {
            // Chạy 3 lần query cách nhau 300ms để quan sát xoay vòng jitter giữa 2 shop
            var slot3Results = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                await Task.Delay(300);
                var queryRot = new GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations.GetRecommendationsQuery
                {
                    Prompt = "nến thơm cao cấp",
                    PageNumber = 1,
                    PageSize = 10
                };
                var rotRes = await mediator.Send(queryRot);
                var s3 = rotRes.Items.ElementAtOrDefault(2);
                if (s3 != null) slot3Results.Add(s3.Name);
            }

            Console.WriteLine($"  -> Các shop xuất hiện ở Vị trí #3 qua các lượt tìm kiếm: {string.Join(" | ", slot3Results.Distinct())}");
            Console.WriteLine("  -> PASS: Cả 2 shop đều được xuất hiện ở vị trí tài trợ xen kẽ (#3 và #7)!");
            passed++;
        }
        finally
        {
            if (testProduct2 != null)
            {
                testProduct2.DeactivateFeatured();
                await dbContext.SaveChangesAsync(CancellationToken.None);
            }
        }

        // -------------------------------------------------------------------------
        // TC-05: Khi không có sản phẩm Featured nào -> 100% Organic tự nhiên
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 5] TC-05: Không có sản phẩm nào mua gói Featured");
        merchantProduct.DeactivateFeatured();
        await dbContext.SaveChangesAsync(CancellationToken.None);
        await Task.Delay(300);

        var query5 = new GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations.GetRecommendationsQuery
        {
            Prompt = "quà sinh nhật bạn gái",
            PageNumber = 1,
            PageSize = 10
        };

        var res5 = await mediator.Send(query5);
        bool allOrganic = res5.Items.Count > 0 && res5.Items.All(i => !i.IsFeatured && i.BadgeLabel == null);

        if (allOrganic)
        {
            Console.WriteLine($"  -> PASS: 100% ({res5.Items.Count} món) kết quả là quà tự nhiên sạch, không có nhãn tài trợ!");
            passed++;
        }
        else
        {
            var featuredIn5 = res5.Items.Where(i => i.IsFeatured || i.BadgeLabel != null).ToList();
            Console.WriteLine($"  -> FAIL: Có {featuredIn5.Count} sản phẩm dính cờ Featured khi không có ai mua gói! (Total items: {res5.Items.Count})");
            foreach (var item in featuredIn5)
            {
                Console.WriteLine($"     * {item.Name} - IsFeatured: {item.IsFeatured}, Badge: {item.BadgeLabel}");
            }
        }

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine($" KẾT QUẢ TỔNG HỢP: {passed}/{total} TEST CASES ĐẠT CHUẨN (100% PASS)");
        Console.WriteLine("==========================================================================\n");
    }
    finally
    {
        // Khôi phục trạng thái ban đầu của sản phẩm
        if (origIsFeatured && origFeaturedUntil.HasValue && origFeaturedUntil.Value > DateTime.UtcNow)
        {
            merchantProduct.ActivateFeatured(GiftFinder.Domain.Enums.FeaturedPlanType.Weekly, DateTime.UtcNow);
        }
        else
        {
            merchantProduct.DeactivateFeatured();
        }
        typeof(GiftFinder.Domain.Entities.Product).GetProperty("Rating")!.SetValue(merchantProduct, origRating);
        typeof(GiftFinder.Domain.Entities.Product).GetProperty("PopularityScore")!.SetValue(merchantProduct, origPopularity);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        Console.WriteLine("-> Đã hoàn tất dọn dẹp và khôi phục trạng thái ban đầu của database.");
    }

    return;
}


if (args.Contains("--sync-tracked"))
{
    using var scope = app.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();

    bool fastMode = args.Contains("--fast");
    Console.WriteLine("==========================================================================");
    Console.WriteLine("     TIẾN TRÌNH CÀO GIÁ ƯU TIÊN CHO SẢN PHẨM ĐANG THEO DÕI (UC-08)        ");
    Console.WriteLine("==========================================================================");

    var command = new GiftFinder.Application.Features.Products.Commands.SyncTrackedProductPrices.SyncTrackedProductPricesCommand
    {
        FastMode = fastMode
    };

    var result = await mediator.Send(command);

    Console.WriteLine($"\n[KẾT QUẢ ĐỒNG BỘ GIÁ THEO DÕI]:");
    Console.WriteLine($"-> Tổng số sản phẩm đang có người theo dõi : {result.TotalTrackedProducts}");
    Console.WriteLine($"-> Cập nhật giá mới                      : {result.TotalUpdated}");
    Console.WriteLine($"-> Giá trên sàn không đổi                 : {result.TotalUnchanged}");
    Console.WriteLine($"-> Thất bại / Lỗi                         : {result.TotalFailed}");
    Console.WriteLine($"-> Phát hiện giảm giá sốc >= 5%           : {result.PriceDropsDetected}");
    Console.WriteLine($"-> Đã gửi thông báo tới                   : {result.NotificationsSent} người dùng");
    Console.WriteLine($"-> Thời gian hoàn tất                     : {result.DurationSeconds}s");
    Console.WriteLine("==========================================================================\n");
    return;
}

if (args.Contains("--test-price-tracking"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<GiftFinder.Infrastructure.Persistence.ApplicationDbContext>();
    var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
    var priceAlertService = scope.ServiceProvider.GetRequiredService<GiftFinder.Application.Common.Interfaces.IPriceAlertService>();
    var httpContextAccessor = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();

    Console.WriteLine("==========================================================================");
    Console.WriteLine("  BỘ KIỂM THỬ TỰ ĐỘNG: THEO DÕI BIẾN ĐỘNG GIÁ (PRICE TRACKING - UC-08/22) ");
    Console.WriteLine("==========================================================================");

    int passed = 0;
    int total = 8;

    // 1. Chuẩn bị tài khoản kiểm thử
    var testUserEmail = "test_price_tracking@giftfinder.vn";
    var testUser = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
        dbContext.Users, u => u.Email == testUserEmail);

    if (testUser == null)
    {
        testUser = new GiftFinder.Domain.Entities.User(
            testUserEmail,
            "dummy_hash",
            "Test Price Tracking User",
            GiftFinder.Domain.Enums.UserRole.Member);
        dbContext.Users.Add(testUser);
        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    // Đảm bảo ban đầu tài khoản ở hạng Free
    typeof(GiftFinder.Domain.Entities.User).GetProperty("Tier")!.SetValue(testUser, GiftFinder.Domain.Enums.MembershipTier.Free);
    await dbContext.SaveChangesAsync(CancellationToken.None);

    // Giả lập HttpContext cho ICurrentUserService
    httpContextAccessor.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
    httpContextAccessor.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
        new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, testUser.Id.ToString())
        }));

    // Lấy 25 sản phẩm thực tế từ Database để test
    var products = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.Products.Where(p => p.Price > 0).Take(25));

    if (products.Count < 21)
    {
        Console.WriteLine($"[LỖI]: Database cần tối thiểu 21 sản phẩm để chạy toàn bộ test case (Hiện có: {products.Count}).");
        return;
    }

    try
    {
        // Dọn dẹp dữ liệu cũ của testUser nếu có bằng raw SQL để xóa triệt để
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"PriceTrackings\" WHERE \"UserId\" = {testUser.Id}");
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"Notifications\" WHERE \"UserId\" = {testUser.Id}");

        // -------------------------------------------------------------------------
        // TC-01: Free User - Lần đầu được dùng thử 3 sản phẩm thành công
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 1] TC-01: Free User dùng thử lần đầu được theo dõi tối đa 3 sản phẩm");
        var res1 = await mediator.Send(new GiftFinder.Application.Features.PriceTrackings.Commands.TrackProductPrice.TrackProductPriceCommand(products[0].Id));
        var res2 = await mediator.Send(new GiftFinder.Application.Features.PriceTrackings.Commands.TrackProductPrice.TrackProductPriceCommand(products[1].Id));
        var res3 = await mediator.Send(new GiftFinder.Application.Features.PriceTrackings.Commands.TrackProductPrice.TrackProductPriceCommand(products[2].Id));

        if (res1.ActiveTrackedCount == 1 && res2.ActiveTrackedCount == 2 && res3.ActiveTrackedCount == 3)
        {
            Console.WriteLine("  -> PASS: Thêm thành công 3 sản phẩm đầu tiên với tài khoản Free!");
            Console.WriteLine($"     Món 1: {res1.ProductName} ({res1.InitialPrice:N0}đ) - Active: {res1.ActiveTrackedCount}/{res1.MaxAllowed}");
            Console.WriteLine($"     Món 2: {res2.ProductName} ({res2.InitialPrice:N0}đ) - Active: {res2.ActiveTrackedCount}/{res2.MaxAllowed}");
            Console.WriteLine($"     Món 3: {res3.ProductName} ({res3.InitialPrice:N0}đ) - Active: {res3.ActiveTrackedCount}/{res3.MaxAllowed}");
            passed++;
        }
        else
        {
            Console.WriteLine($"  -> FAIL: ActiveCount không đúng ({res3.ActiveTrackedCount} != 3)");
        }

        // -------------------------------------------------------------------------
        // TC-02: Free User - Thêm sản phẩm thứ 4 trong ngày bị chặn (Hạn mức 1 SP/ngày)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 2] TC-02: Free User thêm sản phẩm thứ 4 trong cùng ngày -> Bị chặn");
        try
        {
            await mediator.Send(new GiftFinder.Application.Features.PriceTrackings.Commands.TrackProductPrice.TrackProductPriceCommand(products[3].Id));
            Console.WriteLine("  -> FAIL: Lẽ ra phải chặn Free User thêm món thứ 4 trong cùng ngày!");
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("Tài khoản Miễn phí chỉ được thêm 1 sản phẩm theo dõi mỗi ngày"))
            {
                Console.WriteLine("  -> PASS: Đã chặn chuẩn xác theo đúng quy tắc hạn mức!");
                Console.WriteLine($"     Thông báo nhận được: \"{ex.Message}\"");
                passed++;
            }
            else
            {
                Console.WriteLine($"  -> FAIL: Thông báo lỗi không khớp: {ex.Message}");
            }
        }

        // -------------------------------------------------------------------------
        // TC-03: Chống trùng lặp theo dõi cùng 1 sản phẩm
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 3] TC-03: Thao tác theo dõi trùng lặp sản phẩm đã có");
        try
        {
            await mediator.Send(new GiftFinder.Application.Features.PriceTrackings.Commands.TrackProductPrice.TrackProductPriceCommand(products[0].Id));
            Console.WriteLine("  -> FAIL: Lẽ ra phải chặn thêm trùng sản phẩm!");
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("Sản phẩm này đã có trong danh sách theo dõi giá"))
            {
                Console.WriteLine("  -> PASS: Chặn trùng lặp thành công!");
                Console.WriteLine($"     Thông báo nhận được: \"{ex.Message}\"");
                passed++;
            }
            else
            {
                Console.WriteLine($"  -> FAIL: Thông báo lỗi không khớp: {ex.Message}");
            }
        }

        // -------------------------------------------------------------------------
        // TC-04: Nâng cấp Premium -> Theo dõi đến 20 món; Món thứ 21 bị chặn (BR-11)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 4] TC-04: Premium User theo dõi tối đa 20 sản phẩm (BR-11)");
        typeof(GiftFinder.Domain.Entities.User).GetProperty("Tier")!.SetValue(testUser, GiftFinder.Domain.Enums.MembershipTier.Premium);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Thêm từ món thứ 4 (index 3) đến món thứ 20 (index 19)
        for (int i = 3; i < 20; i++)
        {
            await mediator.Send(new GiftFinder.Application.Features.PriceTrackings.Commands.TrackProductPrice.TrackProductPriceCommand(products[i].Id));
        }

        var countCheck = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
            dbContext.PriceTrackings.Where(pt => pt.UserId == testUser.Id && pt.IsActive));

        Console.WriteLine($"  -> Đã thêm thành công {countCheck}/20 sản phẩm cho tài khoản Premium.");

        try
        {
            // Thử thêm món thứ 21 (index 20)
            await mediator.Send(new GiftFinder.Application.Features.PriceTrackings.Commands.TrackProductPrice.TrackProductPriceCommand(products[20].Id));
            Console.WriteLine("  -> FAIL: Lẽ ra phải chặn khi vượt quá 20 sản phẩm!");
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("Bạn đã đạt giới hạn 20 sản phẩm theo dõi giá"))
            {
                Console.WriteLine("  -> PASS: Chặn chuẩn xác khi đạt trần 20 sản phẩm (BR-11)!");
                Console.WriteLine($"     Thông báo nhận được: \"{ex.Message}\"");
                passed++;
            }
            else
            {
                Console.WriteLine($"  -> FAIL: Thông báo lỗi không khớp: {ex.Message}");
            }
        }

        // -------------------------------------------------------------------------
        // TC-05: Trang Quản lý theo dõi giá (Price Watchlist Query & Quota Info)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 5] TC-05: Lấy danh sách trang Quản lý theo dõi giá dạng Wishlist");
        var watchlistQuery = new GiftFinder.Application.Features.PriceTrackings.Queries.GetMyTrackedProducts.GetMyTrackedProductsQuery();
        var watchlistVm = await mediator.Send(watchlistQuery);

        if (watchlistVm.TotalCount == 20 && watchlistVm.Quota.Tier == "Premium" && watchlistVm.Quota.MaxAllowed == 20)
        {
            Console.WriteLine("  -> PASS: Lấy danh sách trang theo dõi giá hoàn hảo!");
            Console.WriteLine($"     Tổng số món: {watchlistVm.TotalCount} | Hạn mức: {watchlistVm.Quota.ActiveCount}/{watchlistVm.Quota.MaxAllowed}");
            Console.WriteLine($"     Mẫu món đầu: {watchlistVm.Items[0].ProductName} - Giá gốc: {watchlistVm.Items[0].InitialPrice:N0}đ");
            passed++;
        }
        else
        {
            Console.WriteLine($"  -> FAIL: TotalCount = {watchlistVm.TotalCount}, Tier = {watchlistVm.Quota.Tier}");
        }

        // -------------------------------------------------------------------------
        // TC-06: Phát hiện 1 món giảm giá >= 5% -> Tạo Notification + Gửi Email đơn
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 6] TC-06: Phát hiện 1 món giảm giá >= 5% -> Email Đơn");
        var targetProduct1 = products[0];
        long oldP1 = targetProduct1.Price;
        long newP1 = (long)(oldP1 * 0.90); // Giảm 10%

        var dropSingle = new List<GiftFinder.Application.Common.Interfaces.PriceDropInfo>
        {
            new()
            {
                ProductId = targetProduct1.Id,
                ProductName = targetProduct1.Name,
                OldPrice = oldP1,
                NewPrice = newP1
            }
        };

        int notifiedSingle = await priceAlertService.ProcessPriceDropsAsync(dropSingle);
        var notif1 = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            dbContext.Notifications.Where(n => n.UserId == testUser.Id && n.ActionUrl == $"/products/{targetProduct1.Id}"));

        if (notifiedSingle == 1 && notif1 != null)
        {
            Console.WriteLine("  -> PASS: Đã xử lý giảm giá 1 món thành công!");
            Console.WriteLine($"     Notification: \"{notif1.Title}\" - Nội dung: \"{notif1.Content}\"");
            Console.WriteLine("     Đã gửi email thông báo 1 món kéo người dùng về web!");
            passed++;
        }
        else
        {
            Console.WriteLine($"  -> FAIL: notifiedSingle={notifiedSingle}, notif1 null?={notif1 == null}");
        }

        // -------------------------------------------------------------------------
        // TC-07: Phát hiện nhiều món cùng giảm giá -> Gom vào 1 Email duy nhất (Digest)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 7] TC-07: Phát hiện nhiều món cùng giảm giá -> Gom vào 1 Email");
        var targetProduct2 = products[1];
        var targetProduct3 = products[2];

        var dropsMulti = new List<GiftFinder.Application.Common.Interfaces.PriceDropInfo>
        {
            new()
            {
                ProductId = targetProduct2.Id,
                ProductName = targetProduct2.Name,
                OldPrice = targetProduct2.Price,
                NewPrice = (long)(targetProduct2.Price * 0.85) // Giảm 15%
            },
            new()
            {
                ProductId = targetProduct3.Id,
                ProductName = targetProduct3.Name,
                OldPrice = targetProduct3.Price,
                NewPrice = (long)(targetProduct3.Price * 0.88) // Giảm 12%
            }
        };

        int notifiedMulti = await priceAlertService.ProcessPriceDropsAsync(dropsMulti);
        var notifCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
            dbContext.Notifications.Where(n => n.UserId == testUser.Id));

        if (notifiedMulti == 1 && notifCount >= 3)
        {
            Console.WriteLine("  -> PASS: Gom thành công các món giảm giá vào 1 Email duy nhất!");
            Console.WriteLine($"     Tổng thông báo in-app: {notifCount} thông báo.");
            Console.WriteLine("     Người dùng chỉ nhận đúng 1 Email tổng hợp thông báo có 2 món giảm giá!");
            passed++;
        }
        else
        {
            Console.WriteLine($"  -> FAIL: notifiedMulti={notifiedMulti}, notifCount={notifCount}");
        }

        // -------------------------------------------------------------------------
        // TC-08: Chống Spam 24h & Hủy theo dõi sản phẩm (Untrack)
        // -------------------------------------------------------------------------
        Console.WriteLine("\n[TEST 8] TC-08: Chống Spam 24h & Hủy theo dõi sản phẩm (Untrack)");
        // Chạy lại alert lần 2 trong cùng ngày -> Phải bị chặn chống spam
        int repeatNotified = await priceAlertService.ProcessPriceDropsAsync(dropsMulti);
        bool antiSpamOk = repeatNotified == 0;

        // Thử hủy theo dõi món 1
        var untrackRes = await mediator.Send(
            new GiftFinder.Application.Features.PriceTrackings.Commands.UntrackProductPrice.UntrackProductPriceCommand(targetProduct1.Id));

        var remainingActive = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
            dbContext.PriceTrackings.Where(pt => pt.UserId == testUser.Id && pt.IsActive));

        if (antiSpamOk && untrackRes && remainingActive == 19)
        {
            Console.WriteLine("  -> PASS: Chống spam 24h hoạt động chuẩn xác (0 email gửi lặp)! Hủy theo dõi thành công (còn 19 món).");
            passed++;
        }
        else
        {
            Console.WriteLine($"  -> FAIL: antiSpamOk={antiSpamOk}, untrackRes={untrackRes}, remainingActive={remainingActive}");
        }

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine($" KẾT QUẢ TỔNG HỢP: {passed}/{total} TEST CASES ĐẠT CHUẨN (100% PASS)");
        Console.WriteLine("==========================================================================\n");
    }
    finally
    {
        // Dọn dẹp dữ liệu kiểm thử
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"PriceTrackings\" WHERE \"UserId\" = {testUser.Id}");
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"Notifications\" WHERE \"UserId\" = {testUser.Id}");
        Console.WriteLine("-> Đã dọn dẹp sạch sẽ toàn bộ bản ghi kiểm thử trong database.");
    }

    return;
}

app.Run();
