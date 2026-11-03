using System.Globalization;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Levels.UI.Components;

public partial class GameBoard : LocalizedComponentBase
{
    private Grid? _kindsGrid;
    private string[] _kinds = [];
    private string _boardStyle = string.Empty;
    private string _heroStyle = string.Empty;

    [Parameter, EditorRequired]
    public required Grid Grid { get; set; }

    [Parameter]
    public Position HeroPosition { get; set; }

    [Parameter]
    public Position? MarkedCell { get; set; }

    [Parameter]
    public TimeSpan StepDuration { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>A small, purely decorative picture of the level: no hero, hidden from assistive technology, no per-cell test ids, sized by its container.</summary>
    [Parameter]
    public bool Preview { get; set; }

    [Parameter]
    public RenderFragment? HeroContent { get; set; }

    [Inject]
    private IStringLocalizer<LevelsResources> Localizer { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(_kindsGrid, Grid))
        {
            _kindsGrid = Grid;
            _kinds = [.. Enumerable.Range(0, Grid.Width * Grid.Height).Select(index => KindOf(new Position(index % Grid.Width, index / Grid.Width)))];
            _boardStyle = string.Create(CultureInfo.InvariantCulture, $"--cols: {Grid.Width}; --rows: {Grid.Height};");
        }

        _heroStyle = string.Create(
            CultureInfo.InvariantCulture,
            $"--x: {HeroPosition.X}; --y: {HeroPosition.Y}; --hp-step-duration: {(int)StepDuration.TotalMilliseconds}ms;");
    }

    private string KindAt(Position position) => _kinds[position.Y * Grid.Width + position.X];

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
}
