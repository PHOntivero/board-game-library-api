using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using BoardGameLibrary.Api.ErrorHandling;
using BoardGameLibrary.Api.HealthChecks;
using BoardGameLibrary.Api.Tracing;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Instance = context.HttpContext.Request.Path,
        };

        problemDetails.Extensions["code"] = ErrorCodes.Common.ValidationFailed;
        problemDetails.Extensions["traceId"] = TraceContext.GetTraceId(context.HttpContext);

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" },
        };
    };
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd(
            "traceId",
            TraceContext.GetTraceId(context.HttpContext));

        if (context.ProblemDetails is ValidationProblemDetails)
        {
            context.ProblemDetails.Extensions.TryAdd(
                "code",
                ErrorCodes.Common.ValidationFailed);
        }
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi("v1");
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

string connectionString = builder.Configuration.GetConnectionString(
        DependencyInjection.ConnectionStringName)
    ?? throw new InvalidOperationException(
        $"Connection string '{DependencyInjection.ConnectionStringName}' is required.");

builder.Services.AddInfrastructure(connectionString);
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: [HealthCheckTags.Live])
    .AddCheck<DatabaseHealthCheck>(
        "database",
        tags: [HealthCheckTags.Ready]);

var app = builder.Build();

app.UseMiddleware<TraceIdentifierMiddleware>();
app.UseExceptionHandler();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi("/openapi/{documentName}.json");
}

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains(HealthCheckTags.Live),
        ResponseWriter = HealthCheckResponseWriter.WriteAsync,
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready),
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
        },
        ResponseWriter = HealthCheckResponseWriter.WriteAsync,
    });

app.MapControllers();

app.Run();

public partial class Program;
