using FluentValidation;
using Imhotep.Application.Common.Behaviors;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Mappings;
using Imhotep.Application.Common.Models;
using Imhotep.Application.Common.Validation;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Invitations;

/// <summary>Anonymous, token-gated: what the invited tenant sees before accepting.
/// Unknown, expired or already-accepted tokens all answer 404 (no information leak).</summary>
public record GetInvitationQuery(string Token) : IRequest<InvitationInfoDto>;

public class GetInvitationQueryHandler(IAppDbContext db, IJwtTokenService tokens, IClock clock)
    : IRequestHandler<GetInvitationQuery, InvitationInfoDto>
{
    public async Task<InvitationInfoDto> Handle(GetInvitationQuery request, CancellationToken ct)
    {
        var hash = tokens.HashToken(request.Token);
        var invitation = await db.LeaseInvitations.AsNoTracking()
            .Include(i => i.Lease).ThenInclude(l => l.Property).ThenInclude(p => p.Owner)
            .FirstOrDefaultAsync(i => i.TokenHash == hash, ct);

        if (invitation is null || !invitation.IsUsable(clock.UtcNow))
            throw new NotFoundException(nameof(LeaseInvitation), "invalid");

        var property = invitation.Lease.Property;
        return new InvitationInfoDto(
            invitation.Email,
            property.Label,
            property.City,
            invitation.Lease.RentAmount,
            invitation.Lease.ChargesAmount,
            invitation.Lease.StartDate,
            property.Owner.FullName);
    }
}

/// <summary>The invited tenant creates their account: activates the placeholder
/// user, activates the pending lease, and signs them in.</summary>
public record AcceptInvitationCommand(
    string Token,
    string FirstName,
    string LastName,
    string Password) : IRequest<AuthResultDto>, IAuditableCommand
{
    public string AuditAction => "invitation.accept";
    public string? AuditEntityType => nameof(LeaseInvitation);
}

public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(128);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).StrongPassword();
    }
}

public class AcceptInvitationCommandHandler(
    IAppDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService tokens,
    ICurrentUserService currentUser,
    IClock clock)
    : IRequestHandler<AcceptInvitationCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(AcceptInvitationCommand request, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var hash = tokens.HashToken(request.Token);
        var invitation = await db.LeaseInvitations
            .Include(i => i.Lease).ThenInclude(l => l.Tenant)
            .Include(i => i.Lease).ThenInclude(l => l.Property)
            .FirstOrDefaultAsync(i => i.TokenHash == hash, ct);

        if (invitation is null || !invitation.IsUsable(now))
            throw new NotFoundException(nameof(LeaseInvitation), "invalid");

        var lease = invitation.Lease;
        var tenant = lease.Tenant;

        if (!tenant.IsActive)
        {
            tenant.FirstName = request.FirstName.Trim();
            tenant.LastName = request.LastName.Trim();
            tenant.PasswordHash = hasher.Hash(request.Password);
            tenant.IsActive = true;
            tenant.UpdatedAt = now;
        }

        invitation.AcceptedAtUtc = now;
        if (lease.Status == LeaseStatus.Pending)
        {
            lease.Status = LeaseStatus.Active;
            lease.Property.Status = PropertyStatus.Rented;
        }

        // The inviter is told the lease is now effective.
        db.Notifications.Add(new Notification
        {
            UserId = invitation.InvitedById,
            Type = NotificationType.LeaseCreated,
            Title = "Invitation acceptée",
            Body = $"{tenant.FullName} a accepté l'invitation pour « {lease.Property.Label} ». Le bail est actif.",
            CreatedAt = now
        });

        // Sign the tenant in right away.
        var accessToken = tokens.CreateAccessToken(tenant);
        var (rawRefresh, refreshHash) = tokens.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = tenant.Id,
            TokenHash = refreshHash,
            ExpiresAtUtc = now.Add(tokens.RefreshTokenLifetime),
            CreatedAt = now,
            CreatedByIp = currentUser.IpAddress
        });

        await db.SaveChangesAsync(ct);
        return new AuthResultDto(accessToken, rawRefresh, tenant.ToDto());
    }
}
