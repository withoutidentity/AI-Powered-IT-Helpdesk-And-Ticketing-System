namespace Application.Common.Models;

public sealed class Result<T>
{
    private Result(T? value, string? errorCode, string? errorMessage)
    {
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess => ErrorCode is null;
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static Result<T> Success(T value)
    {
        return new Result<T>(value, null, null);
    }

    public static Result<T> Failure(string errorCode, string errorMessage)
    {
        return new Result<T>(default, errorCode, errorMessage);
    }
}