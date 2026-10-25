using MediatR;

namespace GiftFinder.Application.Features.Products.Commands.SyncTrackedProductPrices;

/// <summary>
/// Command quét và đồng bộ giá thủ công ƯU TIÊN cho các sản phẩm ĐANG CÓ NGƯỜI THEO DÕI (UC-08 & UC-22)
/// </summary>
public class SyncTrackedProductPricesCommand : IRequest<SyncTrackedProductPricesResultDto>
{
    /// <summary>
    /// Cho phép cào cưỡng bức bỏ qua delay (dùng khi test)
    /// </summary>
    public bool FastMode { get; set; } = false;
}
