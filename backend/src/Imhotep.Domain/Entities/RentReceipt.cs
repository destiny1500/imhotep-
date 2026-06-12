using Imhotep.Domain.Common;

namespace Imhotep.Domain.Entities;

/// <summary>Quittance de loyer issued for a completed payment.</summary>
public class RentReceipt : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public Guid LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    /// <summary>Sequential human-readable number, e.g. "Q-2026-000042".</summary>
    public string Number { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal RentAmount { get; set; }
    public decimal ChargesAmount { get; set; }

    public Guid IssuedById { get; set; }
    public User IssuedBy { get; set; } = null!;
}
