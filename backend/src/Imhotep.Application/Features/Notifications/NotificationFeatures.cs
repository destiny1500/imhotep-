using AutoMapper;
using AutoMapper.QueryableExtensions;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Notifications;

public record ListNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>;

public class ListNotificationsQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<ListNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(ListNotificationsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        return await db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ProjectTo<NotificationDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest;

public class MarkNotificationReadCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId, ct)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);
        notification.IsRead = true;
        await db.SaveChangesAsync(ct);
    }
}
