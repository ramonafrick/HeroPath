using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Components;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Pages;

/// <summary>
/// The routable editor page (<c>/editor</c> and <c>/editor/{LevelId}</c>). It lives in Game.UI because «Try it» needs the hero choice:
/// with a hero the level opens on the play page, without one the start page pre-selects it. The editing itself is the <c>LevelEditor</c> component of Levels.UI. An unknown id opens a new level.
/// </summary>
public partial class EditorPage : LocalizedComponentBase
{
    [Parameter]
    public string? LevelId { get; set; }

    [Inject]
    private HeroChoice Choice { get; set; } = default!;

    [Inject]
    private LevelProgress Progress { get; set; } = default!;

    [Inject]
    private LevelRepository Repository { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IStringLocalizer<GameResources> GameLocalizer { get; set; } = default!;

    [Inject]
    private IStringLocalizer<LevelsResources> LevelsLocalizer { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResources> SharedLocalizer { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Repository.Changed += ReplaceDeletedChoice;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Repository.Changed -= ReplaceDeletedChoice;
        }

        base.Dispose(disposing);
    }

    /// <summary>A deleted level must not stay the chosen one: the selection falls back to the first standard level.</summary>
    private void ReplaceDeletedChoice()
    {
        if (Choice.LevelId is { } id && Progress.Find(id) is null && Progress.Levels.FirstOrDefault() is { } first)
        {
            Choice.Preselect(first.Id);
        }
    }

    /// <summary>«Try it»: with a hero the level is played right away, without one the start page pre-selects it.</summary>
    private void TryOut(string levelId)
    {
        if (Choice.IsChosen)
        {
            Choice.SelectLevel(levelId);
            Navigation.NavigateTo("play");
        }
        else
        {
            Choice.Preselect(levelId);
            Navigation.NavigateTo(string.Empty);
        }
    }
}
