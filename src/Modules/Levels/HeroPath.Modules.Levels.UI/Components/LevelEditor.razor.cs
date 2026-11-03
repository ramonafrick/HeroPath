using System.Text;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
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
    private Pending _pending;
    private string? _pendingId;
    private string? _pendingName;
    private string? _leaveTarget;
    private string? _allowedTarget;
    private bool _trying;
    private bool _deleteFailed;
    private int _templateKey;
    private const long MaxImportBytes = 256 * 1024;

    private ImportMessage _import;
    private string? _importName;
    private int _importKey;
    private bool _exportFailed;

    /// <summary>The id of the custom level to open. <c>null</c> or an unknown id starts a new level.</summary>
    [Parameter]
    public string? LevelId { get; set; }

    /// <summary>Raised with the id of the level after «Try it» saved it. Without a handler the button is not shown.</summary>
    [Parameter]
    public EventCallback<string> OnTryOut { get; set; }

    [Inject]
    private LevelEditorState State { get; set; } = default!;

    [Inject]
    private LevelRepository Repository { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IFileDownload FileDownload { get; set; } = default!;

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

    private enum Pending
    {
        None,
        New,
        Template,
        Open,
        Delete,
        Clear,
        Leave,
    }

    private enum ImportMessage
    {
        None,
        Ok,
        Invalid,
        TooLarge,
        NotSavable,
        LimitReached,
        StorageFailed,
    }

    private string? ImportText => _import switch
    {
        ImportMessage.Ok => Localizer["Editor_Import_Ok", _importName ?? string.Empty].Value,
        ImportMessage.Invalid => Localizer["Editor_Import_Invalid"].Value,
        ImportMessage.TooLarge => Localizer["Editor_Import_TooLarge"].Value,
        ImportMessage.NotSavable => Localizer["Editor_Import_NotSavable"].Value,
        ImportMessage.LimitReached => Localizer["Editor_Import_LimitReached", LevelLimits.MaxCustomLevels].Value,
        ImportMessage.StorageFailed => Localizer["Editor_Import_StorageFailed"].Value,
        _ => null,
    };

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

    private string DialogTitle => _pending switch
    {
        Pending.Delete => Localizer["Confirm_Delete_Title"].Value,
        Pending.Clear => Localizer["Confirm_Clear_Title"].Value,
        Pending.Leave => Localizer["Confirm_Leave_Title"].Value,
        Pending.None => string.Empty,
        _ => Localizer["Confirm_Discard_Title"].Value,
    };

    private string DialogMessage => _pending switch
    {
        Pending.Delete => Localizer["Confirm_Delete_Message", _pendingName ?? string.Empty].Value,
        Pending.Clear => Localizer["Confirm_Clear_Message"].Value,
        Pending.Leave => Localizer["Confirm_Leave_Message"].Value,
        Pending.None => string.Empty,
        _ => Localizer["Confirm_Discard_Message"].Value,
    };

    private string? DialogConfirmLabel => _pending switch
    {
        Pending.Delete => Localizer["Confirm_Delete_Yes"].Value,
        Pending.Clear => Localizer["Confirm_Clear_Yes"].Value,
        Pending.Leave => Localizer["Confirm_Leave_Yes"].Value,
        Pending.None => null,
        _ => Localizer["Confirm_Discard_Yes"].Value,
    };

    private string? DialogCancelLabel => _pending switch
    {
        Pending.Leave or Pending.Open or Pending.New or Pending.Template => Localizer["Confirm_Stay"].Value,
        _ => null,
    };

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

    private void AskNew() => AskDiscard(Pending.New, null);

    private void AskOpen(string id) => AskDiscard(Pending.Open, id);

    private void TemplateChosen(ChangeEventArgs args)
    {
        _templateKey++;
        if (args.Value is string { Length: > 0 } id)
        {
            AskDiscard(Pending.Template, id);
        }
    }

    private void AskDiscard(Pending pending, string? id)
    {
        _deleteFailed = false;
        _pendingId = id;
        if (State.IsDirty)
        {
            _pending = pending;
            return;
        }

        Perform(pending, id);
    }

    private void AskClear() => _pending = Pending.Clear;

    private void AskDelete(string id)
    {
        _deleteFailed = false;
        _pendingId = id;
        _pendingName = Repository.Find(id)?.Name;
        _pending = Pending.Delete;
    }

    private Task BeforeNavigation(LocationChangingContext context)
    {
        if (_trying || !State.IsDirty || IsAllowed(context.TargetLocation))
        {
            return Task.CompletedTask;
        }

        context.PreventNavigation();
        _leaveTarget = context.TargetLocation;
        _pending = Pending.Leave;
        return Task.CompletedTask;
    }

    private bool IsAllowed(string target) =>
        _allowedTarget is { } allowed && Navigation.ToAbsoluteUri(allowed) == Navigation.ToAbsoluteUri(target);

    private void CancelPending()
    {
        _pending = Pending.None;
        _pendingId = null;
        _pendingName = null;
        _leaveTarget = null;
    }

    private async Task ConfirmPendingAsync()
    {
        var pending = _pending;
        var id = _pendingId;
        var target = _leaveTarget;
        CancelPending();
        switch (pending)
        {
            case Pending.Delete when id is not null:
                await DeleteAsync(id);
                break;
            case Pending.Clear:
                State.ClearObjects();
                break;
            case Pending.Leave when target is not null:
                _allowedTarget = target;
                Navigation.NavigateTo(target);
                break;
            default:
                Perform(pending, id);
                break;
        }
    }

    private void Perform(Pending pending, string? id)
    {
        switch (pending)
        {
            case Pending.New:
                State.OpenNew(DefaultName);
                break;
            case Pending.Template when id is not null:
                if (!State.OpenTemplate(id, DefaultName))
                {
                    return;
                }

                break;
            case Pending.Open when id is not null:
                if (!State.Open(id))
                {
                    return;
                }

                break;
            default:
                return;
        }

        _nameText = State.Draft.Name ?? string.Empty;
        GoTo(State.EditingId);
    }

    private async Task DeleteAsync(string id)
    {
        var wasOpen = State.EditingId == id;
        if (!await State.DeleteAsync(id, DefaultName, _disposed.Token))
        {
            _deleteFailed = Repository.Find(id) is not null;
            return;
        }

        if (wasOpen)
        {
            _nameText = State.Draft.Name ?? string.Empty;
            GoTo(null);
        }
    }

    private async Task ExportAsync(string id)
    {
        _exportFailed = false;
        _import = ImportMessage.None;
        if (Repository.Custom.FirstOrDefault(level => level.Id == id) is not { } level)
        {
            return;
        }

        _exportFailed = !await FileDownload.SaveTextAsync(LevelEditing.ExportFileName(level.Name), LevelJson.Write(level), _disposed.Token);
    }

    private async Task ImportAsync(InputFileChangeEventArgs args)
    {
        _importKey++;
        _exportFailed = false;
        _importName = null;
        var json = await ReadFileAsync(args.File);
        if (json is null)
        {
            _import = ImportMessage.TooLarge;
            return;
        }

        var result = await Repository.ImportAsync(json, DefaultName, _disposed.Token);
        _importName = result.Level?.Name;
        _import = result.Outcome switch
        {
            ImportOutcome.Imported => ImportMessage.Ok,
            ImportOutcome.NotSavable => ImportMessage.NotSavable,
            ImportOutcome.LimitReached => ImportMessage.LimitReached,
            ImportOutcome.StorageFailed => ImportMessage.StorageFailed,
            _ => ImportMessage.Invalid,
        };
    }

    private async Task<string?> ReadFileAsync(IBrowserFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream(MaxImportBytes, _disposed.Token);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var buffer = new char[LevelJson.MaxJsonLength + 1];
            var length = await reader.ReadBlockAsync(buffer, _disposed.Token);
            return length > LevelJson.MaxJsonLength ? null : new string(buffer, 0, length);
        }
        catch (IOException)
        {
            return null;
        }
    }

    private async Task SaveAsync()
    {
        var outcome = await State.SaveAsync(_disposed.Token);
        if (outcome == SaveOutcome.Saved && _openedFor is null && State.EditingId is { } id)
        {
            GoTo(id, replace: true);
        }
    }

    private async Task TryOutAsync()
    {
        _trying = true;
        try
        {
            var outcome = await State.SaveAsync(_disposed.Token);
            if (outcome == SaveOutcome.Saved && State.EditingId is { } id)
            {
                await OnTryOut.InvokeAsync(id);
            }
        }
        finally
        {
            _trying = false;
        }
    }

    private void GoTo(string? id, bool replace = false)
    {
        _openedFor = id;
        var target = id is null ? "editor" : $"editor/{id}";
        _allowedTarget = target;
        Navigation.NavigateTo(target, replace: replace);
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
