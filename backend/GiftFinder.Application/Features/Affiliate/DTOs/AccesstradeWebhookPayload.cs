using System.Text.Json.Serialization;

namespace GiftFinder.Application.Features.Affiliate.DTOs;

public class AccesstradeWebhookPayload
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("sub_id")]
    public string? SubId { get; set; }

    [JsonPropertyName("campaign_name")]
    public string? CampaignName { get; set; }

    [JsonPropertyName("platform")]
    public string? Platform { get; set; } // "shopee" | "tiktok"

    [JsonPropertyName("order_amount")]
    public long OrderAmount { get; set; }

    [JsonPropertyName("commission_amount")]
    public long CommissionAmount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending"; // pending, approved, rejected, cancelled

    [JsonPropertyName("ordered_at")]
    public DateTime? OrderedAt { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}
