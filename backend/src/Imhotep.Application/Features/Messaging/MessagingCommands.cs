using FluentValidation;
using Imhotep.Application.Common.Behaviors;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Messaging;

public record StartConversationCommand(
    Guid ParticipantUserId,
    Guid? PropertyId,
    string Subject,
    string Body) : IRequest<Guid>, IAuditableCommand
{
    public string AuditAction => "conversation.start";
    public string? AuditEntityType => nameof(Conversation);
}

public class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationCommandValidator()
    {
        RuleFor(x => x.ParticipantUserId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10_000);
    }
}

public class StartConversationCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock)
    : IRequestHandler<StartConversationCommand, Guid>
{
    public async Task<Guid> Handle(StartConversationCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        if (request.ParticipantUserId == userId)
            throw new ConflictException("Cannot start a conversation with yourself.");

        var other = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.ParticipantUserId && u.IsActive, ct)
            ?? throw new NotFoundException(nameof(User), request.ParticipantUserId);

        // A conversation may only be opened between people a property links together:
        // an owner writes to their tenants or their agency, a tenant writes to their
        // owner or the managing agency. Admins are exempt (platform oversight).
        if (currentUser.Role != UserRole.Admin && !await AreConnectedAsync(userId, other.Id, ct))
            throw new ForbiddenAccessException(
                "Vous ne pouvez écrire qu'aux personnes liées à vos biens : un propriétaire à ses locataires ou à son agence, un locataire à son propriétaire ou à l'agence gestionnaire.");

        var now = clock.UtcNow;
        var conversation = new Conversation
        {
            Subject = request.Subject.Trim(),
            PropertyId = request.PropertyId,
            LastMessageAtUtc = now,
            CreatedAt = now
        };
        conversation.Participants.Add(new ConversationParticipant { Conversation = conversation, UserId = userId });
        conversation.Participants.Add(new ConversationParticipant { Conversation = conversation, UserId = other.Id });
        conversation.Messages.Add(new Message
        {
            Conversation = conversation,
            SenderId = userId,
            Body = request.Body.Trim(),
            SentAtUtc = now,
            CreatedAt = now
        });
        db.Conversations.Add(conversation);
        db.Notifications.Add(new Notification
        {
            UserId = other.Id,
            Type = NotificationType.MessageReceived,
            Title = "Nouveau message",
            Body = $"Nouvelle conversation : « {conversation.Subject} »",
            CreatedAt = now
        });
        await db.SaveChangesAsync(ct);
        return conversation.Id;
    }

    /// <summary>
    /// True when a property links the two users in compatible roles, which is the only
    /// case in which they are allowed to message each other:
    /// owner ↔ tenant (active lease), owner ↔ managing agency, or managing agency ↔ tenant.
    /// The relation is symmetric, so it covers both directions of the rule.
    /// </summary>
    private Task<bool> AreConnectedAsync(Guid a, Guid b, CancellationToken ct) =>
        db.Properties.AsNoTracking().AnyAsync(p =>
            // owner ↔ managing agency
            (p.OwnerId == a && p.ManagingAgencyId == b) ||
            (p.OwnerId == b && p.ManagingAgencyId == a) ||
            // owner ↔ tenant
            (p.OwnerId == a && p.Leases.Any(l => l.TenantId == b && l.Status == LeaseStatus.Active)) ||
            (p.OwnerId == b && p.Leases.Any(l => l.TenantId == a && l.Status == LeaseStatus.Active)) ||
            // managing agency ↔ tenant
            (p.ManagingAgencyId == a && p.Leases.Any(l => l.TenantId == b && l.Status == LeaseStatus.Active)) ||
            (p.ManagingAgencyId == b && p.Leases.Any(l => l.TenantId == a && l.Status == LeaseStatus.Active)),
            ct);
}

public record SendMessageCommand(Guid ConversationId, string Body) : IRequest<Guid>, IAuditableCommand
{
    public string AuditAction => "conversation.message";
    public string? AuditEntityType => nameof(Message);
    public string? AuditEntityId => ConversationId.ToString();
}

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10_000);
    }
}

public class SendMessageCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock)
    : IRequestHandler<SendMessageCommand, Guid>
{
    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var conversation = await db.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct)
            ?? throw new NotFoundException(nameof(Conversation), request.ConversationId);

        if (!conversation.HasParticipant(userId))
            throw new NotFoundException(nameof(Conversation), request.ConversationId);

        var now = clock.UtcNow;
        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = userId,
            Body = request.Body.Trim(),
            SentAtUtc = now,
            CreatedAt = now
        };
        conversation.LastMessageAtUtc = now;
        db.Messages.Add(message);

        foreach (var participant in conversation.Participants.Where(p => p.UserId != userId))
        {
            db.Notifications.Add(new Notification
            {
                UserId = participant.UserId,
                Type = NotificationType.MessageReceived,
                Title = "Nouveau message",
                Body = $"Nouveau message dans « {conversation.Subject} »",
                CreatedAt = now
            });
        }
        await db.SaveChangesAsync(ct);
        return message.Id;
    }
}
