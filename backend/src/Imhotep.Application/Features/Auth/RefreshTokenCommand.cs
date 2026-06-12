using FluentValidation;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Auth;

public record RefreshTokenCommand(string RefreshToken) : IRequest<TokenPairDto>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RefreshTokenCommandHandler(
    IAppDbContext db,
    IJwtTokenService tokens,
    ICurrentUserService currentUser,
    IClock clock)
    : IRequestHandler<RefreshTokenCommand, TokenPairDto>
{
    public async Task<TokenPairDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var hash = tokens.HashToken(request.RefreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || !stored.User.IsActive)
            throw new AuthenticationFailedException("Invalid refresh token.");

        // Reuse of an already-rotated token means the token was probably stolen:
        // revoke every active token of this user.
        if (stored.IsRevoked)
        {
            var userTokens = await db.RefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAtUtc == null)
                .ToListAsync(ct);
            foreach (var t in userTokens)
                t.RevokedAtUtc = clock.UtcNow;
            await db.SaveChangesAsync(ct);
            throw new AuthenticationFailedException("Invalid refresh token.");
        }

        if (stored.IsExpired(clock.UtcNow))
            throw new AuthenticationFailedException("Invalid refresh token.");

        // Rotation: revoke the presented token, issue a new pair.
        var (rawRefresh, refreshHash) = tokens.CreateRefreshToken();
        stored.RevokedAtUtc = clock.UtcNow;
        stored.ReplacedByTokenHash = refreshHash;
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = stored.UserId,
            TokenHash = refreshHash,
            ExpiresAtUtc = clock.UtcNow.Add(tokens.RefreshTokenLifetime),
            CreatedAt = clock.UtcNow,
            CreatedByIp = currentUser.IpAddress
        });
        await db.SaveChangesAsync(ct);

        return new TokenPairDto(tokens.CreateAccessToken(stored.User), rawRefresh);
    }
}
