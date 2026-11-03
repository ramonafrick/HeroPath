using System.Globalization;
using System.Resources;
using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI.Resources;

namespace HeroPath.Modules.Game.Tests.Resources;

public class GameResourcesTests
{
    private static readonly ResourceManager _resources = new(typeof(GameResources).FullName!, typeof(GameResources).Assembly);

    public static TheoryData<string> EnumKeys
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var tempo in Enum.GetValues<Tempo>())
            {
                data.Add($"Tempo_{tempo}");
            }

            foreach (var status in Enum.GetValues<RunStatus>())
            {
                data.Add($"Status_{status}");
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EnumKeys))]
    public void EnumKey_ExistsInGermanAndEnglishResx(string key)
    {
        Assert.False(string.IsNullOrWhiteSpace(ValueOf(key, CultureInfo.InvariantCulture)), $"{key} missing in the German (neutral) resx");
        Assert.False(string.IsNullOrWhiteSpace(ValueOf(key, CultureInfo.GetCultureInfo("en"))), $"{key} missing in the English resx");
    }

    private static string? ValueOf(string key, CultureInfo culture) =>
        _resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)?.GetString(key);
}
