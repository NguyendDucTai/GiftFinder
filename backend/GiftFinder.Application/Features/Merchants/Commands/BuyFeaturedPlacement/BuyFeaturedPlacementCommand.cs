using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Enums;
using MediatR;

namespace GiftFinder.Application.Features.Merchants.Commands.BuyFeaturedPlacement;

public class BuyFeaturedPlacementCommand : IRequest<BuyFeaturedPlacementResponseDto>
{
    public Guid ProductId { get; set; }
    public FeaturedPlanType PlanType { get; set; } = FeaturedPlanType.Weekly;
    public string? IpAddress { get; set; }
}
