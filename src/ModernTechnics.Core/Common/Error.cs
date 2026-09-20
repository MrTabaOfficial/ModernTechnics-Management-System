namespace ModernTechnics.Core.Common;

/// <summary>
/// A business error. <see cref="Code"/> is a stable key the UI localises;
/// <see cref="Field"/> names the offending property when the error is a validation one.
/// </summary>
public sealed record Error(string Code, string? Field = null, object? Argument = null);

public static class ErrorCodes
{
    public const string Required = "Error.Required";
    public const string TooLong = "Error.TooLong";
    public const string InvalidEmail = "Error.InvalidEmail";
    public const string InvalidPersonalId = "Error.InvalidPersonalId";
    public const string InvalidPhone = "Error.InvalidPhone";
    public const string InvalidBirthDate = "Error.InvalidBirthDate";
    public const string InvalidYear = "Error.InvalidYear";
    public const string MustNotBeNegative = "Error.MustNotBeNegative";
    public const string MustBePositive = "Error.MustBePositive";
    public const string WeakPassword = "Error.WeakPassword";

    public const string InvalidCredentials = "Error.InvalidCredentials";
    public const string NotFound = "Error.NotFound";
    public const string Duplicate = "Error.Duplicate";
    public const string InUse = "Error.InUse";
    public const string LastAdministrator = "Error.LastAdministrator";
    public const string CannotDeleteSelf = "Error.CannotDeleteSelf";
    public const string InsufficientStock = "Error.InsufficientStock";
}
