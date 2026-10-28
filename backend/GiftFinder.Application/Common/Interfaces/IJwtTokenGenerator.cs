namespace GiftFinder.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(string userId, string email, string fullName, string role);
}
