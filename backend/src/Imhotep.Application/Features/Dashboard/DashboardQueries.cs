using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Dashboard;

public record GetOwnerDashboardQuery : IRequest<OwnerDashboardDto>;

public class GetOwnerDashboardQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock)
    : IRequestHandler<GetOwnerDashboardQuery, OwnerDashboardDto>
{
    public async Task<OwnerDashboardDto> Handle(GetOwnerDashboardQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var now = clock.UtcNow;

        var propertiesCount = await db.Properties.CountAsync(p => p.OwnerId == userId, ct);
        var activeLeases = await db.Leases.CountAsync(
            l => l.Property.OwnerId == userId && l.Status == LeaseStatus.Active, ct);
        // Summed client-side: monthly volume is small and SQLite (tests) cannot SUM decimals.
        var rentCollected = (await db.Payments
            .Where(p => p.Lease.Property.OwnerId == userId
                && p.PeriodYear == now.Year && p.PeriodMonth == now.Month
                && p.Status == PaymentStatus.Completed)
            .Select(p => p.Amount)
            .ToListAsync(ct)).Sum();
        var latePayments = await db.Leases.CountAsync(
            l => l.Property.OwnerId == userId && l.Status == LeaseStatus.Active
                && !l.Payments.Any(p => p.PeriodYear == now.Year && p.PeriodMonth == now.Month
                    && p.Status == PaymentStatus.Completed), ct);

        return new OwnerDashboardDto(propertiesCount, activeLeases, rentCollected, latePayments);
    }
}

public record GetAgencyDashboardQuery : IRequest<AgencyDashboardDto>;

public class GetAgencyDashboardQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock)
    : IRequestHandler<GetAgencyDashboardQuery, AgencyDashboardDto>
{
    public async Task<AgencyDashboardDto> Handle(GetAgencyDashboardQuery request, CancellationToken ct)
    {
        var agencyId = currentUser.UserId!.Value;
        var now = clock.UtcNow;

        var managed = db.Properties.Where(p => p.ManagingAgencyId == agencyId);
        var managedCount = await managed.CountAsync(ct);
        var ownersCount = await managed.Select(p => p.OwnerId).Distinct().CountAsync(ct);
        var tenantsCount = await db.Leases
            .Where(l => l.Property.ManagingAgencyId == agencyId && l.Status == LeaseStatus.Active)
            .Select(l => l.TenantId).Distinct().CountAsync(ct);
        var rentedCount = await managed.CountAsync(p => p.Status == PropertyStatus.Rented, ct);
        var occupancyRate = managedCount == 0 ? 0m : Math.Round((decimal)rentedCount / managedCount * 100, 1);
        var latePayments = await db.Leases.CountAsync(
            l => l.Property.ManagingAgencyId == agencyId && l.Status == LeaseStatus.Active
                && !l.Payments.Any(p => p.PeriodYear == now.Year && p.PeriodMonth == now.Month
                    && p.Status == PaymentStatus.Completed), ct);
        // A managed property without a lease contract document is flagged as incomplete.
        var missingDocuments = await managed.CountAsync(
            p => !db.Documents.Any(d => d.PropertyId == p.Id
                || (d.Lease != null && d.Lease.PropertyId == p.Id)), ct);
        var rentCollected = (await db.Payments
            .Where(p => p.Lease.Property.ManagingAgencyId == agencyId
                && p.PeriodYear == now.Year && p.PeriodMonth == now.Month
                && p.Status == PaymentStatus.Completed)
            .Select(p => p.Amount)
            .ToListAsync(ct)).Sum();

        return new AgencyDashboardDto(
            managedCount, ownersCount, tenantsCount, occupancyRate,
            latePayments, missingDocuments, rentCollected);
    }
}
