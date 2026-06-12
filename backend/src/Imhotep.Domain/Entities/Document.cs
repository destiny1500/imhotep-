using Imhotep.Domain.Common;
using Imhotep.Domain.Enums;

namespace Imhotep.Domain.Entities;

/// <summary>
/// Metadata for a stored document. The binary content is encrypted at rest
/// (AES-256-GCM) and stored outside the database; <see cref="StoragePath"/> is
/// an opaque random name, never derived from user input.
/// </summary>
public class Document : BaseEntity
{
    public Guid? PropertyId { get; set; }
    public Property? Property { get; set; }
    public Guid? LeaseId { get; set; }
    public Lease? Lease { get; set; }

    public DocumentType Type { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;

    public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;
}
