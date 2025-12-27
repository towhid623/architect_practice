namespace SharedKernel.Common;

/// <summary>
/// Represents the result of an operation
/// </summary>
public class Result
{
    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; protected set; } = string.Empty;

    protected Result(bool isSuccess, string error)
    {
  IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
    
    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    public static Result<T> Failure<T>(string error) => new(default!, false, error);
}

/// <summary>
/// Represents the result of an operation with a value
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; private set; }

    protected internal Result(T? value, bool isSuccess, string error) : base(isSuccess, error)
    {
  Value = value;
    }
}
