using FastEndpoints;
using Imhotep.Application.Common.Models;
using Imhotep.Application.Features.Dashboard;
using Imhotep.Domain.Enums;
using MediatR;

namespace Imhotep.Api.Endpoints;

public class OwnerDashboardEndpoint(ISender sender) : EndpointWithoutRequest<OwnerDashboardDto>
{
    public override void Configure()
    {
        Get("/api/dashboard/owner");
        Roles(nameof(UserRole.Owner));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new GetOwnerDashboardQuery(), ct), ct);
    }
}

public class AgencyDashboardEndpoint(ISender sender) : EndpointWithoutRequest<AgencyDashboardDto>
{
    public override void Configure()
    {
        Get("/api/dashboard/agency");
        Roles(nameof(UserRole.Agency));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new GetAgencyDashboardQuery(), ct), ct);
    }
}
