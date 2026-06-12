using AutoMapper;
using AutoMapper.QueryableExtensions;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Properties;

public record ListPropertiesQuery : IRequest<IReadOnlyList<PropertyDto>>;

public class ListPropertiesQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<ListPropertiesQuery, IReadOnlyList<PropertyDto>>
{
    public async Task<IReadOnlyList<PropertyDto>> Handle(ListPropertiesQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        // Owners see their own properties, agencies the ones delegated to them.
        return await db.Properties.AsNoTracking()
            .Where(p => p.OwnerId == userId || p.ManagingAgencyId == userId)
            .OrderBy(p => p.Label)
            .ProjectTo<PropertyDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}

public record GetPropertyQuery(Guid Id) : IRequest<PropertyDto>;

public class GetPropertyQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<GetPropertyQuery, PropertyDto>
{
    public async Task<PropertyDto> Handle(GetPropertyQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var property = await db.Properties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Property), request.Id);

        // Owner, managing agency, or a tenant with a lease on this property.
        var isTenantOfProperty = await db.Leases.AsNoTracking()
            .AnyAsync(l => l.PropertyId == property.Id && l.TenantId == userId, ct);
        if (!property.IsManagedBy(userId) && !isTenantOfProperty)
            throw new NotFoundException(nameof(Property), request.Id);

        return mapper.Map<PropertyDto>(property);
    }
}
