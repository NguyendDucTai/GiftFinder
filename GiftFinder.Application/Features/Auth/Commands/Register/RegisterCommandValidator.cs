using FluentValidation;

namespace GiftFinder.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Địa chỉ email không hợp lệ, vui lòng kiểm tra lại.")
            .EmailAddress().WithMessage("Địa chỉ email không hợp lệ, vui lòng kiểm tra lại.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải từ 6 ký tự trở lên.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.");
    }
}
