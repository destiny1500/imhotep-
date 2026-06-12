using FastEndpoints;
using Imhotep.Application.Common.Models;
using Imhotep.Application.Features.Payments;
using Imhotep.Domain.Enums;
using MediatR;

namespace Imhotep.Api.Endpoints;

public class ListPaymentsRequest
{
    public Guid LeaseId { get; set; }
}

public class ListPaymentsEndpoint(ISender sender) : Endpoint<ListPaymentsRequest, IReadOnlyList<PaymentDto>>
{
    public override void Configure()
    {
        Get("/api/payments");
    }

    public override async Task HandleAsync(ListPaymentsRequest req, CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new ListPaymentsQuery(req.LeaseId), ct), ct);
    }
}

public class GetMyPaymentsEndpoint(ISender sender) : EndpointWithoutRequest<IReadOnlyList<PaymentDto>>
{
    public override void Configure()
    {
        Get("/api/payments/my");
        Roles(nameof(UserRole.Tenant));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new GetMyPaymentsQuery(), ct), ct);
    }
}

public record RecordPaymentRequest(
    Guid LeaseId,
    decimal Amount,
    int PeriodYear,
    int PeriodMonth,
    DateTime PaidAt,
    PaymentMethod Method);

public class RecordPaymentEndpoint(ISender sender) : Endpoint<RecordPaymentRequest, PaymentDto>
{
    public override void Configure()
    {
        Post("/api/payments");
        Roles(nameof(UserRole.Owner), nameof(UserRole.Agency));
    }

    public override async Task HandleAsync(RecordPaymentRequest req, CancellationToken ct)
    {
        var dto = await sender.Send(new RecordPaymentCommand(
            req.LeaseId, req.Amount, req.PeriodYear, req.PeriodMonth,
            DateTime.SpecifyKind(req.PaidAt, DateTimeKind.Utc), req.Method), ct);
        await SendAsync(dto, StatusCodes.Status201Created, ct);
    }
}
