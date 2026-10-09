using GiftFinder.Application.Features.Affiliate.DTOs;
using MediatR;

namespace GiftFinder.Application.Features.Affiliate.Commands.ProcessAccesstradeWebhook;

public class ProcessAccesstradeWebhookCommand : IRequest<WebhookProcessResultDto>
{
    public AccesstradeWebhookPayload Payload { get; set; } = new();
}

public class WebhookProcessResultDto
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long CommissionAmount { get; set; }
    public bool MatchedClickTracking { get; set; }
    public string Message { get; set; } = string.Empty;
}
