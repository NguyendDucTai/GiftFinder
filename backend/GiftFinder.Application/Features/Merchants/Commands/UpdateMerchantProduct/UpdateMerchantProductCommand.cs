using FluentValidation;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Commands.UpdateMerchantProduct;

public class UpdateMerchantProductCommand : IRequest<MerchantProductDto>
{
    public Guid Id { get; set; }
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

public class UpdateMerchantProductCommandValidator : AbstractValidator<UpdateMerchantProductCommand>
{
    public UpdateMerchantProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã sản phẩm không được để trống.");

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
            .NotEmpty().WithMessage("Đường dẫn đặt mua không được để trống.")
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

public class UpdateMerchantProductCommandHandler : IRequestHandler<UpdateMerchantProductCommand, MerchantProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMerchantProductCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MerchantProductDto> Handle(UpdateMerchantProductCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để cập nhật sản phẩm.");
        }

        var merchant = await _context.Merchants
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new InvalidOperationException("Tài khoản của bạn chưa đăng ký gian hàng.");
        }

        var product = await _context.Products
            .Include(p => p.ProductTags)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.MerchantId == merchant.Id, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm hoặc sản phẩm không thuộc quyền quản lý của gian hàng bạn.");
        }

        // BR-03: Cập nhật thông tin và tự động chuyển về Pending nếu trước đó đã Approved
        product.UpdateMerchantDetails(
            request.Name,
            request.Price,
            request.OriginalUrl,
            request.Description,
            request.ImageUrl,
            request.OriginalPrice
        );

        if (request.SuitableZodiacs != null)
        {
            product.SetSuitableZodiacs(request.SuitableZodiacs);
        }

        // Cập nhật Tags
        if (request.TagIds != null)
        {
            var requestedTagIds = request.TagIds.Distinct().ToList();
            var existingProductTags = product.ProductTags.ToList();

            // Xóa tag không còn chọn
            foreach (var pt in existingProductTags)
            {
                if (!requestedTagIds.Contains(pt.TagId))
                {
                    product.RemoveTag(pt.TagId);
                }
            }

            // Thêm tag mới
            var currentTagIds = product.ProductTags.Select(pt => pt.TagId).ToList();
            var tagsToAdd = requestedTagIds.Except(currentTagIds).ToList();

            var validNewTags = await _context.Tags
                .Where(t => tagsToAdd.Contains(t.Id))
                .ToListAsync(cancellationToken);

            foreach (var tag in validNewTags)
            {
                product.AddTag(tag.Id);
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
                    tag = new GiftFinder.Domain.Entities.Tag(cleanName, slug, TagType.Hobby);
                    _context.Tags.Add(tag);
                    allTags.Add(tag);
                }

                if (!product.ProductTags.Any(pt => pt.TagId == tag.Id))
                {
                    product.AddTag(tag.Id);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var finalTags = await _context.ProductTags
            .Where(pt => pt.ProductId == product.Id)
            .Include(pt => pt.Tag)
            .ToListAsync(cancellationToken);

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
            TagIds = finalTags.Select(pt => pt.TagId).ToList(),
            TagNames = finalTags.Select(pt => pt.Tag.Name).ToList(),
            CreatedAt = product.CreatedAt
        };
    }
}
