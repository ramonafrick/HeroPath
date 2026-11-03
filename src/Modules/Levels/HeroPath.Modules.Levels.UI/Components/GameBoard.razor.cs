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
    private string?[] _items = [];
    private string _boardStyle = string.Empty;
    private string _heroStyle = string.Empty;

    [Parameter, EditorRequired]
    public required Grid Grid { get; set; }

    [Parameter]
    public Position HeroPosition { get; set; }

    [Parameter]
    public Position? MarkedCell { get; set; }

    /// <summary>Cells whose item was collected in the current run. Their items stay in the DOM with <c>data-collected="true"</c> and fade out.</summary>
    [Parameter]
    public IReadOnlySet<Position>? CollectedItems { get; set; }

    [Parameter]
    public TimeSpan StepDuration { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>A small, purely decorative picture of the level: no hero, hidden from assistive technology, no per-cell test ids, sized by its container.</summary>
    [Parameter]
    public bool Preview { get; set; }

    /// <summary>
    /// Edit mode: with a handler every cell is a <c>&lt;button&gt;</c> (<c>edit-cell-x-y</c>) that reports its position, and the board has no hero.
    /// Without it the board is a plain, non-interactive view.
    /// </summary>
    [Parameter]
    public EventCallback<Position> CellClicked { get; set; }

    /// <summary>The accessible name of a cell in edit mode.</summary>
    [Parameter]
    public Func<Position, string>? CellLabel { get; set; }

    [Parameter]
    public RenderFragment? HeroContent { get; set; }

    [Inject]
    private IStringLocalizer<LevelsResources> Localizer { get; set; } = default!;

    private bool IsEditable => !Preview && CellClicked.HasDelegate;

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(_kindsGrid, Grid))
        {
            _kindsGrid = Grid;
            var itemKinds = Enumerable.Range(0, Grid.Width * Grid.Height).Select(index => Grid.GetItem(new Position(index % Grid.Width, index / Grid.Width))).ToArray();
            _items = [.. itemKinds.Select(kind => kind?.ToString().ToLowerInvariant())];
            _kinds = [.. Enumerable.Range(0, Grid.Width * Grid.Height).Select(index => KindOf(new Position(index % Grid.Width, index / Grid.Width), _items[index]))];
            _boardStyle = string.Create(CultureInfo.InvariantCulture, $"--cols: {Grid.Width}; --rows: {Grid.Height};");
        }

        _heroStyle = string.Create(
            CultureInfo.InvariantCulture,
            $"--x: {HeroPosition.X}; --y: {HeroPosition.Y}; --hp-step-duration: {(int)StepDuration.TotalMilliseconds}ms;");
    }

    private string KindAt(Position position) => _kinds[position.Y * Grid.Width + position.X];

    private string? ItemAt(Position position) => _items[position.Y * Grid.Width + position.X];

    private bool IsMarked(Position position) => MarkedCell == position;

    private string KindOf(Position position, string? item)
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
            _ => item ?? "empty",
        };
    }
}
