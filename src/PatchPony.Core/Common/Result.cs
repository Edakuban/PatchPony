namespace PatchPony.Core.Common;

public sealed class Result
{
    private Result(bool isSuccess, DomainError error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public DomainError Error { get; }

    public static Result Success() => new(true, DomainError.None);

    public static Result Failure(DomainError error) => new(false, error);
}

public sealed class Result<T>
{
    private Result(T? value, bool isSuccess, DomainError error)
    {
        Value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    public T? Value { get; }

    public bool IsSuccess { get; }

    public DomainError Error { get; }

    public static Result<T> Success(T value) => new(value, true, DomainError.None);

    public static Result<T> Failure(DomainError error) => new(default, false, error);
}
