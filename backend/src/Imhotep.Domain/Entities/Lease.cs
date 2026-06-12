using Imhotep.Domain.Common;
using Imhotep.Domain.Enums;

namespace Imhotep.Domain.Entities;

public class Lease : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public Guid TenantId { get; set; }
    public User Tenant { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public decimal ChargesAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public LeaseStatus Status { get; set; } = LeaseStatus.Active;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
