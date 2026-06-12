using Imhotep.Domain.Common;
using Imhotep.Domain.Enums;

namespace Imhotep.Domain.Entities;

public class Property : BaseEntity
{
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    /// <summary>Agency currently managing this property, when delegated.</summary>
    public Guid? ManagingAgencyId { get; set; }
    public User? ManagingAgency { get; set; }

    public string Label { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "France";

    public PropertyType Type { get; set; }
    public PropertyStatus Status { get; set; } = PropertyStatus.Available;
    public decimal SurfaceM2 { get; set; }
    public int Rooms { get; set; }
    public decimal RentAmount { get; set; }
    public decimal ChargesAmount { get; set; }

    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();

    public bool IsManagedBy(Guid userId) => OwnerId == userId || ManagingAgencyId == userId;
}
