using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StoreApi.Application.Common.Exceptions;

namespace StoreApi.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 <see cref="ProblemDetails"/> responses so
/// controllers never need try/catch blocks and stack traces never reach the client.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.RequestAborted.IsCancellationRequested)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problem) = exception switch
        {
            AppException app => (app.StatusCode, Create(app.StatusCode, app.Title, app.Message)),
            Domain.Exceptions.DomainException domain =>
                (StatusCodes.Status400BadRequest,
                    Create(StatusCodes.Status400BadRequest, "Business rule violated", domain.Message)),
            _ => (StatusCodes.Status500InternalServerError,
                    Create(
                        StatusCodes.Status500InternalServerError,
                        "An unexpected error occurred",
                        environment.IsDevelopment()
                            ? exception.Message
                            : "The server encountered an unexpected condition."))
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Handled error {StatusCode} for {Method} {Path}",
                statusCode, context.Request.Method, context.Request.Path);
        }

        if (context.Response.HasStarted)
        {
            logger.LogWarning("Response already started; cannot write ProblemDetails for {Path}",
                context.Request.Path);
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions));
    }

    private static ProblemDetails Create(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Type = $"https://httpstatuses.io/{status}"
    };
}
