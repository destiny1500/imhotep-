using FastEndpoints;
using Imhotep.Application.Common.Models;
using Imhotep.Application.Features.Notifications;
using MediatR;

namespace Imhotep.Api.Endpoints;

public class ListNotificationsEndpoint(ISender sender) : EndpointWithoutRequest<IReadOnlyList<NotificationDto>>
{
    public override void Configure()
    {
        Get("/api/notifications");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new ListNotificationsQuery(), ct), ct);
    }
}

public class MarkNotificationReadRequest
{
    public Guid Id { get; set; }
}

public class MarkNotificationReadEndpoint(ISender sender) : Endpoint<MarkNotificationReadRequest>
{
    public override void Configure()
    {
        Post("/api/notifications/{id}/read");
    }

    public override async Task HandleAsync(MarkNotificationReadRequest req, CancellationToken ct)
    {
        await sender.Send(new MarkNotificationReadCommand(req.Id), ct);
        await SendNoContentAsync(ct);
    }
}
