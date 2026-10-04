using System.Globalization;
using System.Resources;
using ModernTechnics.Core.Common;

namespace ModernTechnics.App.Localization;

/// <summary>Looks up user-facing text for the active language (English or Georgian).</summary>
internal static class L
{
    public const string English = "en";
    public const string Georgian = "ka";

    private const string BaseName = "ModernTechnics.App.Localization.Strings";

    private static readonly ResourceManager EnglishStrings = new(BaseName, typeof(L).Assembly);
    private static readonly ResourceManager GeorgianStrings = new(BaseName + "." + Georgian, typeof(L).Assembly);

    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo(English);

    public static string Language => Culture.TwoLetterISOLanguageName;

    public static void SetLanguage(string? code)
    {
        Culture = CultureInfo.GetCultureInfo(code == Georgian ? Georgian : English);
        CultureInfo.CurrentUICulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
    }

    /// <summary>Falls back to English, then to the key itself, so a missing string is visible but harmless.</summary>
    public static string T(string key) =>
        (Language == Georgian ? GeorgianStrings.GetString(key, CultureInfo.InvariantCulture) : null)
        ?? EnglishStrings.GetString(key, CultureInfo.InvariantCulture)
        ?? key;

    public static string T(string key, params object?[] args) => string.Format(Culture, T(key), args);

    public static string Field(string name) => T("Field." + name);

    public static string Enum<TEnum>(TEnum value) where TEnum : struct, Enum =>
        T($"{typeof(TEnum).Name}.{value}");

    public static string Money(decimal amount) => amount.ToString("N2", CultureInfo.CurrentCulture) + " ₾";

    /// <summary>Whole-number money for headline figures.</summary>
    public static string MoneyRounded(decimal amount) =>
        Math.Round(amount).ToString("N0", CultureInfo.CurrentCulture) + " ₾";

    public static string Date(DateOnly date) => date.ToString("d", CultureInfo.CurrentCulture);

    public static string DateTime(DateTime utc) =>
        utc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    /// <summary>Renders a business error; {0} is the field label and {1} the error argument.</summary>
    public static string Error(Error error) =>
        T(error.Code, error.Field is null ? string.Empty : Field(error.Field), error.Argument);

    public static string Errors(Result result) =>
        string.Join(Environment.NewLine, result.Errors.Select(Error));
}
