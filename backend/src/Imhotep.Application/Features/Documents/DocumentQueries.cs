using AutoMapper;
using AutoMapper.QueryableExtensions;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Documents;

public record ListDocumentsQuery(Guid? PropertyId, Guid? LeaseId) : IRequest<IReadOnlyList<DocumentDto>>;

public class ListDocumentsQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<ListDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    public async Task<IReadOnlyList<DocumentDto>> Handle(ListDocumentsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var query = db.Documents.AsNoTracking();

        if (request.LeaseId.HasValue)
        {
            var lease = await db.Leases.AsNoTracking()
                .Include(l => l.Property)
                .FirstOrDefaultAsync(l => l.Id == request.LeaseId.Value, ct)
                ?? throw new NotFoundException(nameof(Lease), request.LeaseId.Value);
            if (!lease.Property.IsManagedBy(userId) && lease.TenantId != userId)
                throw new NotFoundException(nameof(Lease), request.LeaseId.Value);
            query = query.Where(d => d.LeaseId == request.LeaseId.Value);
        }
        else if (request.PropertyId.HasValue)
        {
            var property = await db.Properties.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.PropertyId.Value, ct)
                ?? throw new NotFoundException(nameof(Property), request.PropertyId.Value);
            var isTenant = await db.Leases.AsNoTracking()
                .AnyAsync(l => l.PropertyId == property.Id && l.TenantId == userId, ct);
            if (!property.IsManagedBy(userId) && !isTenant)
                throw new NotFoundException(nameof(Property), request.PropertyId.Value);
            query = query.Where(d => d.PropertyId == request.PropertyId.Value);
        }
        else
        {
            // No filter: everything the caller may see (uploader, manager, or tenant).
            query = query.Where(d =>
                d.UploadedById == userId ||
                (d.Property != null && (d.Property.OwnerId == userId || d.Property.ManagingAgencyId == userId)) ||
                (d.Lease != null && (d.Lease.TenantId == userId ||
                    d.Lease.Property.OwnerId == userId || d.Lease.Property.ManagingAgencyId == userId)));
        }

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .ProjectTo<DocumentDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}

public record DownloadDocumentQuery(Guid DocumentId) : IRequest<FileContentDto>;

public class DownloadDocumentQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IDocumentStorageService storage)
    : IRequestHandler<DownloadDocumentQuery, FileContentDto>
{
    public async Task<FileContentDto> Handle(DownloadDocumentQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var document = await db.Documents.AsNoTracking()
            .Include(d => d.Property)
            .Include(d => d.Lease).ThenInclude(l => l!.Property)
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, ct)
            ?? throw new NotFoundException(nameof(Document), request.DocumentId);

        var allowed = document.UploadedById == userId
            || (document.Property?.IsManagedBy(userId) ?? false)
            || (document.Lease is not null &&
                (document.Lease.TenantId == userId || document.Lease.Property.IsManagedBy(userId)));
        if (!allowed)
            throw new NotFoundException(nameof(Document), request.DocumentId);

        var content = await storage.ReadAsync(document.StoragePath, ct);
        return new FileContentDto(document.FileName, document.ContentType, content);
    }
}
