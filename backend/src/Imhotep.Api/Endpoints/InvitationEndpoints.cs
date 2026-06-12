using FastEndpoints;
using Imhotep.Application.Common.Models;
using Imhotep.Application.Features.Invitations;
using MediatR;

namespace Imhotep.Api.Endpoints;

public class GetInvitationRequest
{
    public string Token { get; set; } = string.Empty;
}

/// <summary>Anonymous but token-gated: the 256-bit token is the credential.</summary>
public class GetInvitationEndpoint(ISender sender) : Endpoint<GetInvitationRequest, InvitationInfoDto>
{
    public override void Configure()
    {
        Get("/api/invitations/{token}");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("auth"));
    }

    public override async Task HandleAsync(GetInvitationRequest req, CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new GetInvitationQuery(req.Token), ct), ct);
    }
}

public class AcceptInvitationRequest
{
    public string Token { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AcceptInvitationEndpoint(ISender sender) : Endpoint<AcceptInvitationRequest, AuthResultDto>
{
    public override void Configure()
    {
        Post("/api/invitations/{token}/accept");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("auth"));
    }

    public override async Task HandleAsync(AcceptInvitationRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new AcceptInvitationCommand(
            req.Token, req.FirstName, req.LastName, req.Password), ct);
        await SendAsync(result, StatusCodes.Status201Created, ct);
    }
}
