using Imhotep.Domain.Common;
using Imhotep.Domain.Enums;

namespace Imhotep.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    public decimal Amount { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateTime PaidAtUtc { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;

    /// <summary>User (owner or agency) who recorded the payment.</summary>
    public Guid RecordedById { get; set; }
    public User RecordedBy { get; set; } = null!;

    public RentReceipt? Receipt { get; set; }
}
