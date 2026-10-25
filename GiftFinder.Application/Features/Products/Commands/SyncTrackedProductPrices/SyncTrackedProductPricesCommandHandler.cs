using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Products.Commands.SyncAllProductPrices;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Application.Features.Products.Commands.SyncTrackedProductPrices;

public class SyncTrackedProductPricesCommandHandler : IRequestHandler<SyncTrackedProductPricesCommand, SyncTrackedProductPricesResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IWebScraperService _scraperService;
    private readonly IPriceAlertService _priceAlertService;
    private readonly ILogger<SyncTrackedProductPricesCommandHandler> _logger;

    public SyncTrackedProductPricesCommandHandler(
        IApplicationDbContext context,
        IWebScraperService scraperService,
        IPriceAlertService priceAlertService,
        ILogger<SyncTrackedProductPricesCommandHandler> logger)
    {
        _context = context;
        _scraperService = scraperService;
        _priceAlertService = priceAlertService;
        _logger = logger;
    }

    public async Task<SyncTrackedProductPricesResultDto> Handle(SyncTrackedProductPricesCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = new SyncTrackedProductPricesResultDto();

        // 1. Chỉ lấy các ProductId ĐANG CÓ NGƯỜI BẤM THEO DÕI
        var trackedProductIds = await _context.PriceTrackings
            .Where(pt => pt.IsActive)
            .Select(pt => pt.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        result.TotalTrackedProducts = trackedProductIds.Count;

        if (trackedProductIds.Count == 0)
        {
            _logger.LogInformation("[SyncTracked] Không có sản phẩm nào đang được người dùng theo dõi giá.");
            sw.Stop();
            result.DurationSeconds = Math.Round(sw.Elapsed.TotalSeconds, 2);
            return result;
        }

        var products = await _context.Products
            .Where(p => trackedProductIds.Contains(p.Id) && p.Source != ProductSource.DirectMerchant)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("[SyncTracked] Bắt đầu cào giá ưu tiên cho {Count} sản phẩm có người theo dõi...", products.Count);

        var detectedPriceDrops = new List<PriceDropInfo>();

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

            var urlToScrape = !string.IsNullOrWhiteSpace(product.OriginalUrl) 
                ? product.OriginalUrl 
                : product.AffiliateUrl;

            if (string.IsNullOrWhiteSpace(urlToScrape))
            {
                detail.Status = "Failed";
                detail.Message = "Sản phẩm không có URL hợp lệ để đồng bộ.";
                result.TotalFailed++;
                result.Details.Add(detail);
                continue;
            }

            if (!request.FastMode)
            {
                if (product.Source == ProductSource.TikTokShop)
                {
                    await Task.Delay(Random.Shared.Next(3000, 4500), cancellationToken);
                }
                else if (product.Source == ProductSource.Shopee)
                {
                    await Task.Delay(Random.Shared.Next(2500, 3500), cancellationToken);
                }
            }

            try
            {
                _logger.LogInformation("[SyncTracked] Đang cào giá món theo dõi '{Name}' ({Source})...", product.Name, product.Source);
                var scraped = await _scraperService.ScrapeProductAsync(urlToScrape, cancellationToken);

                if (scraped.IsSuccess && scraped.Price > 0)
                {
                    long oldPrice = product.Price;
                    long newPrice = scraped.Price;
                    bool isPriceChanged = oldPrice != newPrice;

                    if (isPriceChanged)
                    {
                        product.UpdatePrice(newPrice);
                        detail.NewPrice = newPrice;
                        detail.Status = "Updated";
                        detail.Message = $"Cập nhật: {oldPrice:N0}đ -> {newPrice:N0}đ (chênh lệch: {detail.PriceDifference:+#,##0;-#,##0;0}đ).";
                        result.TotalUpdated++;

                        // Kiểm tra nếu giá giảm >= 5% so với giá cũ
                        if (newPrice < oldPrice)
                        {
                            double dropPercent = ((oldPrice - newPrice) / (double)oldPrice) * 100.0;
                            if (dropPercent >= 5.0)
                            {
                                detectedPriceDrops.Add(new PriceDropInfo
                                {
                                    ProductId = product.Id,
                                    ProductName = product.Name,
                                    OldPrice = oldPrice,
                                    NewPrice = newPrice
                                });
                                _logger.LogInformation("🔥 [SyncTracked] Phát hiện món '{Name}' giảm giá sốc {Percent:F1}% ({Old:N0}đ -> {New:N0}đ)!",
                                    product.Name, dropPercent, oldPrice, newPrice);
                            }
                        }
                    }
                    else
                    {
                        detail.Status = "Unchanged";
                        detail.Message = "Giá không thay đổi.";
                        result.TotalUnchanged++;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    detail.Status = "Failed";
                    detail.Message = !string.IsNullOrWhiteSpace(scraped.ErrorMessage) 
                        ? scraped.ErrorMessage 
                        : "Không tìm thấy giá hợp lệ trên sàn.";
                    result.TotalFailed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SyncTracked] Lỗi cào sản phẩm {Id}: {Message}", product.Id, ex.Message);
                detail.Status = "Failed";
                detail.Message = $"Lỗi ngoại lệ: {ex.Message}";
                result.TotalFailed++;
            }

            result.Details.Add(detail);
        }

        // 2. Kích hoạt xử lý thông báo giảm giá và gom Email
        if (detectedPriceDrops.Count > 0)
        {
            result.PriceDropsDetected = detectedPriceDrops.Count;
            result.NotificationsSent = await _priceAlertService.ProcessPriceDropsAsync(detectedPriceDrops, cancellationToken);
        }

        sw.Stop();
        result.DurationSeconds = Math.Round(sw.Elapsed.TotalSeconds, 2);
        _logger.LogInformation("[SyncTracked] Hoàn tất cào giá ưu tiên trong {Duration}s: Cập nhật={Updated}, Giữ nguyên={Unchanged}, Giảm giá sốc={Drops}, Đã gửi thông báo cho={Notified} người dùng.",
            result.DurationSeconds, result.TotalUpdated, result.TotalUnchanged, result.PriceDropsDetected, result.NotificationsSent);

        return result;
    }
}
