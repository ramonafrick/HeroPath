using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Modules.Game.UI.Services;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Pages;

/// <summary>
/// The play page. Opening it loads the chosen level into the <see cref="GameSession"/> (the same level keeps program and run) or
/// redirects to the start page when nothing was chosen;
/// leaving it stops a running Play and ends a drag in progress, so no loop keeps running and the run resumes consistently (Paused) on return.
/// </summary>
public partial class PlayPage : SessionComponentBase
{
    [Inject]
    private HeroChoice Choice { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ExecutionRunner Runner { get; set; } = default!;

    [Inject]
    private ProgramDragState Drag { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResources> SharedLocalizer { get; set; } = default!;

    [Inject]
    private IStringLocalizer<LevelsResources> LevelsLocalizer { get; set; } = default!;

    [Inject]
    private LevelProgress Progress { get; set; } = default!;

    [Inject]
    private LevelRepository Repository { get; set; } = default!;

    [Inject]
    private IStringLocalizer<GameResources> GameLocalizer { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (Choice.IsChosen && Choice.LevelId is { } id && Progress.Find(id) is { } chosen)
        {
            Session.Load(chosen);
        }
        else
        {
            Navigation.NavigateTo(string.Empty, replace: true);
        }

        base.OnInitialized();
        Progress.Changed += HandleChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Progress.Changed -= HandleChanged;
        }

        base.Dispose(disposing);
        if (disposing)
        {
            Runner.Stop();
            Drag.End();
        }
    }

    private void Edit(string id) => Navigation.NavigateTo($"editor/{id}");

    private void SwitchLevel(string id)
    {
        if (Progress.Find(id) is { } level)
        {
            LoadLevel(level);
        }
    }

    private void LoadLevel(LevelDefinition level)
    {
        Choice.SelectLevel(level.Id);
        Session.Load(level);
    }
}
