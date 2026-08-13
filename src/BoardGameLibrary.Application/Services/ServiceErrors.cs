using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Application.Services;

internal static class ServiceErrors
{
    internal static Error NotFound(string code, string resourceName) =>
        Error.NotFound(code, $"{resourceName} was not found.");

    internal static Error Conflict(string code, string description) =>
        Error.Conflict(code, description);

    internal static Error Validation(string description) =>
        Error.Validation(ErrorCodes.Common.ValidationFailed, description);
}
