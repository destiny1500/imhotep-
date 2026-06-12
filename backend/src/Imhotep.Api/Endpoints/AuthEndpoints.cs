using FastEndpoints;
using Imhotep.Application.Common.Models;
using Imhotep.Application.Features.Auth;
using Imhotep.Domain.Enums;
using MediatR;

namespace Imhotep.Api.Endpoints;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName, UserRole Role);
public record RegisterResponse(Guid UserId);

public class RegisterEndpoint(ISender sender) : Endpoint<RegisterRequest, RegisterResponse>
{
    public override void Configure()
    {
        Post("/api/auth/register");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("auth"));
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var userId = await sender.Send(
            new RegisterCommand(req.Email, req.Password, req.FirstName, req.LastName, req.Role), ct);
        await SendAsync(new RegisterResponse(userId), StatusCodes.Status201Created, ct);
    }
}

public record LoginRequest(string Email, string Password);

public class LoginEndpoint(ISender sender) : Endpoint<LoginRequest, AuthResultDto>
{
    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("auth"));
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(req.Email, req.Password), ct);
        await SendOkAsync(result, ct);
    }
}

public record RefreshRequest(string RefreshToken);

public class RefreshEndpoint(ISender sender) : Endpoint<RefreshRequest, TokenPairDto>
{
    public override void Configure()
    {
        Post("/api/auth/refresh");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("auth"));
    }

    public override async Task HandleAsync(RefreshRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new RefreshTokenCommand(req.RefreshToken), ct);
        await SendOkAsync(result, ct);
    }
}

public record LogoutRequest(string RefreshToken);

public class LogoutEndpoint(ISender sender) : Endpoint<LogoutRequest>
{
    public override void Configure()
    {
        Post("/api/auth/logout");
        // Authenticated: revoking a token requires a valid session.
    }

    public override async Task HandleAsync(LogoutRequest req, CancellationToken ct)
    {
        await sender.Send(new LogoutCommand(req.RefreshToken), ct);
        await SendNoContentAsync(ct);
    }
}

public class MeEndpoint(ISender sender) : EndpointWithoutRequest<UserDto>
{
    public override void Configure()
    {
        Get("/api/users/me");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(await sender.Send(new GetMeQuery(), ct), ct);
    }
}
