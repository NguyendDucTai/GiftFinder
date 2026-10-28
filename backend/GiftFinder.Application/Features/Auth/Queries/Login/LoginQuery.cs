using MediatR;
using GiftFinder.Application.Features.Auth.DTOs;

namespace GiftFinder.Application.Features.Auth.Queries.Login;

public record LoginQuery(
    string Email,
    string Password
) : IRequest<AuthResult>;
