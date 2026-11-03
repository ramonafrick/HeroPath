using System.Globalization;
using System.Resources;
using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Modules.Levels.Domain;

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

            foreach (var key in new[] { "Start_Title", "Start_Intro", "Start_HeroGroup", "Start_NameLabel", "Start_NameHint", "Start_LevelsTitle", "Start_Go", "Hero_Change", "Hero_BadgeLabel" })
            {
                data.Add(key);
            }

            foreach (var status in Enum.GetValues<RunStatus>())
            {
                data.Add($"Status_{status}");
            }

            foreach (var code in Enum.GetValues<GameMessageCode>())
            {
                switch (code)
                {
                    case GameMessageCode.Obstacle:
                        foreach (var kind in Enum.GetValues<ObstacleKind>())
                        {
                            data.Add($"Message_Obstacle_{kind}");
                        }

                        break;
                    case GameMessageCode.ItemsMissing:
                        data.Add("Message_ItemsMissing_One");
                        data.Add("Message_ItemsMissing_Other");
                        break;
                    default:
                        data.Add($"Message_{code}");
                        break;
                }
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
