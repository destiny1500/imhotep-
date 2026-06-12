using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Mappings;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Payments;

public record ListPaymentsQuery(Guid LeaseId) : IRequest<IReadOnlyList<PaymentDto>>;

public class ListPaymentsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ListPaymentsQuery, IReadOnlyList<PaymentDto>>
{
    public async Task<IReadOnlyList<PaymentDto>> Handle(ListPaymentsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var lease = await db.Leases.AsNoTracking()
            .Include(l => l.Property)
            .FirstOrDefaultAsync(l => l.Id == request.LeaseId, ct)
            ?? throw new NotFoundException(nameof(Lease), request.LeaseId);

        // Manager (owner/agency) or the tenant of the lease itself.
        if (!lease.Property.IsManagedBy(userId) && lease.TenantId != userId)
            throw new NotFoundException(nameof(Lease), request.LeaseId);

        return await db.Payments.AsNoTracking()
            .Where(p => p.LeaseId == request.LeaseId)
            .OrderByDescending(p => p.PeriodYear).ThenByDescending(p => p.PeriodMonth)
            .Select(Projections.ToPaymentDto)
            .ToListAsync(ct);
    }
}

/// <summary>Tenant payment history across their leases.</summary>
public record GetMyPaymentsQuery : IRequest<IReadOnlyList<PaymentDto>>;

public class GetMyPaymentsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyPaymentsQuery, IReadOnlyList<PaymentDto>>
{
    public async Task<IReadOnlyList<PaymentDto>> Handle(GetMyPaymentsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        return await db.Payments.AsNoTracking()
            .Where(p => p.Lease.TenantId == userId)
            .OrderByDescending(p => p.PeriodYear).ThenByDescending(p => p.PeriodMonth)
            .Select(Projections.ToPaymentDto)
            .ToListAsync(ct);
    }
}
