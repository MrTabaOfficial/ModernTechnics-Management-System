using System.Text.Json;
using ModernTechnics.App.Localization;

namespace ModernTechnics.App;

/// <summary>Where the application keeps its per-user files.</summary>
internal static class AppPaths
{
    public static string DataDirectory { get; private set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModernTechnics");

    public static string Database => Path.Combine(DataDirectory, "moderntechnics.db");

    public static string Settings => Path.Combine(DataDirectory, "settings.json");

    public static void UseDirectory(string directory) => DataDirectory = directory;
}

/// <summary>Preferences remembered between launches.</summary>
internal sealed class AppSettings
{
    public string Language { get; set; } = L.English;

    public string LastEmail { get; set; } = string.Empty;

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(AppPaths.Settings)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(AppPaths.Settings)) ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            // Preferences are a convenience; a damaged file must never block startup.
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            File.WriteAllText(AppPaths.Settings, JsonSerializer.Serialize(this));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Same reasoning as Load: losing a preference is not worth an error dialog.
        }
    }
}
