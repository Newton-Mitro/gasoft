using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace Gasoft.Api.Middleware;

public sealed class BadRequestBodyExceptionHandler(
    ILogger<BadRequestBodyExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException badRequest ||
            badRequest.StatusCode != StatusCodes.Status400BadRequest)
        {
            return false;
        }

        var jsonException = FindJsonException(badRequest);
        var field = FieldFromJsonPath(jsonException?.Path);
        var message = GetValidationMessage(badRequest, field);

        logger.LogInformation(
            "Rejected malformed request body for {Method} {Path}, field {Field}.",
            httpContext.Request.Method,
            httpContext.Request.Path,
            field);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await Results.ValidationProblem(
            new Dictionary<string, string[]> { [field] = [message] },
            title: "The request contains invalid input.")
            .ExecuteAsync(httpContext);

        return true;
    }

    private static JsonException? FindJsonException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is JsonException jsonException)
            {
                return jsonException;
            }
        }

        return null;
    }

    private static string GetValidationMessage(Exception exception, string field)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current.Message.Contains(nameof(DateOnly), StringComparison.Ordinal))
            {
                return "Enter a valid date in YYYY-MM-DD format.";
            }

            if (current is InvalidOperationException &&
                current.Message.Contains("token type 'Null' as a number", StringComparison.OrdinalIgnoreCase))
            {
                return "This field is required and must be a number.";
            }
        }

        var fieldName = field[(field.LastIndexOf('.') + 1)..];
        if (fieldName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
        {
            return "Enter a valid whole-number identifier.";
        }

        if (fieldName.EndsWith("Date", StringComparison.OrdinalIgnoreCase) ||
            fieldName.EndsWith("On", StringComparison.OrdinalIgnoreCase))
        {
            return "Enter a valid date in YYYY-MM-DD format.";
        }

        return "The value has an invalid format.";
    }

    private static string FieldFromJsonPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "$")
        {
            return "request";
        }

        var field = path.StartsWith("$.", StringComparison.Ordinal)
            ? path[2..]
            : path.TrimStart('$', '.');
        field = field.Replace("['", ".", StringComparison.Ordinal)
            .Replace("']", string.Empty, StringComparison.Ordinal)
            .Replace("[\"", ".", StringComparison.Ordinal)
            .Replace("\"]", string.Empty, StringComparison.Ordinal);
        return field.Length > 0 ? field : "request";
    }
}
