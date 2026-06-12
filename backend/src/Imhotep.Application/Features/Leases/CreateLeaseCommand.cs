using AutoMapper;
using FluentValidation;
using Imhotep.Application.Common.Behaviors;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Leases;

public record CreateLeaseCommand(
    Guid PropertyId,
    string TenantEmail,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal RentAmount,
    decimal ChargesAmount,
    decimal DepositAmount) : IRequest<LeaseDto>, IAuditableCommand
{
    public string AuditAction => "lease.create";
    public string? AuditEntityType => nameof(Lease);
}

public class CreateLeaseCommandValidator : AbstractValidator<CreateLeaseCommand>
{
    public CreateLeaseCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.TenantEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.RentAmount).GreaterThan(0).LessThan(1_000_000);
        RuleFor(x => x.ChargesAmount).GreaterThanOrEqualTo(0).LessThan(1_000_000);
        RuleFor(x => x.DepositAmount).GreaterThanOrEqualTo(0).LessThan(1_000_000);
    }
}

public class CreateLeaseCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock, IMapper mapper)
    : IRequestHandler<CreateLeaseCommand, LeaseDto>
{
    public async Task<LeaseDto> Handle(CreateLeaseCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var property = await db.Properties.FirstOrDefaultAsync(p => p.Id == request.PropertyId, ct)
            ?? throw new NotFoundException(nameof(Property), request.PropertyId);

        if (!property.IsManagedBy(userId))
            throw new NotFoundException(nameof(Property), request.PropertyId);

        var email = request.TenantEmail.Trim().ToLowerInvariant();
        var tenant = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == UserRole.Tenant && u.IsActive, ct)
            ?? throw new NotFoundException("Tenant", request.TenantEmail);

        var hasActiveLease = await db.Leases
            .AnyAsync(l => l.PropertyId == property.Id && l.Status == LeaseStatus.Active, ct);
        if (hasActiveLease)
            throw new ConflictException("This property already has an active lease.");

        var lease = new Lease
        {
            PropertyId = property.Id,
            TenantId = tenant.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RentAmount = request.RentAmount,
            ChargesAmount = request.ChargesAmount,
            DepositAmount = request.DepositAmount,
            Status = LeaseStatus.Active,
            CreatedAt = clock.UtcNow,
            Tenant = tenant
        };
        property.Status = PropertyStatus.Rented;
        db.Leases.Add(lease);
        db.Notifications.Add(new Notification
        {
            UserId = tenant.Id,
            Type = NotificationType.LeaseCreated,
            Title = "Nouveau bail",
            Body = $"Un bail a été créé pour le logement « {property.Label} ».",
            CreatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return mapper.Map<LeaseDto>(lease);
    }
}
