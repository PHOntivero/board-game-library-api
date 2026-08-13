namespace BoardGameLibrary.Domain.Common;

internal static class DomainGuard
{
    internal static string RequiredText(
        string? value,
        int maximumLength,
        string fieldName,
        string codePrefix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                $"{codePrefix}.required",
                $"{fieldName} is required.",
                DomainErrorType.Validation);
        }

        string trimmedValue = value.Trim();

        if (trimmedValue.Length > maximumLength)
        {
            throw new DomainException(
                $"{codePrefix}.too_long",
                $"{fieldName} cannot exceed {maximumLength} characters.",
                DomainErrorType.Validation);
        }

        return trimmedValue;
    }

    internal static string? OptionalText(
        string? value,
        int maximumLength,
        string fieldName,
        string codePrefix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmedValue = value.Trim();

        if (trimmedValue.Length > maximumLength)
        {
            throw new DomainException(
                $"{codePrefix}.too_long",
                $"{fieldName} cannot exceed {maximumLength} characters.",
                DomainErrorType.Validation);
        }

        return trimmedValue;
    }

    internal static void NotEmpty(Guid value, string fieldName, string code)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(code, $"{fieldName} is required.", DomainErrorType.Validation);
        }
    }

    internal static void NotFuture(
        DateOnly value,
        DateOnly todayUtc,
        string fieldName,
        string code)
    {
        if (value > todayUtc)
        {
            throw new DomainException(
                code,
                $"{fieldName} cannot be in the future.",
                DomainErrorType.Validation);
        }
    }

    internal static void Utc(DateTimeOffset value, string fieldName, string code)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                code,
                $"{fieldName} must be expressed in UTC.",
                DomainErrorType.Validation);
        }
    }
}
