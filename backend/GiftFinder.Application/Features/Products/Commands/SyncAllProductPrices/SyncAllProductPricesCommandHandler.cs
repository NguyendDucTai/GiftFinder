using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Application.Features.Products.Commands.SyncAllProductPrices;

public class SyncAllProductPricesCommandHandler : IRequestHandler<SyncAllProductPricesCommand, SyncAllProductPricesResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IWebScraperService _scraperService;
    private readonly ILogger<SyncAllProductPricesCommandHandler> _logger;

    public SyncAllProductPricesCommandHandler(
        IApplicationDbContext context,
        IWebScraperService scraperService,
        ILogger<SyncAllProductPricesCommandHandler> logger)
    {
        _context = context;
        _scraperService = scraperService;
        _logger = logger;
    }

    public async Task<SyncAllProductPricesResultDto> Handle(SyncAllProductPricesCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = new SyncAllProductPricesResultDto();

        var query = _context.Products.AsQueryable();

        if (request.ProductIds != null && request.ProductIds.Count > 0)
        {
            query = query.Where(p => request.ProductIds.Contains(p.Id));
        }
        else if (request.Source.HasValue)
        {
            query = query.Where(p => p.Source == request.Source.Value);
        }

        var products = await query
            .OrderBy(p => p.Source)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        result.TotalProducts = products.Count;
        _logger.LogInformation("Bắt đầu tiến trình đồng bộ giá cho {Count} sản phẩm trong kho...", products.Count);

        foreach (var product in products)
        {
            var detail = new ProductSyncDetailDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Source = product.Source.ToString(),
                OldPrice = product.Price,
                NewPrice = product.Price,
                ImageUrl = product.ImageUrl
            };

            // 1. Phân loại sản phẩm nội bộ của Merchant -> Không quét sàn ngoài
            if (product.Source == ProductSource.DirectMerchant)
            {
                detail.Status = "Skipped";
                detail.Message = "Sản phẩm do Merchant tự quản lý giá, không cần scrape sàn ngoài.";
                result.TotalSkipped++;
                result.Details.Add(detail);
                continue;
            }

            // 2. Xác định URL cần cào dữ liệu
            var urlToScrape = !string.IsNullOrWhiteSpace(product.OriginalUrl) 
                ? product.OriginalUrl 
                : product.AffiliateUrl;

            if (string.IsNullOrWhiteSpace(urlToScrape))
            {
                detail.Status = "Failed";
                detail.Message = "Sản phẩm không có đường dẫn URL hợp lệ để đồng bộ.";
                result.TotalFailed++;
                result.Details.Add(detail);
                continue;
            }

            // Giãn cách giữa các request để hành vi cào tự nhiên như người dùng thật, tránh bị sàn chặn Captcha
            if (product.Source == ProductSource.Shopee)
            {
                // Shopee Fast Scrape (HTTP GET) cần một nhịp nghỉ nhẹ để không spam CDN
                await Task.Delay(Random.Shared.Next(1500, 2500), cancellationToken);
            }
            // TikTokShop đã được PlaywrightScraperService tự động điều phối cooldown an toàn (3s - 4.5s) ngay sau khi đóng tab,
            // không cần delay thêm ở đây để tránh bị cộng dồn thời gian chờ gấp đôi.

            try
            {
                _logger.LogInformation("[Sync] Đang cào giá sản phẩm '{Name}' ({Source}): {Url}", product.Name, product.Source, urlToScrape);
                var scraped = await _scraperService.ScrapeProductAsync(urlToScrape, cancellationToken);

                if (scraped.IsSuccess && scraped.Price > 0)
                {
                    bool isPriceChanged = product.Price != scraped.Price;
                    bool isImageUpdated = false;

                    if (isPriceChanged)
                    {
                        product.UpdatePrice(scraped.Price);
                        detail.NewPrice = scraped.Price;
                    }

                    if (product.Source == ProductSource.TikTokShop)
                    {
                        product.SetOriginalPrice(null);
                    }

                    if (request.ForceUpdateImages && !string.IsNullOrWhiteSpace(scraped.ImageUrl))
                    {
                        if (string.IsNullOrWhiteSpace(product.ImageUrl) || product.ImageUrl.Contains("unsplash.com"))
                        {
                            product.SetImageUrl(scraped.ImageUrl);
                            detail.ImageUrl = scraped.ImageUrl;
                            isImageUpdated = true;
                        }
                    }

                    if (isPriceChanged)
                    {
                        detail.Status = "Updated";
                        detail.Message = $"Cập nhật thành công: {detail.OldPrice:N0}đ -> {detail.NewPrice:N0}đ (chênh lệch: {detail.PriceDifference:+#,##0;-#,##0;0}đ).";
                        result.TotalUpdated++;
                    }
                    else
                    {
                        detail.Status = "Unchanged";
                        detail.Message = isImageUpdated 
                            ? "Giá không đổi, đã cập nhật ảnh gốc từ sàn." 
                            : "Giá trên sàn giữ nguyên, không thay đổi.";
                        result.TotalUnchanged++;
                    }

                    // Lưu thay đổi ngay cho sản phẩm này
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    detail.Status = "Failed";
                    detail.Message = !string.IsNullOrWhiteSpace(scraped.ErrorMessage) 
                        ? $"Không cào được giá: {scraped.ErrorMessage}" 
                        : "Không tìm thấy giá hợp lệ trên sàn, giữ nguyên giá cũ.";
                    result.TotalFailed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Sync] Lỗi khi đồng bộ sản phẩm {Id} ({Name}): {Message}", product.Id, product.Name, ex.Message);
                detail.Status = "Failed";
                detail.Message = $"Lỗi ngoại lệ: {ex.Message}";
                result.TotalFailed++;
            }

            result.Details.Add(detail);
        }

        sw.Stop();
        result.DurationSeconds = Math.Round(sw.Elapsed.TotalSeconds, 2);
        _logger.LogInformation("Hoàn tất đồng bộ giá trong {Duration}s: Cập nhật={Updated}, Giữ nguyên={Unchanged}, Bỏ qua={Skipped}, Lỗi={Failed}",
            result.DurationSeconds, result.TotalUpdated, result.TotalUnchanged, result.TotalSkipped, result.TotalFailed);

        return result;
    }
}
