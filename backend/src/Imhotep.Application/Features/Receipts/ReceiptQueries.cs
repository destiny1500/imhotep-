using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Mappings;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Imhotep.Application.Features.Receipts;

public record GetMyReceiptsQuery : IRequest<IReadOnlyList<ReceiptDto>>;

public class GetMyReceiptsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyReceiptsQuery, IReadOnlyList<ReceiptDto>>
{
    public async Task<IReadOnlyList<ReceiptDto>> Handle(GetMyReceiptsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        return await db.RentReceipts.AsNoTracking()
            .Where(r => r.Lease.TenantId == userId)
            .OrderByDescending(r => r.IssuedAtUtc)
            .Select(Projections.ToReceiptDto)
            .ToListAsync(ct);
    }
}

public record ListReceiptsQuery(Guid LeaseId) : IRequest<IReadOnlyList<ReceiptDto>>;

public class ListReceiptsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ListReceiptsQuery, IReadOnlyList<ReceiptDto>>
{
    public async Task<IReadOnlyList<ReceiptDto>> Handle(ListReceiptsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var lease = await db.Leases.AsNoTracking()
            .Include(l => l.Property)
            .FirstOrDefaultAsync(l => l.Id == request.LeaseId, ct)
            ?? throw new NotFoundException(nameof(Lease), request.LeaseId);
        if (!lease.Property.IsManagedBy(userId) && lease.TenantId != userId)
            throw new NotFoundException(nameof(Lease), request.LeaseId);

        return await db.RentReceipts.AsNoTracking()
            .Where(r => r.LeaseId == request.LeaseId)
            .OrderByDescending(r => r.IssuedAtUtc)
            .Select(Projections.ToReceiptDto)
            .ToListAsync(ct);
    }
}

/// <summary>Renders the quittance as a downloadable HTML document.</summary>
public record DownloadReceiptQuery(Guid ReceiptId) : IRequest<FileContentDto>;

public class DownloadReceiptQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DownloadReceiptQuery, FileContentDto>
{
    public async Task<FileContentDto> Handle(DownloadReceiptQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var receipt = await db.RentReceipts.AsNoTracking()
            .Include(r => r.Lease).ThenInclude(l => l.Tenant)
            .Include(r => r.Lease).ThenInclude(l => l.Property).ThenInclude(p => p.Owner)
            .FirstOrDefaultAsync(r => r.Id == request.ReceiptId, ct)
            ?? throw new NotFoundException(nameof(RentReceipt), request.ReceiptId);

        // Tenant of the lease, owner, or managing agency only.
        if (receipt.Lease.TenantId != userId && !receipt.Lease.Property.IsManagedBy(userId))
            throw new NotFoundException(nameof(RentReceipt), request.ReceiptId);

        var html = Render(receipt);
        return new FileContentDto(
            $"quittance-{receipt.Number}.html",
            "text/html; charset=utf-8",
            Encoding.UTF8.GetBytes(html));
    }

    private static string Render(RentReceipt r)
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var property = r.Lease.Property;
        var total = r.RentAmount + r.ChargesAmount;
        // All dynamic values are HTML-encoded to keep the rendered document XSS-safe.
        string E(string s) => System.Net.WebUtility.HtmlEncode(s);
        return $$"""
            <!DOCTYPE html>
            <html lang="fr">
            <head><meta charset="utf-8"><title>Quittance {{E(r.Number)}}</title>
            <style>body{font-family:sans-serif;max-width:640px;margin:2rem auto;color:#1a1a1a}
            h1{font-size:1.3rem}table{width:100%;border-collapse:collapse}td{padding:.4rem;border-bottom:1px solid #ddd}</style>
            </head>
            <body>
            <h1>Quittance de loyer n° {{E(r.Number)}}</h1>
            <p>Émise le {{r.IssuedAtUtc.ToString("d MMMM yyyy", fr)}}</p>
            <p><strong>Bailleur :</strong> {{E(property.Owner.FullName)}}<br>
            <strong>Locataire :</strong> {{E(r.Lease.Tenant.FullName)}}<br>
            <strong>Logement :</strong> {{E(property.AddressLine1)}}, {{E(property.PostalCode)}} {{E(property.City)}}</p>
            <p>Période : du {{r.PeriodStart.ToString("d MMMM yyyy", fr)}} au {{r.PeriodEnd.ToString("d MMMM yyyy", fr)}}</p>
            <table>
            <tr><td>Loyer</td><td>{{r.RentAmount.ToString("C", fr)}}</td></tr>
            <tr><td>Charges</td><td>{{r.ChargesAmount.ToString("C", fr)}}</td></tr>
            <tr><td><strong>Total</strong></td><td><strong>{{total.ToString("C", fr)}}</strong></td></tr>
            </table>
            <p>Le bailleur déclare avoir reçu du locataire la somme indiquée ci-dessus
            au titre du loyer et des charges pour la période mentionnée, et lui en donne quittance.</p>
            </body></html>
            """;
    }
}
