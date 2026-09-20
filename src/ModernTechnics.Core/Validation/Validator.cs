using System.Text.RegularExpressions;
using ModernTechnics.Core.Common;

namespace ModernTechnics.Core.Validation;

/// <summary>Small fluent collector for validation errors.</summary>
public sealed partial class Validator
{
    public const int MinPasswordLength = 8;

    private readonly List<Error> _errors = [];

    public IReadOnlyList<Error> Errors => _errors;

    public Result ToResult() => _errors.Count == 0 ? Result.Success() : Result.Failure(_errors);

    public Validator Required(string? value, string field, int maxLength = 255)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _errors.Add(new Error(ErrorCodes.Required, field));
        }
        else if (value.Trim().Length > maxLength)
        {
            _errors.Add(new Error(ErrorCodes.TooLong, field, maxLength));
        }

        return this;
    }

    public Validator Optional(string? value, string field, int maxLength = 255)
    {
        if (value is not null && value.Trim().Length > maxLength)
        {
            _errors.Add(new Error(ErrorCodes.TooLong, field, maxLength));
        }

        return this;
    }

    public Validator PersonalId(string? value, string field)
    {
        if (value is null || !PersonalIdPattern().IsMatch(value))
        {
            _errors.Add(new Error(ErrorCodes.InvalidPersonalId, field));
        }

        return this;
    }

    public Validator Email(string? value, string field)
    {
        if (value is null || value.Length > 255 || !EmailPattern().IsMatch(value))
        {
            _errors.Add(new Error(ErrorCodes.InvalidEmail, field));
        }

        return this;
    }

    public Validator Phone(string? value, string field)
    {
        if (value is null || !PhonePattern().IsMatch(value))
        {
            _errors.Add(new Error(ErrorCodes.InvalidPhone, field));
        }

        return this;
    }

    /// <summary>Birth date of a person aged between <paramref name="minAge"/> and 100 today.</summary>
    public Validator BirthDate(DateOnly value, string field, DateOnly today, int minAge = 0)
    {
        if (value > today.AddYears(-minAge) || value < today.AddYears(-100))
        {
            _errors.Add(new Error(ErrorCodes.InvalidBirthDate, field, minAge));
        }

        return this;
    }

    public Validator Year(int value, string field, int currentYear)
    {
        if (value < 1970 || value > currentYear + 1)
        {
            _errors.Add(new Error(ErrorCodes.InvalidYear, field));
        }

        return this;
    }

    public Validator NotNegative(decimal value, string field)
    {
        if (value < 0)
        {
            _errors.Add(new Error(ErrorCodes.MustNotBeNegative, field));
        }

        return this;
    }

    public Validator Positive(decimal value, string field)
    {
        if (value <= 0)
        {
            _errors.Add(new Error(ErrorCodes.MustBePositive, field));
        }

        return this;
    }

    public Validator Password(string? value, string field)
    {
        if (value is null
            || value.Length < MinPasswordLength
            || !value.Any(char.IsLetter)
            || !value.Any(char.IsDigit))
        {
            _errors.Add(new Error(ErrorCodes.WeakPassword, field, MinPasswordLength));
        }

        return this;
    }

    [GeneratedRegex(@"^\d{11}$")]
    private static partial Regex PersonalIdPattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^\+?[\d\s\-()]{6,20}$")]
    private static partial Regex PhonePattern();
}
