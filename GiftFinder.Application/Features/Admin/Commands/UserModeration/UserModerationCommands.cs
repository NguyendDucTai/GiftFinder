using FluentValidation;
using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Admin.Commands.UserModeration;

public class LockUserCommand : IRequest<UserModerationResultDto>
{
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class LockUserCommandValidator : AbstractValidator<LockUserCommand>
{
    public LockUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Mã người dùng không được để trống.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Lý do khóa tài khoản không được để trống.")
            .MaximumLength(500).WithMessage("Lý do khóa không được vượt quá 500 ký tự.");
    }
}

public class UnlockUserCommand : IRequest<UserModerationResultDto>
{
    public Guid UserId { get; set; }

    public UnlockUserCommand(Guid userId)
    {
        UserId = userId;
    }
}

public class UserModerationResultDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class LockUserCommandHandler : IRequestHandler<LockUserCommand, UserModerationResultDto>
{
    private readonly IApplicationDbContext _context;

    public LockUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserModerationResultDto> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy người dùng với mã '{request.UserId}'.");
        }

        user.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return new UserModerationResultDto
        {
            UserId = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
            Message = $"Đã khóa tài khoản người dùng '{user.Email}' thành công. Lý do: {request.Reason}"
        };
    }
}

public class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand, UserModerationResultDto>
{
    private readonly IApplicationDbContext _context;

    public UnlockUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserModerationResultDto> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy người dùng với mã '{request.UserId}'.");
        }

        user.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        return new UserModerationResultDto
        {
            UserId = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
            Message = $"Đã mở khóa tài khoản người dùng '{user.Email}' thành công."
        };
    }
}
