using GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecommendationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RecommendationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Tìm kiếm và gợi ý quà tặng (Kết hợp AI Cung Hoàng Đạo)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRecommendations([FromQuery] GetRecommendationsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
