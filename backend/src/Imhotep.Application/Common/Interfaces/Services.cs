using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;

namespace Imhotep.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    UserRole? Role { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    string CreateAccessToken(User user);
    /// <summary>Returns the raw refresh token (to give to the client) and its SHA-256 hash (to persist).</summary>
    (string RawToken, string TokenHash) CreateRefreshToken();
    string HashToken(string rawToken);
    TimeSpan RefreshTokenLifetime { get; }
}

public interface IDocumentStorageService
{
    /// <summary>Encrypts and stores content; returns the opaque storage path.</summary>
    Task<string> SaveAsync(Stream content, CancellationToken ct);
    /// <summary>Decrypts and returns the document content.</summary>
    Task<byte[]> ReadAsync(string storagePath, CancellationToken ct);
}

public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>Builds the public frontend URL a tenant follows to accept an invitation.</summary>
public interface IInvitationLinkBuilder
{
    string BuildInvitationUrl(string rawToken);
}

/// <summary>Outbound e-mail. The default implementation only logs; plug an SMTP/API
/// provider here in production.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}
