namespace BoardGameLibrary.Api.Tracing;

public sealed class TraceIdentifierMiddleware(
    RequestDelegate next,
    ILogger<TraceIdentifierMiddleware> logger)
{
    public const string ResponseHeaderName = "X-Trace-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        string traceId = TraceContext.GetTraceId(context);
        context.TraceIdentifier = traceId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ResponseHeaderName] = context.TraceIdentifier;
            return Task.CompletedTask;
        });

        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
        });

        await next(context);
    }
}
