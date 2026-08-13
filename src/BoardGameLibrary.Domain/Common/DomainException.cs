namespace BoardGameLibrary.Domain.Common;

public sealed class DomainException : Exception
{
    public DomainException(string code, string message, DomainErrorType type)
        : base(ValidateMessage(message))
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Domain error type is invalid.");
        }

        Code = code;
        Type = type;
    }

    public string Code { get; }

    public DomainErrorType Type { get; }

    private static string ValidateMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return message;
    }
}
