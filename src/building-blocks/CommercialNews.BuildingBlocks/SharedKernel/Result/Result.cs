namespace CommercialNews.BuildingBlocks.SharedKernel.Results;

using System.Diagnostics.CodeAnalysis;

public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException(
                "A successful result cannot contain an error.",
                nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentNullException(
                nameof(error),
                "A failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(false, error);
    }
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        if (isSuccess && value is null)
        {
            throw new ArgumentNullException(
                nameof(value),
                "A successful result must contain a value.");
        }

        _value = value;
    }

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                "A failed result does not contain a value.");

    private T? ValueOrDefault => _value;

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value, true, null);
    }

    public static new Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(default, false, error);
    }
}