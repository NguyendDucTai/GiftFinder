using MediatR;
using GiftFinder.Application.Features.Auth.DTOs;

namespace GiftFinder.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName
) : IRequest<AuthResult>;
