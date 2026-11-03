using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;

namespace HeroPath.Modules.Levels.Tests.Resources;

public partial class LevelsResourcesTests
{
    private static readonly ResourceManager _resources = new(typeof(LevelsResources).FullName!, typeof(LevelsResources).Assembly);

    public static TheoryData<string> EditorKeys
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var tool in Enum.GetValues<EditorTool>())
            {
                data.Add($"Editor_Tool_{tool}");
            }

            foreach (var hint in Enum.GetValues<EditHint>().Where(hint => hint != EditHint.None))
            {
                data.Add($"Editor_Hint_{hint}");
            }

            foreach (var kind in Enum.GetValues<ObstacleKind>())
            {
                data.Add($"Cell_{kind}");
            }

            foreach (var kind in Enum.GetValues<ItemKind>())
            {
                data.Add($"Cell_{kind}");
            }

            foreach (var key in new[]
            {
                "Editor_Title", "Editor_NameHint", "Editor_Status_Solvable_One", "Editor_Status_Solvable_Other", "Editor_Status_GoalUnreachable",
                "Editor_Status_StrawberriesUnreachable", "Editor_Status_TooManySteps", "Editor_Status_NameMissing", "Editor_Status_Invalid",
                "Editor_Save_Saved", "Editor_Save_LimitReached", "Editor_Save_StorageFailed", "Editor_Save_Invalid", "Editor_Removed_One",
                "Editor_Removed_Other", "Editor_DefaultName", "Cell_Label", "Cell_Empty", "Cell_Start", "Cell_Goal", "Cell_Goal_Strawberry", "Cell_Goal_Pizza",
            })
            {
                data.Add(key);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EditorKeys))]
    public void Key_ExistsInGermanAndEnglishResx(string key)
    {
        Assert.False(string.IsNullOrWhiteSpace(ValueOf(key, CultureInfo.InvariantCulture)), $"{key} missing in the German (neutral) resx");
        Assert.False(string.IsNullOrWhiteSpace(ValueOf(key, CultureInfo.GetCultureInfo("en"))), $"{key} missing in the English resx");
    }

    [Fact]
    public void GermanAndEnglish_HaveTheSameKeysAndPlaceholders()
    {
        var german = All(CultureInfo.InvariantCulture);
        var english = All(CultureInfo.GetCultureInfo("en"));

        Assert.Equal(german.Keys.Order(), english.Keys.Order());
        foreach (var (key, value) in german)
        {
            Assert.Equal(Placeholders(value), Placeholders(english[key]));
        }
    }

    [Fact]
    public void German_UsesSwissSpelling()
    {
        Assert.DoesNotContain(All(CultureInfo.InvariantCulture), pair => pair.Value.Contains('ß'));
    }

    private static Dictionary<string, string> All(CultureInfo culture)
    {
        var set = _resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!;
        return set.Cast<System.Collections.DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);
    }

    private static string[] Placeholders(string value) =>
        [.. PlaceholderPattern().Matches(value).Select(match => match.Value).Distinct().Order()];

    private static string? ValueOf(string key, CultureInfo culture) =>
        _resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)?.GetString(key);

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();
}
