using GiftFinder.Application.Features.Affiliate.Commands.ImportProductByUrl;
using GiftFinder.Application.Features.Affiliate.Commands.SyncAffiliateFeed;
using GiftFinder.Application.Features.Affiliate.Queries.GetAffiliateRevenueReport;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AffiliateController : ControllerBase
{
    private readonly IMediator _mediator;

    public AffiliateController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// [TRỤ CỘT CHÍNH] Đồng bộ sản phẩm quà tặng hàng loạt theo từ khóa từ Shopee hoặc TikTok Shop
    /// Tự động chạy Groq AI (120B) giám định chất lượng và gắn Tag Dịp, Sở thích, Cung hoàng đạo
    /// </summary>
    [HttpPost("sync-feed")]
    public async Task<IActionResult> SyncFeed([FromBody] SyncAffiliateFeedCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// [TRỤ CỘT PHỤ] Nhập quà tặng bằng đường link Shopee hoặc TikTok Shop tùy ý
    /// Bóc tách thông tin, tự sinh deeplink và dùng AI gắn Tag thông minh
    /// </summary>
    [HttpPost("import-by-url")]
    public async Task<IActionResult> ImportByUrl([FromBody] ImportProductByUrlCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Báo cáo doanh thu & hoa hồng Affiliate (Tổng Clicks, Đơn hàng, GMV, Tỷ lệ chuyển đổi %, Hoa hồng theo sàn)
    /// </summary>
    [HttpGet("revenue-report")]
    public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var query = new GetAffiliateRevenueReportQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        };
        var report = await _mediator.Send(query);
        return Ok(report);
    }
}
