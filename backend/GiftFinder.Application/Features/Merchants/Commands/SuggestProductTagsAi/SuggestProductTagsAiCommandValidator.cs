using FluentValidation;

namespace GiftFinder.Application.Features.Merchants.Commands.SuggestProductTagsAi;

public class SuggestProductTagsAiCommandValidator : AbstractValidator<SuggestProductTagsAiCommand>
{
    public SuggestProductTagsAiCommandValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .MaximumLength(255).WithMessage("Tên sản phẩm không được vượt quá 255 ký tự.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá sản phẩm phải lớn hơn 0.");
    }
}
