using FastEndpoints;
using Imhotep.Application.Common.Models;
using Imhotep.Application.Features.Documents;
using Imhotep.Domain.Enums;
using MediatR;

namespace Imhotep.Api.Endpoints;

public class UploadDocumentRequest
{
    public IFormFile File { get; set; } = null!;
    public DocumentType Type { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? LeaseId { get; set; }
}

public class UploadDocumentEndpoint(ISender sender) : Endpoint<UploadDocumentRequest, DocumentDto>
{
    public override void Configure()
    {
        Post("/api/documents");
        AllowFileUploads();
    }

    public override async Task HandleAsync(UploadDocumentRequest req, CancellationToken ct)
    {
        if (req.File is null || req.File.Length == 0)
        {
            AddError("A file is required.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await using var stream = req.File.OpenReadStream();
        var dto = await sender.Send(new UploadDocumentCommand(
            stream,
            req.File.FileName,
            req.File.ContentType,
            req.File.Length,
            req.Type,
            req.PropertyId,
            req.LeaseId), ct);
        await SendAsync(dto, StatusCodes.Status201Created, ct);
    }
}

public class ListDocumentsRequest
{
    public Guid? PropertyId { get; set; }
    public Guid? LeaseId { get; set; }
}

public class ListDocumentsEndpoint(ISender sender) : Endpoint<ListDocumentsRequest, IReadOnlyList<DocumentDto>>
{
    public override void Configure()
    {
        Get("/api/documents");
    }

    public override async Task HandleAsync(ListDocumentsRequest req, CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new ListDocumentsQuery(req.PropertyId, req.LeaseId), ct), ct);
    }
}

public class DownloadDocumentRequest
{
    public Guid Id { get; set; }
}

public class DownloadDocumentEndpoint(ISender sender) : Endpoint<DownloadDocumentRequest>
{
    public override void Configure()
    {
        Get("/api/documents/{id}/download");
    }

    public override async Task HandleAsync(DownloadDocumentRequest req, CancellationToken ct)
    {
        var file = await sender.Send(new DownloadDocumentQuery(req.Id), ct);
        await SendBytesAsync(file.Content, file.FileName, file.ContentType, cancellation: ct);
    }
}
