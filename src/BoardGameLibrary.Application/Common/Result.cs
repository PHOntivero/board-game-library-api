using System.Collections.ObjectModel;

namespace BoardGameLibrary.Application.Common;

public class Result
{
    private static readonly IReadOnlyList<Error> NoErrors = Array.Empty<Error>();

    protected Result(bool isSuccess, IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        Error[] materializedErrors = errors.ToArray();

        if (isSuccess && materializedErrors.Length > 0)
        {
            throw new ArgumentException("A successful result cannot contain errors.", nameof(errors));
        }

        if (!isSuccess && materializedErrors.Length == 0)
        {
            throw new ArgumentException("A failed result must contain at least one error.", nameof(errors));
        }

        if (materializedErrors.Any(error => error is null))
        {
            throw new ArgumentException("A result cannot contain a null error.", nameof(errors));
        }

        if (!isSuccess && materializedErrors.Select(error => error.Type).Distinct().Count() > 1)
        {
            throw new ArgumentException("A failed result cannot contain errors of different types.", nameof(errors));
        }

        IsSuccess = isSuccess;
        Errors = materializedErrors.Length == 0
            ? NoErrors
            : new ReadOnlyCollection<Error>(materializedErrors);
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Error> Errors { get; }

    public static Result Success() => new(true, NoErrors);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(false, [error]);
    }

    public static Result Failure(IEnumerable<Error> errors) => new(false, errors);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, [])
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    private Result(IEnumerable<Error> errors)
        : base(false, errors)
    {
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result does not have a value.");

    public static Result<T> Success(T value) => new(value);

    public new static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>([error]);
    }

    public new static Result<T> Failure(IEnumerable<Error> errors) => new(errors);
}
