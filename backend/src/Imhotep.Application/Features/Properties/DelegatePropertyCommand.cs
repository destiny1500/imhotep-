using FluentValidation;
using Imhotep.Application.Common.Behaviors;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Properties;

/// <summary>Owner entrusts the management of a property to an agency (mandat de gestion).</summary>
public record DelegatePropertyCommand(Guid PropertyId, Guid AgencyId, decimal FeePercent)
    : IRequest, IAuditableCommand
{
    public string AuditAction => "property.delegate";
    public string? AuditEntityType => nameof(Property);
    public string? AuditEntityId => PropertyId.ToString();
}

public class DelegatePropertyCommandValidator : AbstractValidator<DelegatePropertyCommand>
{
    public DelegatePropertyCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.AgencyId).NotEmpty();
        RuleFor(x => x.FeePercent).InclusiveBetween(0, 50);
    }
}

public class DelegatePropertyCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock)
    : IRequestHandler<DelegatePropertyCommand>
{
    public async Task Handle(DelegatePropertyCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var property = await db.Properties.FirstOrDefaultAsync(p => p.Id == request.PropertyId, ct)
            ?? throw new NotFoundException(nameof(Property), request.PropertyId);

        // Only the owner can delegate their own property.
        if (property.OwnerId != userId)
            throw new NotFoundException(nameof(Property), request.PropertyId);

        var agency = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.AgencyId && u.Role == UserRole.Agency && u.IsActive, ct)
            ?? throw new NotFoundException("Agency", request.AgencyId);

        if (property.ManagingAgencyId is not null)
            throw new ConflictException("This property is already managed by an agency.");

        property.ManagingAgencyId = agency.Id;
        property.UpdatedAt = clock.UtcNow;
        db.ManagementContracts.Add(new ManagementContract
        {
            PropertyId = property.Id,
            OwnerId = userId,
            AgencyId = agency.Id,
            StartDate = DateOnly.FromDateTime(clock.UtcNow),
            FeePercent = request.FeePercent,
            CreatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
