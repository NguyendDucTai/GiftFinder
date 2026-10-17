using System.Text;
using GiftFinder.Application;
using GiftFinder.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        Console.WriteLine($"\n[LỖI] Không tìm thấy file: {filePath}\n");
        return;
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

app.Run();
