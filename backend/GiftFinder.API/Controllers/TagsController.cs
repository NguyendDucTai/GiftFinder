using GiftFinder.Application.Features.Tags.Queries.GetTags;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TagsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TagsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh mục Tags (Dịp tặng, Sở thích, Người nhận...) phục vụ vẽ các nút bấm chọn trên giao diện
    /// </summary>
    /// <param name="type">Lọc theo loại Tag (Tùy chọn: 0=Recipient, 1=Occasion, 2=Hobby, 3=Category, 4=Budget)</param>
    [HttpGet]
    public async Task<IActionResult> GetTags([FromQuery] TagType? type)
    {
        var result = await _mediator.Send(new GetTagsQuery { Type = type });
        return Ok(result);
    }
}
