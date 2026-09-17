using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace MyRunshaw.Api.Middleware;

/// <summary>
/// Converts exceptions which escape a controller into a RFC 9110-style
/// problem response.
/// </summary>
public sealed class ApiExceptionHandlerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlerMiddleware> _logger;

    public ApiExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Type = "urn:myrunshaw:problem:internal-server-error",
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
                // Keep the existing "detail" field so older clients can still
                // display a useful error
                Detail = "The server could not complete the request."
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;

            await JsonSerializer.SerializeAsync(context.Response.Body, problem, JsonOptions);
        }
    }
}
