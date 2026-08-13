using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Api.Contracts;

public static class QueryValueParser
{
    public static Result<OptionalEnumValue<TEnum>> ParseOptionalEnum<TEnum>(
        string? value,
        string parameterName)
        where TEnum : struct, Enum
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<OptionalEnumValue<TEnum>>.Success(new(null));
        }

        string candidate = value.Trim();

        if (int.TryParse(candidate, out _) ||
            !Enum.TryParse(candidate, ignoreCase: true, out TEnum parsed) ||
            !Enum.IsDefined(parsed))
        {
            string allowedValues = string.Join(
                ", ",
                Enum.GetNames<TEnum>().Select(name => char.ToLowerInvariant(name[0]) + name[1..]));

            return Result<OptionalEnumValue<TEnum>>.Failure(Error.Validation(
                ErrorCodes.Common.ValidationFailed,
                $"{parameterName} must be one of: {allowedValues}."));
        }

        return Result<OptionalEnumValue<TEnum>>.Success(new(parsed));
    }
}

public sealed record OptionalEnumValue<TEnum>(TEnum? Value)
    where TEnum : struct, Enum;
