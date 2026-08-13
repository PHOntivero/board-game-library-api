using System.Diagnostics;

namespace BoardGameLibrary.Api.Tracing;

internal static class TraceContext
{
    internal static string GetTraceId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? activityTraceId = Activity.Current?.TraceId.ToString();
        return string.IsNullOrWhiteSpace(activityTraceId)
            ? context.TraceIdentifier
            : activityTraceId;
    }
}
