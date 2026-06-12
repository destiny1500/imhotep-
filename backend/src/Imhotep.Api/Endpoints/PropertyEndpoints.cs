using FastEndpoints;
using Imhotep.Application.Common.Models;
using Imhotep.Application.Features.Properties;
using Imhotep.Domain.Enums;
using MediatR;

namespace Imhotep.Api.Endpoints;

public class ListPropertiesEndpoint(ISender sender) : EndpointWithoutRequest<IReadOnlyList<PropertyDto>>
{
    public override void Configure()
    {
        Get("/api/properties");
        Roles(nameof(UserRole.Owner), nameof(UserRole.Agency), nameof(UserRole.Admin));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new ListPropertiesQuery(), ct), ct);
    }
}

public record CreatePropertyRequest(
    string Label,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string PostalCode,
    string Country,
    PropertyType Type,
    decimal SurfaceM2,
    int Rooms,
    decimal RentAmount,
    decimal ChargesAmount);

public class CreatePropertyEndpoint(ISender sender) : Endpoint<CreatePropertyRequest, PropertyDto>
{
    public override void Configure()
    {
        Post("/api/properties");
        Roles(nameof(UserRole.Owner));
    }

    public override async Task HandleAsync(CreatePropertyRequest req, CancellationToken ct)
    {
        var dto = await sender.Send(new CreatePropertyCommand(
            req.Label, req.AddressLine1, req.AddressLine2, req.City, req.PostalCode, req.Country,
            req.Type, req.SurfaceM2, req.Rooms, req.RentAmount, req.ChargesAmount), ct);
        await SendAsync(dto, StatusCodes.Status201Created, ct);
    }
}

// Route/query-bound DTOs are mutable classes: FastEndpoints sets these properties
// by reflection after instantiation, which positional records don't allow.
public class GetPropertyRequest
{
    public Guid Id { get; set; }
}

public class GetPropertyEndpoint(ISender sender) : Endpoint<GetPropertyRequest, PropertyDto>
{
    public override void Configure()
    {
        Get("/api/properties/{id}");
    }

    public override async Task HandleAsync(GetPropertyRequest req, CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new GetPropertyQuery(req.Id), ct), ct);
    }
}

public class UpdatePropertyRequest
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public PropertyType Type { get; set; }
    public PropertyStatus Status { get; set; }
    public decimal SurfaceM2 { get; set; }
    public int Rooms { get; set; }
    public decimal RentAmount { get; set; }
    public decimal ChargesAmount { get; set; }
}

public class UpdatePropertyEndpoint(ISender sender) : Endpoint<UpdatePropertyRequest, PropertyDto>
{
    public override void Configure()
    {
        Put("/api/properties/{id}");
        Roles(nameof(UserRole.Owner), nameof(UserRole.Agency));
    }

    public override async Task HandleAsync(UpdatePropertyRequest req, CancellationToken ct)
    {
        var dto = await sender.Send(new UpdatePropertyCommand(
            req.Id, req.Label, req.AddressLine1, req.AddressLine2, req.City, req.PostalCode, req.Country,
            req.Type, req.Status, req.SurfaceM2, req.Rooms, req.RentAmount, req.ChargesAmount), ct);
        await SendOkAsync(dto, ct);
    }
}

public class DelegatePropertyRequest
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public decimal FeePercent { get; set; }
}

public class DelegatePropertyEndpoint(ISender sender) : Endpoint<DelegatePropertyRequest>
{
    public override void Configure()
    {
        Post("/api/properties/{id}/delegate");
        Roles(nameof(UserRole.Owner));
    }

    public override async Task HandleAsync(DelegatePropertyRequest req, CancellationToken ct)
    {
        await sender.Send(new DelegatePropertyCommand(req.Id, req.AgencyId, req.FeePercent), ct);
        await SendOkAsync(ct);
    }
}
