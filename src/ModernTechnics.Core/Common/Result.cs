namespace ModernTechnics.Core.Common;

/// <summary>Outcome of an operation that can fail for business reasons.</summary>
public class Result
{
    protected Result(IReadOnlyList<Error> errors) => Errors = errors;

    public IReadOnlyList<Error> Errors { get; }

    public bool IsSuccess => Errors.Count == 0;

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new([]);

    public static Result Failure(params Error[] errors) => new(errors);

    public static Result Failure(IEnumerable<Error> errors) => new([.. errors]);

    public static Result<T> Success<T>(T value) => new(value, []);

    public static Result<T> Failure<T>(params Error[] errors) => new(default, errors);

    public static Result<T> Failure<T>(IEnumerable<Error> errors) => new(default, [.. errors]);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, IReadOnlyList<Error> errors) : base(errors) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");
}
