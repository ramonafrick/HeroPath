using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Components;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Pages;

/// <summary>
/// The start page: the child picks a hero, a name and a level and starts the game. The form state lives in the component, so a
/// language switch keeps it; <see cref="HeroChoice"/> pre-fills it when the child returns from the play page.
/// </summary>
public partial class StartPage : LocalizedComponentBase
{
    private readonly Dictionary<string, Grid> _grids = [];
    private HeroAvatar _avatar;
    private string _name = string.Empty;
    private string _levelId = string.Empty;

    private static IReadOnlyList<HeroAvatar> Avatars { get; } = Enum.GetValues<HeroAvatar>();

    private static IReadOnlyList<LevelDefinition> Levels => StandardLevels.All;

    [Inject]
    private HeroChoice Choice { get; set; } = default!;

    [Inject]
    private GameSession Session { get; set; } = default!;

    [Inject]
    private LevelProgress Progress { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    [Inject]
    private IStringLocalizer<LevelsResources> LevelsLocalizer { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResources> SharedLocalizer { get; set; } = default!;

    protected override void OnInitialized()
    {
        _avatar = Choice.Avatar;
        _name = Choice.Name;
        _levelId = Choice.LevelId;
        base.OnInitialized();
    }

    private Grid GridOf(LevelDefinition level)
    {
        if (!_grids.TryGetValue(level.Id, out var grid))
        {
            grid = level.ToGrid();
            _grids[level.Id] = grid;
        }

        return grid;
    }

    private void SelectAvatar(HeroAvatar avatar)
    {
        _name = HeroChoice.NameAfterSwitch(_name, _avatar, avatar);
        _avatar = avatar;
    }

    private void SelectLevel(string id) => _levelId = id;

    private void Go()
    {
        Choice.Choose(_avatar, _name, _levelId);
        Session.Load(StandardLevels.Get(_levelId));
        Navigation.NavigateTo("play");
    }
}
