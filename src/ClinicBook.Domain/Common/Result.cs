namespace ClinicBook.Domain.Common;

/// <summary>
/// Represents the outcome of an operation.
/// </summary>
public class Result
{
    /// <summary>Initializes a result.</summary>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Successful result cannot have an error.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Failure result must have an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Indicates whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Indicates whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The error associated with a failed operation.</summary>
    public Error Error { get; }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Creates a failed result.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Creates a successful result with a value.</summary>
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    /// <summary>Creates a failed result with a value type.</summary>
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
}

/// <summary>
/// Represents the outcome of an operation that returns a value.
/// </summary>
/// <typeparam name="TValue">The value type.</typeparam>
#pragma warning disable CA1000 // Static factory members on generic result type are intentional for ergonomics/spec parity.
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    /// <summary>Initializes a result with a value.</summary>
    protected Result(bool isSuccess, TValue? value, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>The value produced by a successful operation.</summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing the value of a failed result.</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    /// <summary>Creates a successful result.</summary>
    public static Result<TValue> Success(TValue value) => new(true, value, Error.None);

    /// <summary>Creates a failed result.</summary>
    public static new Result<TValue> Failure(Error error) => new(false, default, error);

    /// <summary>Implicitly converts a value to a successful result.</summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
#pragma warning restore CA1000
