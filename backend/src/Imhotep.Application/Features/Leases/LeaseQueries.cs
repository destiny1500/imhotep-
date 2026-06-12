using AutoMapper;
using AutoMapper.QueryableExtensions;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Leases;

public record ListLeasesQuery(Guid PropertyId) : IRequest<IReadOnlyList<LeaseDto>>;

public class ListLeasesQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<ListLeasesQuery, IReadOnlyList<LeaseDto>>
{
    public async Task<IReadOnlyList<LeaseDto>> Handle(ListLeasesQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var property = await db.Properties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId, ct)
            ?? throw new NotFoundException(nameof(Property), request.PropertyId);
        if (!property.IsManagedBy(userId))
            throw new NotFoundException(nameof(Property), request.PropertyId);

        return await db.Leases.AsNoTracking()
            .Where(l => l.PropertyId == request.PropertyId)
            .OrderByDescending(l => l.StartDate)
            .ProjectTo<LeaseDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}

/// <summary>Tenant view: their active lease with housing information.</summary>
public record GetMyLeaseQuery : IRequest<MyLeaseDto?>;

public class GetMyLeaseQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<GetMyLeaseQuery, MyLeaseDto?>
{
    public async Task<MyLeaseDto?> Handle(GetMyLeaseQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var lease = await db.Leases.AsNoTracking()
            .Include(l => l.Tenant)
            .Include(l => l.Property).ThenInclude(p => p.Owner)
            .Include(l => l.Property).ThenInclude(p => p.ManagingAgency)
            .Where(l => l.TenantId == userId && l.Status == LeaseStatus.Active)
            .OrderByDescending(l => l.StartDate)
            .FirstOrDefaultAsync(ct);

        if (lease is null)
            return null;

        return new MyLeaseDto(
            mapper.Map<LeaseDto>(lease),
            mapper.Map<PropertyDto>(lease.Property),
            lease.Property.Owner.FullName,
            lease.Property.ManagingAgency?.FullName);
    }
}
