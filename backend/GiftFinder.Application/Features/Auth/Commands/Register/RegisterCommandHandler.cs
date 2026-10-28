using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Auth.DTOs;
using GiftFinder.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiá»ƒm tra email tá»“n táº¡i chÆ°a
        var emailExists = await _context.Users.AnyAsync(x => x.Email == request.Email, cancellationToken);
        if (emailExists)
            throw new Exception("Email nĂ y Ä‘Ă£ Ä‘Æ°á»£c sá»­ dá»¥ng.");

        // 2. Hash máº­t kháº©u
        var hash = _passwordHasher.HashPassword(request.Password);

        // 3. Táº¡o User
        var user = new User(request.Email, hash, request.FullName);
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Táº¡o JWT Token
        var token = _jwtTokenGenerator.GenerateToken(user.Id.ToString(), user.Email, user.FullName, user.Role.ToString());

        return new AuthResult(user.Id.ToString(), user.Email, user.FullName, user.Role.ToString(), token);
    }
}
