using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.PriceTrackings.Queries.GetProductPriceHistory;

public class GetProductPriceHistoryQueryHandler : IRequestHandler<GetProductPriceHistoryQuery, ProductPriceHistoryVm>
{
    private readonly IApplicationDbContext _context;

    public GetProductPriceHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductPriceHistoryVm> Handle(GetProductPriceHistoryQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new ArgumentException("Sản phẩm không tồn tại.");

        var histories = await _context.PriceHistories
            .Where(ph => ph.ProductId == request.ProductId)
            .OrderBy(ph => ph.ChangedAt)
            .ToListAsync(cancellationToken);

        var points = histories.Select(ph => new PriceHistoryPointDto
        {
            OldPrice = ph.OldPrice,
            NewPrice = ph.NewPrice,
            ChangedAt = ph.ChangedAt
        }).ToList();

        long lowest = product.Price;
        long highest = product.Price;

        if (histories.Count > 0)
        {
            var allPrices = histories.Select(h => h.OldPrice).Concat(histories.Select(h => h.NewPrice)).ToList();
            lowest = allPrices.Min();
            highest = allPrices.Max();
        }

        return new ProductPriceHistoryVm
        {
            ProductId = product.Id,
            ProductName = product.Name,
            CurrentPrice = product.Price,
            LowestPrice = lowest,
            HighestPrice = highest,
            History = points
        };
    }
}
