namespace Imhotep.Application.Common.Exceptions;

/// <summary>Mapped to HTTP 404. Also used instead of 403 when revealing the
/// existence of a resource to a non-authorized caller would leak information (anti-IDOR).</summary>
public class NotFoundException(string entity, object key)
    : Exception($"{entity} '{key}' was not found.");

/// <summary>Mapped to HTTP 403.</summary>
public class ForbiddenAccessException(string? message = null)
    : Exception(message ?? "You are not allowed to perform this action.");

/// <summary>Mapped to HTTP 409.</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>Mapped to HTTP 401 (login/refresh failures). Message is intentionally generic.</summary>
public class AuthenticationFailedException(string? message = null)
    : Exception(message ?? "Invalid credentials.");
