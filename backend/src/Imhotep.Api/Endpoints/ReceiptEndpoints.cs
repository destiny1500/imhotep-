using FastEndpoints;
using Imhotep.Application.Common.Models;
using Imhotep.Application.Features.Receipts;
using Imhotep.Domain.Enums;
using MediatR;

namespace Imhotep.Api.Endpoints;

public class GetMyReceiptsEndpoint(ISender sender) : EndpointWithoutRequest<IReadOnlyList<ReceiptDto>>
{
    public override void Configure()
    {
        Get("/api/receipts/my");
        Roles(nameof(UserRole.Tenant));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new GetMyReceiptsQuery(), ct), ct);
    }
}

public class ListReceiptsRequest
{
    public Guid LeaseId { get; set; }
}

public class ListReceiptsEndpoint(ISender sender) : Endpoint<ListReceiptsRequest, IReadOnlyList<ReceiptDto>>
{
    public override void Configure()
    {
        Get("/api/receipts");
    }

    public override async Task HandleAsync(ListReceiptsRequest req, CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new ListReceiptsQuery(req.LeaseId), ct), ct);
    }
}

public record GenerateReceiptRequest(Guid PaymentId);

public class GenerateReceiptEndpoint(ISender sender) : Endpoint<GenerateReceiptRequest, ReceiptDto>
{
    public override void Configure()
    {
        Post("/api/receipts/generate");
        Roles(nameof(UserRole.Owner), nameof(UserRole.Agency));
    }

    public override async Task HandleAsync(GenerateReceiptRequest req, CancellationToken ct)
    {
        var dto = await sender.Send(new GenerateReceiptCommand(req.PaymentId), ct);
        await SendAsync(dto, StatusCodes.Status201Created, ct);
    }
}

public class DownloadReceiptRequest
{
    public Guid Id { get; set; }
}

public class DownloadReceiptEndpoint(ISender sender) : Endpoint<DownloadReceiptRequest>
{
    public override void Configure()
    {
        Get("/api/receipts/{id}/download");
    }

    public override async Task HandleAsync(DownloadReceiptRequest req, CancellationToken ct)
    {
        var file = await sender.Send(new DownloadReceiptQuery(req.Id), ct);
        await SendBytesAsync(file.Content, file.FileName, file.ContentType, cancellation: ct);
    }
}
