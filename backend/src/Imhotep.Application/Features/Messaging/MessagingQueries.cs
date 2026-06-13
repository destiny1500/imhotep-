using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Mappings;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Messaging;

/// <summary>The people the current user is allowed to start a conversation with:
/// an owner sees their tenants and managing agency, a tenant sees their owner and
/// managing agency, an agency sees the owners and tenants of the properties it manages.
/// Mirrors the relationship rule enforced in <see cref="StartConversationCommandHandler"/>.</summary>
public record ListContactsQuery : IRequest<IReadOnlyList<ParticipantDto>>;

public class ListContactsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ListContactsQuery, IReadOnlyList<ParticipantDto>>
{
    public async Task<IReadOnlyList<ParticipantDto>> Handle(ListContactsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        return await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Id != userId && db.Properties.Any(p =>
                // owner ↔ managing agency
                (p.OwnerId == userId && p.ManagingAgencyId == u.Id) ||
                (p.OwnerId == u.Id && p.ManagingAgencyId == userId) ||
                // owner ↔ tenant
                (p.OwnerId == userId && p.Leases.Any(l => l.TenantId == u.Id && l.Status == LeaseStatus.Active)) ||
                (p.OwnerId == u.Id && p.Leases.Any(l => l.TenantId == userId && l.Status == LeaseStatus.Active)) ||
                // managing agency ↔ tenant
                (p.ManagingAgencyId == userId && p.Leases.Any(l => l.TenantId == u.Id && l.Status == LeaseStatus.Active)) ||
                (p.ManagingAgencyId == u.Id && p.Leases.Any(l => l.TenantId == userId && l.Status == LeaseStatus.Active))))
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Select(u => new ParticipantDto(
                u.Id,
                u.FirstName == "" ? u.Email : u.FirstName + " " + u.LastName,
                u.Role))
            .ToListAsync(ct);
    }
}

public record ListConversationsQuery : IRequest<IReadOnlyList<ConversationDto>>;


public class ListConversationsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ListConversationsQuery, IReadOnlyList<ConversationDto>>
{
    public async Task<IReadOnlyList<ConversationDto>> Handle(ListConversationsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var conversations = await db.Conversations.AsNoTracking()
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.LastMessageAtUtc)
            .ToListAsync(ct);
        return conversations.Select(c => c.ToDto()).ToList();
    }
}

public record GetMessagesQuery(Guid ConversationId) : IRequest<IReadOnlyList<MessageDto>>;

public class GetMessagesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMessagesQuery, IReadOnlyList<MessageDto>>
{
    public async Task<IReadOnlyList<MessageDto>> Handle(GetMessagesQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var isParticipant = await db.Conversations.AsNoTracking()
            .AnyAsync(c => c.Id == request.ConversationId && c.Participants.Any(p => p.UserId == userId), ct);
        if (!isParticipant)
            throw new NotFoundException(nameof(Conversation), request.ConversationId);

        return await db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == request.ConversationId)
            .OrderBy(m => m.SentAtUtc)
            .Select(Projections.ToMessageDto)
            .ToListAsync(ct);
    }
}
