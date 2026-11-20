#pragma warning disable CA1848
using System.Text.Json;
using ClinicBook.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBook.Api.Middleware;

/// <summary>
/// Global exception handling middleware that returns RFC 7807 ProblemDetails.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>Initializes the middleware.</summary>
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>Invokes the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            _logger.LogWarning(validationException, "Validation failed. TraceId: {TraceId}", traceId);
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Validation failed",
                "One or more validation errors occurred.", traceId, validationException.Errors).ConfigureAwait(false);
            return;
        }

        if (exception is ForbiddenAccessException forbiddenException)
        {
            _logger.LogWarning(forbiddenException, "Forbidden access. TraceId: {TraceId}", traceId);
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "Forbidden",
                "You do not have permission to perform this action.", traceId).ConfigureAwait(false);
            return;
        }

        _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);

        var detail = _environment.IsDevelopment()
            ? exception.Message
            : "An unexpected error occurred. Please try again later.";

        await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Server error",
            detail, traceId).ConfigureAwait(false);
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string traceId,
        IDictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = traceId;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions)).ConfigureAwait(false);
    }
}
#pragma warning restore CA1848
