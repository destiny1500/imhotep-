using FastEndpoints;
using Imhotep.Application.Common.Models;
using Imhotep.Application.Features.Messaging;
using MediatR;

namespace Imhotep.Api.Endpoints;

public class ListConversationsEndpoint(ISender sender) : EndpointWithoutRequest<IReadOnlyList<ConversationDto>>
{
    public override void Configure()
    {
        Get("/api/conversations");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new ListConversationsQuery(), ct), ct);
    }
}

public class ListContactsEndpoint(ISender sender) : EndpointWithoutRequest<IReadOnlyList<ParticipantDto>>
{
    public override void Configure()
    {
        Get("/api/conversations/contacts");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new ListContactsQuery(), ct), ct);
    }
}

public record StartConversationRequest(Guid ParticipantUserId, Guid? PropertyId, string Subject, string Body);
public record StartConversationResponse(Guid ConversationId);

public class StartConversationEndpoint(ISender sender) : Endpoint<StartConversationRequest, StartConversationResponse>
{
    public override void Configure()
    {
        Post("/api/conversations");
    }

    public override async Task HandleAsync(StartConversationRequest req, CancellationToken ct)
    {
        var id = await sender.Send(new StartConversationCommand(
            req.ParticipantUserId, req.PropertyId, req.Subject, req.Body), ct);
        await SendAsync(new StartConversationResponse(id), StatusCodes.Status201Created, ct);
    }
}

public class GetMessagesRequest
{
    public Guid ConversationId { get; set; }
}

public class GetMessagesEndpoint(ISender sender) : Endpoint<GetMessagesRequest, IReadOnlyList<MessageDto>>
{
    public override void Configure()
    {
        Get("/api/conversations/{conversationId}/messages");
    }

    public override async Task HandleAsync(GetMessagesRequest req, CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new GetMessagesQuery(req.ConversationId), ct), ct);
    }
}

public class SendMessageRequest
{
    public Guid ConversationId { get; set; }
    public string Body { get; set; } = string.Empty;
}
public record SendMessageResponse(Guid MessageId);

public class SendMessageEndpoint(ISender sender) : Endpoint<SendMessageRequest, SendMessageResponse>
{
    public override void Configure()
    {
        Post("/api/conversations/{conversationId}/messages");
    }

    public override async Task HandleAsync(SendMessageRequest req, CancellationToken ct)
    {
        var id = await sender.Send(new SendMessageCommand(req.ConversationId, req.Body), ct);
        await SendAsync(new SendMessageResponse(id), StatusCodes.Status201Created, ct);
    }
}
