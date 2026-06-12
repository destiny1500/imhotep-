using Imhotep.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Imhotep.Infrastructure.Services;

public class FrontendOptions
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; set; } = "http://localhost:5173";
}

public class InvitationLinkBuilder(IOptions<FrontendOptions> options) : IInvitationLinkBuilder
{
    public string BuildInvitationUrl(string rawToken) =>
        $"{options.Value.BaseUrl.TrimEnd('/')}/invitation/{rawToken}";
}

/// <summary>
/// Development e-mail sender: writes the message to the log. Replace with an
/// SMTP/API implementation (registered against <see cref="IEmailSender"/>) in production —
/// the invitation link is also returned to the inviter in the API response, so the
/// flow works end-to-end without a mail server.
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        logger.LogInformation("E-mail (dev) to {To}: {Subject} — {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
