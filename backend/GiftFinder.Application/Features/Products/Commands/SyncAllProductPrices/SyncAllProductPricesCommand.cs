using GiftFinder.Domain.Enums;
using MediatR;

namespace GiftFinder.Application.Features.Products.Commands.SyncAllProductPrices;

/// <summary>
/// Command quét và đồng bộ lại giá và ảnh của các sản phẩm từ các sàn TMĐT về DB
/// </summary>
public class SyncAllProductPricesCommand : IRequest<SyncAllProductPricesResultDto>
{
    /// <summary>
    /// Lọc theo sàn nếu muốn chỉ quét 1 sàn (null = quét tất cả các sàn)
    /// </summary>
    public ProductSource? Source { get; set; }

    /// <summary>
    /// Có cập nhật lại ảnh gốc nếu ảnh cũ là ảnh tạm Unsplash hay không
    /// </summary>
    public bool ForceUpdateImages { get; set; } = true;

    /// <summary>
    /// Danh sách Id cụ thể cần quét lại (nếu null = quét theo Source)
    /// </summary>
    public List<Guid>? ProductIds { get; set; }
}
