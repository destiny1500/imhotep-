using Imhotep.Application.Common.Interfaces;
using Imhotep.Domain.Entities;
using MediatR;

namespace Imhotep.Application.Common.Behaviors;

/// <summary>A command that must leave a trace in the audit log.</summary>
public interface IAuditableCommand
{
    string AuditAction { get; }
    string? AuditEntityType => null;
    string? AuditEntityId => null;
}

/// <summary>
/// Writes an audit entry after each successful auditable command. Request payloads
/// are deliberately not serialized so credentials and personal data never reach the log.
/// </summary>
public class AuditBehavior<TRequest, TResponse>(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IClock clock)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IAuditableCommand auditable)
        {
            db.AuditLogs.Add(new AuditLog
            {
                UserId = currentUser.UserId,
                Action = auditable.AuditAction,
                EntityType = auditable.AuditEntityType,
                EntityId = auditable.AuditEntityId,
                IpAddress = currentUser.IpAddress,
                TimestampUtc = clock.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
