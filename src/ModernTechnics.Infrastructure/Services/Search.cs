namespace ModernTechnics.Infrastructure.Services;

/// <summary>Turns free-text search input into a safe SQL LIKE pattern.</summary>
internal static class Search
{
    public const string Escape = "\\";

    public static string? ToPattern(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return null;
        }

        var escaped = term.Trim()
            .Replace(Escape, Escape + Escape, StringComparison.Ordinal)
            .Replace("%", Escape + "%", StringComparison.Ordinal)
            .Replace("_", Escape + "_", StringComparison.Ordinal);

        return $"%{escaped}%";
    }
}
