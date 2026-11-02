using GiftFinder.Domain.Enums;
using MediatR;

namespace GiftFinder.Application.Features.Catalog.Queries.GetCatalogProducts;

public class GetCatalogProductsQuery : IRequest<CatalogProductsResultDto>
{
    public string? Category { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public long? MinPrice { get; set; }
    public long? MaxPrice { get; set; }
    public string? Sort { get; set; } = "relevance";
    public ProductSource? Source { get; set; }
}

public class CatalogProductsResultDto
{
    public CatalogCategoryInfoDto Category { get; set; } = default!;
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<CatalogProductItemDto> Items { get; set; } = new();
}

public class CatalogCategoryInfoDto
{
    public string Slug { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Group { get; set; } = default!;
}

public class CatalogProductItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public long Price { get; set; }
    public long? OriginalPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string OriginalUrl { get; set; } = default!;
    public string? AffiliateUrl { get; set; }
    public string Source { get; set; } = default!;
    public double Rating { get; set; }
    public int TotalReviews { get; set; }
    public bool IsFeatured { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class CatalogCategoryCountDto
{
    public string Slug { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Group { get; set; } = default!;
    public int Count { get; set; }
}

