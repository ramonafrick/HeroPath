using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Localization;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Pages;

public partial class PlayPage : IDisposable
{
    private const string DemoLevelId = "demo";

    private LevelDefinition _level = default!;
    private Grid _grid = default!;

    [Inject]
    private GameSession Session { get; set; } = default!;

    [Inject]
    private LanguageService Language { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResources> Localizer { get; set; } = default!;

    [Inject]
    private IStringLocalizer<LevelsResources> LevelsLocalizer { get; set; } = default!;

    protected override void OnInitialized()
    {
        _level = StandardLevels.Get(DemoLevelId);
        _grid = _level.ToGrid();
        Session.Load(_grid);
        Session.Changed += HandleChanged;
        Language.Changed += HandleChanged;
    }

    public void Dispose()
    {
        Language.Changed -= HandleChanged;
        Session.Changed -= HandleChanged;
    }

    private void HandleChanged() => _ = InvokeAsync(StateHasChanged);
}
