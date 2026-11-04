using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Levels.UI.Components;

/// <summary>
/// The level editor: board, tools, size, name, check and save, plus the list of the child's own levels. All rules and the draft live in
/// <see cref="LevelEditorState"/> (a service), so a language switch only re-renders. Opens the level named by <see cref="LevelId"/>
/// (a new level when there is none or the id is not a custom level) whenever that parameter changes. Import and export are in <see cref="EditorLevelFiles"/>.
/// </summary>
public partial class LevelEditor : LocalizedComponentBase
{
    private static readonly Dictionary<Pending, Question> _questions = new()
    {
        [Pending.New] = new("Discard", CancelStays: true),
        [Pending.Template] = new("Discard", CancelStays: true),
        [Pending.Open] = new("Discard", CancelStays: true),
        [Pending.Delete] = new("Delete", CancelStays: false),
        [Pending.Clear] = new("Clear", CancelStays: false),
        [Pending.Leave] = new("Leave", CancelStays: true),
    };

    private readonly CancellationTokenSource _disposed = new();
    private EditorLevelFiles? _files;
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

    /// <summary>The id of the custom level to open. <c>null</c> or an unknown id starts a new level.</summary>
    [Parameter]
    public string? LevelId { get; set; }

    /// <summary>Optional heading row (title, back link) that spans the whole editor, so it lines up with the board and the panel.</summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

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
    private IStringLocalizer<LevelsResources> Localizer { get; set; } = default!;

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

    private string? HintText => EditorTexts.Hint(Localizer, State.Hint, State.RemovedCount);

    private string? SavedText => EditorTexts.Saved(Localizer, State.LastSave);

    /// <summary>Texts of the open confirm question (resx keys <c>Confirm_{Prefix}_Title|Message|Yes</c>); empty while nothing is pending.</summary>
    private DialogTexts Dialog => _questions.TryGetValue(_pending, out var question)
        ? new(
            Localizer[$"Confirm_{question.Prefix}_Title"].Value,
            Localizer[$"Confirm_{question.Prefix}_Message", _pendingName ?? string.Empty].Value,
            Localizer[$"Confirm_{question.Prefix}_Yes"].Value,
            question.CancelStays ? Localizer["Confirm_Stay"].Value : null)
        : new(string.Empty, string.Empty, null, null);

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

    /// <summary>An untouched default name follows the language («Mein Level 1» becomes «My level 1»); a name the child typed stays.</summary>
    protected override void OnLanguageChanged()
    {
        if (State.RenameDefault(DefaultName))
        {
            _nameText = State.Draft.Name ?? string.Empty;
        }
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

    private string CellLabel(Position position) => EditorTexts.CellLabel(Localizer, BoardGrid, position);

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

    private Task ExportAsync(string id) => _files?.ExportAsync(id) ?? Task.CompletedTask;

    private Task BeforeNavigation(LocationChangingContext context)
    {
        var allowed = IsAllowed(context.TargetLocation);
        _allowedTarget = null;
        if (allowed || _trying || !State.IsDirty)
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

    /// <summary>
    /// The editor's own navigation (open, new, URL replace after the first save) is exempt from the leave question. The exemption is only set when the
    /// URL really changes and <see cref="BeforeNavigation"/> clears it on the next navigation, so it never lingers.
    /// </summary>
    private void GoTo(string? id, bool replace = false)
    {
        _openedFor = id;
        var target = id is null ? "editor" : $"editor/{id}";
        _allowedTarget = Navigation.ToAbsoluteUri(target) == new Uri(Navigation.Uri) ? null : target;
        Navigation.NavigateTo(target, replace: replace);
    }

    private readonly record struct Question(string Prefix, bool CancelStays);

    private readonly record struct DialogTexts(string Title, string Message, string? Yes, string? Cancel);
}
