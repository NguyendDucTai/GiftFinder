namespace GiftFinder.Application.Features.Auth.DTOs;

public record AuthResult(
    string Id,
    string Email,
    string FullName,
    string Role,
    string Token
);
