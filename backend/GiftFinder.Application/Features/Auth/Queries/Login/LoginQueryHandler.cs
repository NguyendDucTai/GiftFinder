using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Auth.Queries.Login;

public class LoginQueryHandler : IRequestHandler<LoginQuery, AuthResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginQueryHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResult> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        // 1. TĂ¬m User theo email
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        if (user == null)
            throw new Exception("Email hoáº·c máº­t kháº©u khĂ´ng chĂ­nh xĂ¡c.");

        // 2. XĂ¡c thá»±c máº­t kháº©u
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
            throw new Exception("Email hoáº·c máº­t kháº©u khĂ´ng chĂ­nh xĂ¡c.");

        if (!user.IsActive)
            throw new Exception("TĂ i khoáº£n cá»§a báº¡n Ä‘Ă£ bá»‹ khĂ³a.");

        // 3. Táº¡o JWT Token
        var token = _jwtTokenGenerator.GenerateToken(user.Id.ToString(), user.Email, user.FullName, user.Role.ToString());

        return new AuthResult(user.Id.ToString(), user.Email, user.FullName, user.Role.ToString(), token);
    }
}
