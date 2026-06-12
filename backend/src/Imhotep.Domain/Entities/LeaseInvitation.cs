using Imhotep.Domain.Common;

namespace Imhotep.Domain.Entities;

/// <summary>
/// Invitation sent to a tenant who has no account yet. The lease is created in
/// Pending status with a placeholder (inactive) user; accepting the invitation
/// activates the account and the lease. The token is stored hashed (SHA-256).
/// </summary>
public class LeaseInvitation : BaseEntity
{
    public Guid LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public Guid InvitedById { get; set; }
    public User InvitedBy { get; set; } = null!;

    public bool IsUsable(DateTime nowUtc) => AcceptedAtUtc is null && nowUtc < ExpiresAtUtc;
}
