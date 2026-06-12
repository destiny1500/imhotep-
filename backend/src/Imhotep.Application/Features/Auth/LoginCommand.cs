using AutoMapper;
using FluentValidation;
using Imhotep.Application.Common.Behaviors;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>, IAuditableCommand
{
    public string AuditAction => "auth.login";
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler(
    IAppDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService tokens,
    ICurrentUserService currentUser,
    IClock clock,
    IMapper mapper)
    : IRequestHandler<LoginCommand, AuthResultDto>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

        // Same generic error whether the account exists or not (no user enumeration).
        if (user is null)
            throw new AuthenticationFailedException();

        if (user.IsLockedOut(clock.UtcNow))
            throw new AuthenticationFailedException("Account temporarily locked. Try again later.");

        if (!hasher.Verify(request.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin(clock.UtcNow, MaxFailedAttempts, LockoutDuration);
            await db.SaveChangesAsync(ct);
            throw new AuthenticationFailedException();
        }

        user.RegisterSuccessfulLogin();

        var accessToken = tokens.CreateAccessToken(user);
        var (rawRefresh, refreshHash) = tokens.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAtUtc = clock.UtcNow.Add(tokens.RefreshTokenLifetime),
            CreatedAt = clock.UtcNow,
            CreatedByIp = currentUser.IpAddress
        });
        await db.SaveChangesAsync(ct);

        return new AuthResultDto(accessToken, rawRefresh, mapper.Map<UserDto>(user));
    }
}
