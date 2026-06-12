using AutoMapper;
using AutoMapper.QueryableExtensions;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Messaging;

public record ListConversationsQuery : IRequest<IReadOnlyList<ConversationDto>>;

public class ListConversationsQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IMapper mapper)
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
        return conversations.Select(mapper.Map<ConversationDto>).ToList();
    }
}

public record GetMessagesQuery(Guid ConversationId) : IRequest<IReadOnlyList<MessageDto>>;

public class GetMessagesQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IMapper mapper)
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
            .ProjectTo<MessageDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}
