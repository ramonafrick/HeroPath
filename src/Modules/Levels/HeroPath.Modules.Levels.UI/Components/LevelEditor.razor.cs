using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Levels.UI.Components;

/// <summary>
/// The level editor: board, tools, size, name, check and save, plus the list of the child's own levels. All rules and the draft live in
/// <see cref="LevelEditorState"/> (a service), so a language switch only re-renders. Opens the level named by <see cref="LevelId"/>
/// (a new level when there is none or the id is not a custom level) whenever that parameter changes.
/// </summary>
public partial class LevelEditor : LocalizedComponentBase
{
    private readonly CancellationTokenSource _disposed = new();
    private bool _opened;
    private string? _openedFor;
    private string _nameText = string.Empty;
    private Grid? _grid;
    private LevelDefinition? _gridFor;

    /// <summary>The id of the custom level to open. <c>null</c> or an unknown id starts a new level.</summary>
    [Parameter]
    public string? LevelId { get; set; }

    [Inject]
    private LevelEditorState State { get; set; } = default!;

    [Inject]
    private LevelRepository Repository { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IStringLocalizer<LevelsResources> Localizer { get; set; } = default!;

    private Grid BoardGrid
    {
        get
        {
            if (!ReferenceEquals(_gridFor, State.Draft))
            {
                _gridFor = State.Draft;
                _grid = State.Draft.ToGrid();
            }

            return _grid!;
        }
    }

    private bool HasStrawberry => State.Draft.Items.Any(item => item.Kind == ItemKind.Strawberry);

    private string? HintText
    {
        get
        {
            if (State.Hint != EditHint.None)
            {
                return State.Hint switch
                {
                    EditHint.TooManyStrawberries => Localizer["Editor_Hint_TooManyStrawberries", LevelLimits.MaxStrawberries].Value,
                    EditHint.TooManyPizzas => Localizer["Editor_Hint_TooManyPizzas", LevelLimits.MaxPizzas].Value,
                    var hint => Localizer[$"Editor_Hint_{hint}"].Value,
                };
            }

            return State.RemovedCount switch
            {
                0 => null,
                1 => Localizer["Editor_Removed_One", 1].Value,
                var count => Localizer["Editor_Removed_Other", count].Value,
            };
        }
    }

    private string? SavedText => State.LastSave switch
    {
        SaveOutcome.Saved => Localizer["Editor_Save_Saved"].Value,
        SaveOutcome.LimitReached => Localizer["Editor_Save_LimitReached", LevelLimits.MaxCustomLevels].Value,
        SaveOutcome.StorageFailed => Localizer["Editor_Save_StorageFailed"].Value,
        SaveOutcome.Invalid => Localizer["Editor_Save_Invalid"].Value,
        _ => null,
    };

    protected override void OnInitialized()
    {
        base.OnInitialized();
        State.Changed += HandleChanged;
        Repository.Changed += HandleChanged;
    }

    protected override void OnParametersSet()
    {
        if (_opened && _openedFor == LevelId)
        {
            return;
        }

        _opened = true;
        _openedFor = LevelId;
        if (LevelId is null || !State.Open(LevelId))
        {
            State.OpenNew(DefaultName);
        }

        _nameText = State.Draft.Name ?? string.Empty;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            State.Changed -= HandleChanged;
            Repository.Changed -= HandleChanged;
            _disposed.Cancel();
            _disposed.Dispose();
        }

        base.Dispose(disposing);
    }

    private string DefaultName(int number) => Localizer["Editor_DefaultName", number].Value;

    private void CellClicked(Position position) => State.Apply(position);

    private void NameInput(ChangeEventArgs args)
    {
        _nameText = args.Value as string ?? string.Empty;
        State.SetName(_nameText);
    }

    private void RequireAllChanged(ChangeEventArgs args) => State.SetRequireAllItems(args.Value is true);

    private void OpenLevel(string id)
    {
        if (!State.Open(id))
        {
            return;
        }

        _nameText = State.Draft.Name ?? string.Empty;
        GoTo(id);
    }

    private void NewLevel()
    {
        State.OpenNew(DefaultName);
        _nameText = State.Draft.Name ?? string.Empty;
        GoTo(null);
    }

    private async Task SaveAsync()
    {
        var outcome = await State.SaveAsync(_disposed.Token);
        if (outcome == SaveOutcome.Saved && LevelId is null && State.EditingId is { } id)
        {
            GoTo(id, replace: true);
        }
    }

    private void GoTo(string? id, bool replace = false)
    {
        _openedFor = id;
        Navigation.NavigateTo(id is null ? "editor" : $"editor/{id}", replace: replace);
    }

    private string CellLabel(Position position)
    {
        var grid = BoardGrid;
        var item = grid.GetItem(position);
        var content = position == grid.Start ? "Cell_Start"
            : position == grid.Goal ? (item switch { ItemKind.Strawberry => "Cell_Goal_Strawberry", ItemKind.Pizza => "Cell_Goal_Pizza", _ => "Cell_Goal" })
            : grid.GetObstacle(position) is { } obstacle ? $"Cell_{obstacle}"
            : item is { } itemKind ? $"Cell_{itemKind}"
            : "Cell_Empty";
        return Localizer["Cell_Label", position.X + 1, position.Y + 1, Localizer[content].Value].Value;
    }
}
