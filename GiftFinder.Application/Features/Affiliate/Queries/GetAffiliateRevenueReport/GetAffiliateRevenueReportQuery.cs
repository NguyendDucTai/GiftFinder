using GiftFinder.Application.Features.Affiliate.DTOs;
using MediatR;

namespace GiftFinder.Application.Features.Affiliate.Queries.GetAffiliateRevenueReport;

public class GetAffiliateRevenueReportQuery : IRequest<AffiliateRevenueReportDto>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
