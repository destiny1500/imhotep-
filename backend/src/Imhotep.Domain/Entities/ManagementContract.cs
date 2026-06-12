using Imhotep.Domain.Common;
using Imhotep.Domain.Enums;

namespace Imhotep.Domain.Entities;

/// <summary>Mandate by which an owner entrusts the management of a property to an agency.</summary>
public class ManagementContract : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public Guid AgencyId { get; set; }
    public User Agency { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal FeePercent { get; set; }
    public ManagementContractStatus Status { get; set; } = ManagementContractStatus.Active;
}
