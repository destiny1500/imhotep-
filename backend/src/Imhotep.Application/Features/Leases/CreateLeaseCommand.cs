using FluentValidation;
using Imhotep.Application.Common.Behaviors;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Mappings;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Imhotep.Application.Features.Leases;

public record CreateLeaseCommand(
    Guid PropertyId,
    string TenantEmail,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal RentAmount,
    decimal ChargesAmount,
    decimal DepositAmount) : IRequest<CreateLeaseResultDto>, IAuditableCommand
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

/// <summary>
/// Creates the lease for an existing tenant account, or — when the e-mail is
/// unknown — creates a placeholder (inactive) tenant, a Pending lease and a
/// time-limited invitation the tenant uses to create their account, after
/// which the lease is attached and activated automatically.
/// </summary>
public class CreateLeaseCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IClock clock,
    IJwtTokenService tokens,
    IInvitationLinkBuilder linkBuilder,
    IEmailSender emailSender)
    : IRequestHandler<CreateLeaseCommand, CreateLeaseResultDto>
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(14);

    public async Task<CreateLeaseResultDto> Handle(CreateLeaseCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var now = clock.UtcNow;

        var property = await db.Properties.FirstOrDefaultAsync(p => p.Id == request.PropertyId, ct)
            ?? throw new NotFoundException(nameof(Property), request.PropertyId);
        if (!property.IsManagedBy(userId))
            throw new NotFoundException(nameof(Property), request.PropertyId);

        var hasOngoingLease = await db.Leases.AnyAsync(
            l => l.PropertyId == property.Id
                && (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Pending), ct);
        if (hasOngoingLease)
            throw new ConflictException("This property already has an active or pending lease.");

        var email = request.TenantEmail.Trim().ToLowerInvariant();
        var tenant = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (tenant is not null && tenant.Role != UserRole.Tenant)
            throw new ConflictException("This email belongs to an existing account that is not a tenant account.");

        // Unknown e-mail: placeholder account, activated when the invitation is accepted.
        if (tenant is null)
        {
            tenant = new User
            {
                Email = email,
                PasswordHash = string.Empty, // unusable until the invitation is accepted
                FirstName = string.Empty,
                LastName = string.Empty,
                Role = UserRole.Tenant,
                IsActive = false,
                CreatedAt = now
            };
            db.Users.Add(tenant);
        }

        var needsInvitation = !tenant.IsActive;
        var lease = new Lease
        {
            Property = property,
            PropertyId = property.Id,
            Tenant = tenant,
            TenantId = tenant.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RentAmount = request.RentAmount,
            ChargesAmount = request.ChargesAmount,
            DepositAmount = request.DepositAmount,
            Status = needsInvitation ? LeaseStatus.Pending : LeaseStatus.Active,
            CreatedAt = now
        };
        db.Leases.Add(lease);

        string? invitationUrl = null;
        if (needsInvitation)
        {
            // 256 bits of entropy, hex = URL-safe; only the hash is persisted.
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            db.LeaseInvitations.Add(new LeaseInvitation
            {
                Lease = lease,
                Email = email,
                TokenHash = tokens.HashToken(rawToken),
                ExpiresAtUtc = now.Add(InvitationLifetime),
                InvitedById = userId,
                CreatedAt = now
            });
            invitationUrl = linkBuilder.BuildInvitationUrl(rawToken);
            await emailSender.SendAsync(
                email,
                "Invitation à rejoindre Imhotep",
                $"Un bail vous attend pour le logement « {property.Label} ». " +
                $"Créez votre compte pour y accéder : {invitationUrl}",
                ct);
        }
        else
        {
            property.Status = PropertyStatus.Rented;
            db.Notifications.Add(new Notification
            {
                UserId = tenant.Id,
                Type = NotificationType.LeaseCreated,
                Title = "Nouveau bail",
                Body = $"Un bail a été créé pour le logement « {property.Label} ».",
                CreatedAt = now
            });
        }

        await db.SaveChangesAsync(ct);
        return new CreateLeaseResultDto(lease.ToDto(), invitationUrl);
    }
}
