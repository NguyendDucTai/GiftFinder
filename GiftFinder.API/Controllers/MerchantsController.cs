using GiftFinder.Application.Features.Merchants.Commands.BuyFeaturedPlacement;
using GiftFinder.Application.Features.Merchants.Commands.CancelFeaturedPlacement;
using GiftFinder.Application.Features.Merchants.Commands.CreateMerchantProduct;
using GiftFinder.Application.Features.Merchants.Commands.DeleteMerchantProduct;
using GiftFinder.Application.Features.Merchants.Commands.RegisterMerchant;
using GiftFinder.Application.Features.Merchants.Commands.SuggestProductTagsAi;
using GiftFinder.Application.Features.Merchants.Commands.UpdateMerchantProduct;
using GiftFinder.Application.Features.Merchants.Commands.UpdateMerchantProfile;
using GiftFinder.Application.Features.Merchants.Queries.GetMerchantDashboardStats;
using GiftFinder.Application.Features.Merchants.Queries.GetMerchantProductById;
using GiftFinder.Application.Features.Merchants.Queries.GetMerchantProfile;
using GiftFinder.Application.Features.Merchants.Queries.GetMyProducts;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MerchantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MerchantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// [UC-06] Nâng cấp tài khoản hiện tại từ Member lên Merchant (Mở gian hàng bán quà)
    /// </summary>
    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> Register([FromBody] RegisterMerchantCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Xem thông tin hồ sơ gian hàng của tài khoản Merchant đang đăng nhập
    /// </summary>
    [HttpGet("profile")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _mediator.Send(new GetMerchantProfileQuery());
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật thông tin hồ sơ gian hàng (Tên shop, SĐT, Email, Giấy phép)
    /// </summary>
    [HttpPut("profile")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateMerchantProfileCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// [UC-19] Báo cáo thống kê hiệu quả gian hàng (Tổng quà, Lượt click Zalo/Web, Quà Featured, Top quà hot)
    /// </summary>
    [HttpGet("dashboard-stats")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var result = await _mediator.Send(new GetMerchantDashboardStatsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Trợ lý AI đọc tên/mô tả quà để gợi ý Cung hoàng đạo & Tag Dịp/Đối tượng/Sở thích khi Shop đăng hoặc sửa quà tặng
    /// </summary>
    [HttpPost("ai-suggest-tags")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> SuggestTagsAi([FromBody] SuggestProductTagsAiCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// [UC-06] Đối tác đăng sản phẩm quà tặng mới trực tiếp lên GiftFinder (Trạng thái ban đầu: Pending)
    /// </summary>
    [HttpPost("products")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateMerchantProductCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách toàn bộ quà tặng của chính gian hàng mình (Hỗ trợ phân trang, lọc Pending / Approved / Rejected)
    /// </summary>
    [HttpGet("my-products")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> GetMyProducts(
        [FromQuery] ProductStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetMyProductsQuery
        {
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Xem chi tiết 1 sản phẩm quà tặng của gian hàng mình
    /// </summary>
    [HttpGet("products/{id}")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        var result = await _mediator.Send(new GetMerchantProductByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Chỉnh sửa thông tin quà tặng (Nếu sản phẩm đã Approved, sửa đổi sẽ tự động chuyển về Pending để Admin duyệt lại)
    /// </summary>
    [HttpPut("products/{id}")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateMerchantProductCommand command)
    {
        if (id != command.Id)
        {
            command.Id = id;
        }
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Xóa hoặc ẩn quà tặng khỏi gian hàng (Chuyển trạng thái sang Inactive)
    /// </summary>
    [HttpDelete("products/{id}")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var result = await _mediator.Send(new DeleteMerchantProductCommand(id));
        return Ok(new { Success = result, Message = "Đã ẩn sản phẩm thành công khỏi gian hàng." });
    }

    /// <summary>
    /// [UC-17] Đăng ký gói hiển thị nổi bật / Đẩy Top quà tặng lên hàng đầu (Gói tuần 49k hoặc Tháng 199k)
    /// </summary>
    [HttpPost("products/{id}/buy-featured")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> BuyFeatured(Guid id, [FromBody] BuyFeaturedRequest? request)
    {
        var command = new BuyFeaturedPlacementCommand
        {
            ProductId = id,
            PlanType = request?.PlanType ?? FeaturedPlanType.Weekly
        };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Hủy gói hiển thị nổi bật của sản phẩm trước hạn (khi shop hết hàng hoặc tạm ngưng ưu tiên)
    /// </summary>
    [HttpDelete("products/{id}/cancel-featured")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> CancelFeatured(Guid id)
    {
        var result = await _mediator.Send(new CancelFeaturedPlacementCommand(id));
        return Ok(result);
    }
}

public class BuyFeaturedRequest
{
    public FeaturedPlanType PlanType { get; set; } = FeaturedPlanType.Weekly;
}

