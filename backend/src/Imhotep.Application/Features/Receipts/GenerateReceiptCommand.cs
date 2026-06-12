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

namespace Imhotep.Application.Features.Receipts;

public record GenerateReceiptCommand(Guid PaymentId) : IRequest<ReceiptDto>, IAuditableCommand
{
    public string AuditAction => "receipt.generate";
    public string? AuditEntityType => nameof(RentReceipt);
    public string? AuditEntityId => PaymentId.ToString();
}

public class GenerateReceiptCommandValidator : AbstractValidator<GenerateReceiptCommand>
{
    public GenerateReceiptCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
    }
}

public class GenerateReceiptCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock, IMapper mapper)
    : IRequestHandler<GenerateReceiptCommand, ReceiptDto>
{
    public async Task<ReceiptDto> Handle(GenerateReceiptCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var payment = await db.Payments
            .Include(p => p.Receipt)
            .Include(p => p.Lease).ThenInclude(l => l.Property)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        if (!payment.Lease.Property.IsManagedBy(userId))
            throw new NotFoundException(nameof(Payment), request.PaymentId);

        if (payment.Receipt is not null)
            throw new ConflictException("A receipt has already been issued for this payment.");
        if (payment.Status != PaymentStatus.Completed)
            throw new ConflictException("A receipt can only be issued for a completed payment.");

        var now = clock.UtcNow;
        var sequence = await db.RentReceipts.CountAsync(r => r.IssuedAtUtc.Year == now.Year, ct) + 1;
        var periodStart = new DateOnly(payment.PeriodYear, payment.PeriodMonth, 1);

        var receipt = new RentReceipt
        {
            PaymentId = payment.Id,
            LeaseId = payment.LeaseId,
            Number = $"Q-{now.Year}-{sequence:000000}",
            IssuedAtUtc = now,
            PeriodStart = periodStart,
            PeriodEnd = periodStart.AddMonths(1).AddDays(-1),
            RentAmount = payment.Lease.RentAmount,
            ChargesAmount = payment.Lease.ChargesAmount,
            IssuedById = userId,
            CreatedAt = now
        };
        db.RentReceipts.Add(receipt);
        db.Notifications.Add(new Notification
        {
            UserId = payment.Lease.TenantId,
            Type = NotificationType.ReceiptIssued,
            Title = "Quittance disponible",
            Body = $"La quittance {receipt.Number} ({payment.PeriodMonth:00}/{payment.PeriodYear}) est disponible.",
            CreatedAt = now
        });
        await db.SaveChangesAsync(ct);
        return mapper.Map<ReceiptDto>(receipt);
    }
}
