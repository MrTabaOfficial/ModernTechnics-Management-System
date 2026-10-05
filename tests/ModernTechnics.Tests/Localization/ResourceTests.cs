using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Security;
using Module = ModernTechnics.Core.Security.Module;

namespace ModernTechnics.Tests.Localization;

/// <summary>
/// Guards the two string tables: the UI looks keys up at run time, so a missing or
/// mistyped key would otherwise only show up as raw text on some screen.
/// </summary>
public partial class ResourceTests
{
    private static readonly string AppDirectory = Path.Combine(FindRepositoryRoot(), "src", "ModernTechnics.App");
    private static readonly Dictionary<string, string> English = Load("Strings.resx");
    private static readonly Dictionary<string, string> Georgian = Load("Strings.ka.resx");

    [Fact]
    public void Both_languages_define_exactly_the_same_keys()
    {
        Assert.Empty(English.Keys.Except(Georgian.Keys));
        Assert.Empty(Georgian.Keys.Except(English.Keys));
    }

    [Fact]
    public void Translations_use_the_same_placeholders()
    {
        foreach (var (key, english) in English)
        {
            Assert.True(
                Placeholders(english).SetEquals(Placeholders(Georgian[key])),
                $"Placeholders differ for '{key}'.");
        }
    }

    [Fact]
    public void No_translation_is_left_empty()
    {
        Assert.DoesNotContain(English, pair => string.IsNullOrWhiteSpace(pair.Value));
        Assert.DoesNotContain(Georgian, pair => string.IsNullOrWhiteSpace(pair.Value));
    }

    [Fact]
    public void Every_key_referenced_in_the_source_exists()
    {
        var referenced = Directory
            .EnumerateFiles(AppDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(file => LiteralKey().Matches(File.ReadAllText(file)))
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(referenced);
        Assert.Empty(referenced.Except(English.Keys));
    }

    [Fact]
    public void Every_error_code_has_a_message()
    {
        var codes = typeof(ErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (string)field.GetRawConstantValue()!);

        Assert.Empty(codes.Except(English.Keys));
    }

    [Fact]
    public void Every_enum_value_shown_in_the_ui_has_a_label()
    {
        var expected = Labels<Role>()
            .Concat(Labels<MaritalStatus>())
            .Concat(Labels<ApplicationStatus>())
            .Concat(Enum.GetNames<Module>().SelectMany(name => new[] { $"Module.{name}", $"Module.{name}.Hint" }));

        Assert.Empty(expected.Except(English.Keys));
    }

    [Fact]
    public void Every_validated_property_has_a_field_label()
    {
        Type[] entities = [typeof(Employee), typeof(Customer), typeof(Product), typeof(JobApplication), typeof(UserAccount)];
        var properties = entities
            .SelectMany(type => type.GetProperties())
            .Where(property => property.PropertyType.IsValueType || property.PropertyType == typeof(string))
            .Select(property => "Field." + property.Name)
            .Except(["Field.Id", "Field.TotalStock", "Field.SubmittedAtUtc"])
            .Distinct();

        Assert.Empty(properties.Except(English.Keys));
    }

    private static IEnumerable<string> Labels<TEnum>() where TEnum : struct, Enum =>
        Enum.GetNames<TEnum>().Select(name => $"{typeof(TEnum).Name}.{name}");

    private static HashSet<string> Placeholders(string text) =>
        [.. Placeholder().Matches(text).Select(match => match.Value)];

    private static Dictionary<string, string> Load(string fileName) =>
        XDocument.Load(Path.Combine(AppDirectory, "Localization", fileName))
            .Root!
            .Elements("data")
            .ToDictionary(data => data.Attribute("name")!.Value, data => data.Element("value")!.Value);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ModernTechnics.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

    [GeneratedRegex(@"L\.T\(\s*""([A-Za-z0-9.]+)""")]
    private static partial Regex LiteralKey();
}
