using MediatR;

namespace GiftFinder.Application.Features.Products.Commands.TrackAffiliateClick;

public class TrackAffiliateClickCommand : IRequest<string>
{
    public Guid ProductId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? UserId { get; set; } // Nullable, dùng cho trường hợp user đã đăng nhập
}
