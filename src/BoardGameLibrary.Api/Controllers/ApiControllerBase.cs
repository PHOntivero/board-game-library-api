using BoardGameLibrary.Api.Tracing;
using BoardGameLibrary.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameLibrary.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult? ValidateIdentifier(Guid identifier, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

        return identifier == Guid.Empty
            ? ToErrorResponse(Result.Failure(Error.Validation(
                ErrorCodes.Common.ValidationFailed,
                $"{parameterName} must be a non-empty identifier.")))
            : null;
    }

    protected static void LogSucceeded(ILogger logger, string operation, Guid resourceId)
    {
        logger.LogInformation(
            "UseCaseSucceeded {Operation} {ResourceId}",
            operation,
            resourceId);
    }

    protected static void LogConflict(
        ILogger logger,
        string operation,
        Result result,
        Guid? resourceId = null)
    {
        if (result.IsFailure && result.Errors[0].Type == ErrorType.Conflict)
        {
            logger.LogWarning(
                "UseCaseConflict {Operation} {ResourceId} {ErrorCode}",
                operation,
                resourceId,
                result.Errors[0].Code);
        }
    }

    protected ActionResult ToErrorResponse(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            throw new ArgumentException("A successful result cannot be converted to an error response.", nameof(result));
        }

        ErrorType errorType = result.Errors[0].Type;
        string code = result.Errors[0].Code;

        if (errorType == ErrorType.Validation)
        {
            var validationProblem = new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["request"] = result.Errors.Select(error => error.Description).ToArray(),
                })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Instance = HttpContext.Request.Path,
            };

            validationProblem.Extensions["code"] = code;
            validationProblem.Extensions["traceId"] = TraceContext.GetTraceId(HttpContext);

            return new BadRequestObjectResult(validationProblem)
            {
                ContentTypes = { "application/problem+json" },
            };
        }

        int statusCode = errorType switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => throw new ArgumentOutOfRangeException(nameof(result), errorType, "Unsupported error type."),
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = errorType == ErrorType.NotFound
                ? "The requested resource was not found."
                : "The request conflicts with the current resource state.",
            Detail = result.Errors[0].Description,
            Type = statusCode == StatusCodes.Status404NotFound
                ? "https://tools.ietf.org/html/rfc9110#section-15.5.5"
                : "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            Instance = HttpContext.Request.Path,
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = TraceContext.GetTraceId(HttpContext);

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" },
        };
    }
}
