using System.Globalization;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Levels.UI.Components;

public partial class GameBoard : IDisposable
{
    [Parameter, EditorRequired]
    public required Grid Grid { get; set; }

    [Parameter]
    public Position HeroPosition { get; set; }

    [Parameter]
    public Position? MarkedCell { get; set; }

    [Parameter]
    public TimeSpan StepDuration { get; set; } = TimeSpan.FromMilliseconds(500);

    [Parameter]
    public RenderFragment? HeroContent { get; set; }

    [Inject]
    private LanguageService Language { get; set; } = default!;

    [Inject]
    private IStringLocalizer<LevelsResources> Localizer { get; set; } = default!;

    private string BoardStyle => string.Create(CultureInfo.InvariantCulture, $"--cols: {Grid.Width}; --rows: {Grid.Height};");

    private string HeroStyle => string.Create(
        CultureInfo.InvariantCulture,
        $"--x: {HeroPosition.X}; --y: {HeroPosition.Y}; --hp-step-duration: {(int)StepDuration.TotalMilliseconds}ms;");

    protected override void OnInitialized() => Language.Changed += HandleChanged;

    public void Dispose() => Language.Changed -= HandleChanged;

    private bool IsMarked(Position position) => MarkedCell == position;

    private string KindOf(Position position)
    {
        if (position == Grid.Start)
        {
            return "start";
        }

        if (position == Grid.Goal)
        {
            return "goal";
        }

        return Grid.GetObstacle(position) switch
        {
            ObstacleKind.Tree => "tree",
            ObstacleKind.Rock => "rock",
            ObstacleKind.Pond => "pond",
            _ => "empty",
        };
    }

    private void HandleChanged() => _ = InvokeAsync(StateHasChanged);
}
