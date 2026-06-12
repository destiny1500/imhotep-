using Imhotep.Domain.Common;

namespace Imhotep.Domain.Entities;

/// <summary>
/// Refresh tokens are stored hashed (SHA-256). Rotation: each refresh issues a new
/// token and revokes the previous one; reuse of a revoked token revokes the whole chain.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsActive(DateTime nowUtc) => !IsRevoked && !IsExpired(nowUtc);
}
