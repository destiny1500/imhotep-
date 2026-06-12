using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using System.Linq.Expressions;

namespace Imhotep.Application.Common.Mappings;

/// <summary>
/// Explicit entity → DTO mapping (no AutoMapper: trivial mappings don't justify the
/// dependency, see GHSA-rvv3-g6hj-g44x). Extension methods for in-memory mapping;
/// navigations they touch must be loaded by the caller.
/// </summary>
public static class EntityMapping
{
    public static UserDto ToDto(this User u) =>
        new(u.Id, u.Email, u.FirstName, u.LastName, u.Role);

    public static PropertyDto ToDto(this Property p) =>
        new(p.Id, p.OwnerId, p.ManagingAgencyId, p.Label, p.AddressLine1, p.AddressLine2,
            p.City, p.PostalCode, p.Country, p.Type, p.Status, p.SurfaceM2, p.Rooms,
            p.RentAmount, p.ChargesAmount);

    /// <summary>Requires <see cref="Lease.Tenant"/> to be loaded. Falls back to the
    /// e-mail while an invited tenant hasn't filled in their name yet.</summary>
    public static LeaseDto ToDto(this Lease l) =>
        new(l.Id, l.PropertyId, l.TenantId,
            l.Tenant.FirstName.Length == 0 ? l.Tenant.Email : l.Tenant.FullName,
            l.StartDate, l.EndDate,
            l.RentAmount, l.ChargesAmount, l.DepositAmount, l.Status);

    public static PaymentDto ToDto(this Payment p) =>
        new(p.Id, p.LeaseId, p.Amount, p.PeriodYear, p.PeriodMonth, p.PaidAtUtc,
            p.Method, p.Status, p.Receipt != null);

    public static ReceiptDto ToDto(this RentReceipt r) =>
        new(r.Id, r.PaymentId, r.LeaseId, r.Number, r.IssuedAtUtc, r.PeriodStart,
            r.PeriodEnd, r.RentAmount, r.ChargesAmount);

    public static DocumentDto ToDto(this Document d) =>
        new(d.Id, d.PropertyId, d.LeaseId, d.Type, d.FileName, d.ContentType,
            d.SizeBytes, d.CreatedAt);

    public static NotificationDto ToDto(this Notification n) =>
        new(n.Id, n.Type, n.Title, n.Body, n.IsRead, n.CreatedAt);

    /// <summary>Requires <see cref="Conversation.Participants"/> (with users) loaded.</summary>
    public static ConversationDto ToDto(this Conversation c) =>
        new(c.Id, c.Subject, c.PropertyId, c.LastMessageAtUtc,
            c.Participants
                .Select(p => new ParticipantDto(p.UserId, p.User.FullName, p.User.Role))
                .ToList());
}

/// <summary>
/// SQL-translatable projections for EF queries (used in <c>Select</c> so only the
/// needed columns are fetched). Name concatenations stay translatable —
/// <c>FullName</c> is an unmapped computed property and must not appear here.
/// </summary>
public static class Projections
{
    public static readonly Expression<Func<Property, PropertyDto>> ToPropertyDto =
        p => new PropertyDto(p.Id, p.OwnerId, p.ManagingAgencyId, p.Label, p.AddressLine1,
            p.AddressLine2, p.City, p.PostalCode, p.Country, p.Type, p.Status,
            p.SurfaceM2, p.Rooms, p.RentAmount, p.ChargesAmount);

    public static readonly Expression<Func<Lease, LeaseDto>> ToLeaseDto =
        l => new LeaseDto(l.Id, l.PropertyId, l.TenantId,
            l.Tenant.FirstName == ""
                ? l.Tenant.Email
                : l.Tenant.FirstName + " " + l.Tenant.LastName,
            l.StartDate, l.EndDate,
            l.RentAmount, l.ChargesAmount, l.DepositAmount, l.Status);

    public static readonly Expression<Func<Payment, PaymentDto>> ToPaymentDto =
        p => new PaymentDto(p.Id, p.LeaseId, p.Amount, p.PeriodYear, p.PeriodMonth,
            p.PaidAtUtc, p.Method, p.Status, p.Receipt != null);

    public static readonly Expression<Func<RentReceipt, ReceiptDto>> ToReceiptDto =
        r => new ReceiptDto(r.Id, r.PaymentId, r.LeaseId, r.Number, r.IssuedAtUtc,
            r.PeriodStart, r.PeriodEnd, r.RentAmount, r.ChargesAmount);

    public static readonly Expression<Func<Document, DocumentDto>> ToDocumentDto =
        d => new DocumentDto(d.Id, d.PropertyId, d.LeaseId, d.Type, d.FileName,
            d.ContentType, d.SizeBytes, d.CreatedAt);

    public static readonly Expression<Func<Notification, NotificationDto>> ToNotificationDto =
        n => new NotificationDto(n.Id, n.Type, n.Title, n.Body, n.IsRead, n.CreatedAt);

    public static readonly Expression<Func<Message, MessageDto>> ToMessageDto =
        m => new MessageDto(m.Id, m.SenderId,
            m.Sender.FirstName + " " + m.Sender.LastName, m.Body, m.SentAtUtc);
}
