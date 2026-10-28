using FluentValidation;

namespace GiftFinder.Application.Features.ReminderDates.Commands.Create;

public class CreateReminderDateCommandValidator : AbstractValidator<CreateReminderDateCommand>
{
    public CreateReminderDateCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Vui lòng nhập tên người nhận/tiêu đề.")
            .MaximumLength(50).WithMessage("Vui lòng nhập tên người nhận, không quá 50 ký tự.");

        RuleFor(v => v.RecipientRelation)
            .NotEmpty().WithMessage("Vui lòng chọn mối quan hệ với người nhận.");

        RuleFor(v => v.EventDate)
            .NotEmpty().WithMessage("Vui lòng nhập ngày và tháng của sự kiện.");

        RuleFor(v => v.DaysBeforeNotify)
            .InclusiveBetween(1, 30).WithMessage("Số ngày nhắc trước phải từ 1 đến 30 ngày.");
    }
}
