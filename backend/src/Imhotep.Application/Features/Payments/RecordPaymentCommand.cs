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

namespace Imhotep.Application.Features.Payments;

public record RecordPaymentCommand(
    Guid LeaseId,
    decimal Amount,
    int PeriodYear,
    int PeriodMonth,
    DateTime PaidAtUtc,
    PaymentMethod Method) : IRequest<PaymentDto>, IAuditableCommand
{
    public string AuditAction => "payment.record";
    public string? AuditEntityType => nameof(Payment);
}

public class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.LeaseId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThan(1_000_000);
        RuleFor(x => x.PeriodYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.PeriodMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.Method).IsInEnum();
    }
}

public class RecordPaymentCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock, IMapper mapper)
    : IRequestHandler<RecordPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(RecordPaymentCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var lease = await db.Leases
            .Include(l => l.Property)
            .FirstOrDefaultAsync(l => l.Id == request.LeaseId, ct)
            ?? throw new NotFoundException(nameof(Lease), request.LeaseId);

        if (!lease.Property.IsManagedBy(userId))
            throw new NotFoundException(nameof(Lease), request.LeaseId);

        var alreadyPaid = await db.Payments.AnyAsync(p =>
            p.LeaseId == lease.Id &&
            p.PeriodYear == request.PeriodYear &&
            p.PeriodMonth == request.PeriodMonth &&
            p.Status == PaymentStatus.Completed, ct);
        if (alreadyPaid)
            throw new ConflictException("A payment is already recorded for this period.");

        var payment = new Payment
        {
            LeaseId = lease.Id,
            Amount = request.Amount,
            PeriodYear = request.PeriodYear,
            PeriodMonth = request.PeriodMonth,
            PaidAtUtc = request.PaidAtUtc,
            Method = request.Method,
            Status = PaymentStatus.Completed,
            RecordedById = userId,
            CreatedAt = clock.UtcNow
        };
        db.Payments.Add(payment);
        db.Notifications.Add(new Notification
        {
            UserId = lease.TenantId,
            Type = NotificationType.PaymentRecorded,
            Title = "Paiement enregistré",
            Body = $"Votre paiement de {request.Amount:0.00} € ({request.PeriodMonth:00}/{request.PeriodYear}) a été enregistré.",
            CreatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return mapper.Map<PaymentDto>(payment);
    }
}
