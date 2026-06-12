using FastEndpoints;
using Imhotep.Application.Common.Models;
using Imhotep.Application.Features.Leases;
using Imhotep.Domain.Enums;
using MediatR;

namespace Imhotep.Api.Endpoints;

public class ListLeasesRequest
{
    public Guid PropertyId { get; set; }
}

public class ListLeasesEndpoint(ISender sender) : Endpoint<ListLeasesRequest, IReadOnlyList<LeaseDto>>
{
    public override void Configure()
    {
        Get("/api/leases");
        Roles(nameof(UserRole.Owner), nameof(UserRole.Agency), nameof(UserRole.Admin));
    }

    public override async Task HandleAsync(ListLeasesRequest req, CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new ListLeasesQuery(req.PropertyId), ct), ct);
    }
}

public record CreateLeaseRequest(
    Guid PropertyId,
    string TenantEmail,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal RentAmount,
    decimal ChargesAmount,
    decimal DepositAmount);

public class CreateLeaseEndpoint(ISender sender) : Endpoint<CreateLeaseRequest, LeaseDto>
{
    public override void Configure()
    {
        Post("/api/leases");
        Roles(nameof(UserRole.Owner), nameof(UserRole.Agency));
    }

    public override async Task HandleAsync(CreateLeaseRequest req, CancellationToken ct)
    {
        var dto = await sender.Send(new CreateLeaseCommand(
            req.PropertyId, req.TenantEmail, req.StartDate, req.EndDate,
            req.RentAmount, req.ChargesAmount, req.DepositAmount), ct);
        await SendAsync(dto, StatusCodes.Status201Created, ct);
    }
}

public class GetMyLeaseEndpoint(ISender sender) : EndpointWithoutRequest<MyLeaseDto?>
{
    public override void Configure()
    {
        Get("/api/leases/my");
        Roles(nameof(UserRole.Tenant));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new GetMyLeaseQuery(), ct), ct);
    }
}
