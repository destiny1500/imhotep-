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

namespace Imhotep.Application.Features.Documents;

public record UploadDocumentCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes,
    DocumentType Type,
    Guid? PropertyId,
    Guid? LeaseId) : IRequest<DocumentDto>, IAuditableCommand
{
    public string AuditAction => "document.upload";
    public string? AuditEntityType => nameof(Document);
}

public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private const long MaxSizeBytes = 20 * 1024 * 1024; // 20 MB
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf", "image/png", "image/jpeg",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.SizeBytes).GreaterThan(0).LessThanOrEqualTo(MaxSizeBytes)
            .WithMessage("File must be 20 MB or smaller.");
        RuleFor(x => x.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Only PDF, PNG, JPEG and DOCX files are accepted.");
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x)
            .Must(x => x.PropertyId.HasValue || x.LeaseId.HasValue)
            .WithMessage("A document must be attached to a property or a lease.");
    }
}

public class UploadDocumentCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IDocumentStorageService storage,
    IClock clock,
    IMapper mapper)
    : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        Guid? tenantToNotify = null;

        if (request.LeaseId.HasValue)
        {
            var lease = await db.Leases.AsNoTracking()
                .Include(l => l.Property)
                .FirstOrDefaultAsync(l => l.Id == request.LeaseId.Value, ct)
                ?? throw new NotFoundException(nameof(Lease), request.LeaseId.Value);
            if (!lease.Property.IsManagedBy(userId) && lease.TenantId != userId)
                throw new NotFoundException(nameof(Lease), request.LeaseId.Value);
            if (lease.Property.IsManagedBy(userId))
                tenantToNotify = lease.TenantId;
        }
        else
        {
            var property = await db.Properties.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.PropertyId!.Value, ct)
                ?? throw new NotFoundException(nameof(Property), request.PropertyId!.Value);
            if (!property.IsManagedBy(userId))
                throw new NotFoundException(nameof(Property), request.PropertyId!.Value);
        }

        // Encrypted at rest; the storage path is an opaque random name.
        var storagePath = await storage.SaveAsync(request.Content, ct);

        var document = new Document
        {
            PropertyId = request.PropertyId,
            LeaseId = request.LeaseId,
            Type = request.Type,
            FileName = Path.GetFileName(request.FileName), // strip any path components
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            StoragePath = storagePath,
            UploadedById = userId,
            CreatedAt = clock.UtcNow
        };
        db.Documents.Add(document);

        if (tenantToNotify.HasValue)
        {
            db.Notifications.Add(new Notification
            {
                UserId = tenantToNotify.Value,
                Type = NotificationType.DocumentAdded,
                Title = "Nouveau document",
                Body = $"Le document « {document.FileName} » a été ajouté à votre dossier.",
                CreatedAt = clock.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        return mapper.Map<DocumentDto>(document);
    }
}
