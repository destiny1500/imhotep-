using Imhotep.Application.Common.Behaviors;
using Imhotep.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Auth;

public record LogoutCommand(string RefreshToken) : IRequest, IAuditableCommand
{
    public string AuditAction => "auth.logout";
}

public class LogoutCommandHandler(IAppDbContext db, IJwtTokenService tokens, IClock clock)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var hash = tokens.HashToken(request.RefreshToken);
        var stored = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAtUtc == null, ct);
        if (stored is not null)
        {
            stored.RevokedAtUtc = clock.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        // Unknown token: succeed silently, logout must be idempotent.
    }
}
