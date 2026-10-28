using FluentValidation;

namespace GiftFinder.Application.Features.Wishlists.Commands.Add;

public class AddToWishlistCommandValidator : AbstractValidator<AddToWishlistCommand>
{
    public AddToWishlistCommandValidator()
    {
        RuleFor(v => v.ProductId)
            .NotEmpty().WithMessage("ProductId là bắt buộc.");

        RuleFor(v => v.TargetPrice)
            .GreaterThan(0).When(v => v.TargetPrice.HasValue).WithMessage("Giá mục tiêu phải lớn hơn 0.");

        RuleFor(v => v.Note)
            .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");
    }
}
