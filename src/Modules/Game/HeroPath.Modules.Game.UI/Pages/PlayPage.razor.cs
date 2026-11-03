using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Pages;

/// <summary>
/// The play page. Opening it loads the level into the <see cref="GameSession"/> (an equal grid keeps program and run);
/// leaving it stops a running Play, so no loop keeps running and the run resumes consistently (Paused) on return.
/// </summary>
public partial class PlayPage : SessionComponentBase
{
    private const string DemoLevelId = "demo";

    private LevelDefinition _level = default!;

    [Inject]
    private ExecutionRunner Runner { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResources> Localizer { get; set; } = default!;

    [Inject]
    private IStringLocalizer<LevelsResources> LevelsLocalizer { get; set; } = default!;

    protected override void OnInitialized()
    {
        _level = StandardLevels.Get(DemoLevelId);
        Session.Load(_level.ToGrid());
        base.OnInitialized();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Runner.Stop();
        }
    }
}
