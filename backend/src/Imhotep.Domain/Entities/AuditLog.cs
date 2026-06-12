namespace Imhotep.Domain.Entities;

/// <summary>
/// Immutable trace of sensitive actions (writes, auth events, document access).
/// Payloads are never stored verbatim to avoid persisting secrets; only
/// non-sensitive metadata goes into <see cref="MetadataJson"/> (JSONB).
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime TimestampUtc { get; set; }
}
