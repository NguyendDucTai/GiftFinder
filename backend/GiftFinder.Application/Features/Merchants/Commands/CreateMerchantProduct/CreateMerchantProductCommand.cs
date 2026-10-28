using FluentValidation;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Commands.CreateMerchantProduct;

public class CreateMerchantProductCommand : IRequest<MerchantProductDto>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long Price { get; set; }
    public long? OriginalPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public List<Guid>? TagIds { get; set; }
    public List<string>? CustomTagNames { get; set; }
    public List<ZodiacSign>? SuitableZodiacs { get; set; }
}

public class CreateMerchantProductCommandValidator : AbstractValidator<CreateMerchantProductCommand>
{
    public CreateMerchantProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .MaximumLength(200).WithMessage("Tên sản phẩm không được vượt quá 200 ký tự.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá sản phẩm phải lớn hơn 0.");

        When(x => x.OriginalPrice.HasValue, () =>
        {
            RuleFor(x => x.OriginalPrice!.Value)
                .GreaterThanOrEqualTo(x => x.Price)
                .WithMessage("Giá gốc trước giảm phải lớn hơn hoặc bằng giá bán.");
        });

        RuleFor(x => x.OriginalUrl)
            .NotEmpty().WithMessage("Đường dẫn đặt mua của shop không được để trống.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Đường dẫn đặt mua phải là một URL hợp lệ.");

        When(x => !string.IsNullOrWhiteSpace(x.ImageUrl), () =>
        {
            RuleFor(x => x.ImageUrl!)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .WithMessage("Đường dẫn hình ảnh phải là một URL hợp lệ.");
        });
    }
}

public class CreateMerchantProductCommandHandler : IRequestHandler<CreateMerchantProductCommand, MerchantProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateMerchantProductCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MerchantProductDto> Handle(CreateMerchantProductCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập với tài khoản đối tác để đăng sản phẩm.");
        }

        var merchant = await _context.Merchants
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new InvalidOperationException("Tài khoản của bạn chưa đăng ký hồ sơ gian hàng Merchant.");
        }

        // Tạo Product với trạng thái mặc định là Pending (Chờ Admin duyệt)
        var product = new Product(
            request.Name.Trim(),
            request.Price,
            request.OriginalUrl.Trim(),
            ProductSource.DirectMerchant,
            externalProductId: null,
            imageUrl: request.ImageUrl?.Trim(),
            originalPrice: request.OriginalPrice,
            merchantId: merchant.Id
        );

        product.SetDescription(request.Description);

        if (request.SuitableZodiacs != null && request.SuitableZodiacs.Count > 0)
        {
            product.SetSuitableZodiacs(request.SuitableZodiacs);
        }

        // Gắn các thẻ phân loại Tags (Dịp tặng, Đối tượng, Sở thích)
        var validTagIds = new List<Guid>();
        var tagNames = new List<string>();

        if (request.TagIds != null && request.TagIds.Count > 0)
        {
            var requestedTagIds = request.TagIds.Distinct().ToList();
            var existingTags = await _context.Tags
                .Where(t => requestedTagIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            foreach (var tag in existingTags)
            {
                product.AddTag(tag.Id);
                validTagIds.Add(tag.Id);
                tagNames.Add(tag.Name);
            }
        }

        // Hỗ trợ Merchant tự gõ thêm Tag mới bất kỳ bằng tay
        if (request.CustomTagNames != null && request.CustomTagNames.Count > 0)
        {
            var allTags = await _context.Tags.ToListAsync(cancellationToken);
            foreach (var customName in request.CustomTagNames)
            {
                if (string.IsNullOrWhiteSpace(customName)) continue;
                var cleanName = customName.Trim();
                var slug = GiftFinder.Application.Common.Helpers.SlugHelper.ToUrlFriendlySlug(cleanName);

                var tag = allTags.FirstOrDefault(t =>
                    t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase));

                if (tag == null)
                {
                    tag = new Tag(cleanName, slug, TagType.Hobby);
                    _context.Tags.Add(tag);
                    allTags.Add(tag);
                }

                if (!validTagIds.Contains(tag.Id))
                {
                    product.AddTag(tag.Id);
                    validTagIds.Add(tag.Id);
                    tagNames.Add(tag.Name);
                }
            }
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return new MerchantProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            OriginalPrice = product.OriginalPrice,
            ImageUrl = product.ImageUrl,
            OriginalUrl = product.OriginalUrl,
            Source = product.Source.ToString(),
            Status = product.Status.ToString(),
            RejectReason = product.RejectReason,
            IsFeatured = product.IsFeatured,
            FeaturedUntil = product.FeaturedUntil,
            SuitableZodiacs = product.SuitableZodiacs.Select(z => z.ToString()).ToList(),
            TagIds = validTagIds,
            TagNames = tagNames,
            CreatedAt = product.CreatedAt
        };
    }
}
