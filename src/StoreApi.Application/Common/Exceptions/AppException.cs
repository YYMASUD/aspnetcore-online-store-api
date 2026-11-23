namespace StoreApi.Application.Common.Exceptions;

/// <summary>Base class for errors that map to a specific HTTP status code.</summary>
public abstract class AppException(int statusCode, string title, string message) : Exception(message)
{
    /// <summary>The HTTP status code returned by the API.</summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>The ProblemDetails title.</summary>
    public string Title { get; } = title;
}

/// <summary>The requested resource does not exist (404).</summary>
public sealed class NotFoundException(string message)
    : AppException(404, "Resource not found", message);

/// <summary>The request is malformed or violates a business rule (400).</summary>
public sealed class BadRequestException(string message)
    : AppException(400, "Bad request", message);

/// <summary>The request conflicts with current state (409).</summary>
public sealed class ConflictException(string message)
    : AppException(409, "Conflict", message);

/// <summary>The caller is authenticated but not permitted (403).</summary>
public sealed class ForbiddenException(string message)
    : AppException(403, "Forbidden", message);

/// <summary>The caller is not authenticated (401).</summary>
public sealed class UnauthorizedException(string message)
    : AppException(401, "Unauthorized", message);
