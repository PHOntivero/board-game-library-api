using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BoardGameLibrary.Api.HealthChecks;

internal static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    internal static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.ContentType = "application/json";

        var response = new HealthResponse(
            FormatStatus(report.Status),
            report.Entries
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new HealthCheckResponse(
                    entry.Key,
                    FormatStatus(entry.Value.Status),
                    entry.Value.Duration,
                    entry.Value.Description))
                .ToArray());

        return JsonSerializer.SerializeAsync(
            context.Response.Body,
            response,
            SerializerOptions,
            context.RequestAborted);
    }

    private static string FormatStatus(HealthStatus status) =>
        status.ToString().ToLowerInvariant();

    private sealed record HealthResponse(
        string Status,
        IReadOnlyList<HealthCheckResponse> Checks);

    private sealed record HealthCheckResponse(
        string Name,
        string Status,
        TimeSpan Duration,
        string? Description);
}
