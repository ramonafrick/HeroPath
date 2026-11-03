using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application;

/// <summary>
/// The level being edited: the draft, the active tool, the unsaved flag, the message of the last click and the check result.
/// A service (scoped, like the repository), so the draft survives anything that only re-renders the page, such as a language switch.
/// The draft keeps one id from the moment it is created, so saving the same draft twice overwrites instead of adding a second level.
/// Every change replaces <see cref="Draft"/> and recomputes <see cref="Check"/>.
/// </summary>
public sealed class LevelEditorState
{
    private readonly LevelRepository _repository;
    private int _generation;

    public LevelEditorState(LevelRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
        Draft = LevelEditing.NewLevel(NewId(), string.Empty);
        Check = LevelCheck.Evaluate(Draft);
    }

    /// <summary>Raised after every change of the state.</summary>
    public event Action? Changed;

    /// <summary>The level being edited. Its name is empty (not <c>null</c>) while the child has not typed one.</summary>
    public LevelDefinition Draft { get; private set; }

    /// <summary>The id of the saved custom level this draft belongs to; <c>null</c> while it was never saved.</summary>
    public string? EditingId { get; private set; }

    public EditorTool Tool { get; private set; } = EditorTool.Tree;

    /// <summary>The draft differs from what is saved (or from the freshly opened level).</summary>
    public bool IsDirty { get; private set; }

    /// <summary>Why the last click changed nothing, <see cref="EditHint.None"/> otherwise.</summary>
    public EditHint Hint { get; private set; }

    /// <summary>How many obstacles and items the last resize removed.</summary>
    public int RemovedCount { get; private set; }

    /// <summary>Result of the last save attempt of this draft; cleared by the next change.</summary>
    public SaveOutcome? LastSave { get; private set; }

    public bool IsSaving { get; private set; }

    public LevelCheckResult Check { get; private set; }

    /// <summary>The draft can be saved (valid, solvable, within the step limit) and no save is running.</summary>
    public bool CanSave => Check.CanSave && !IsSaving;

    /// <summary>Starts a new 10x10 level with a fresh id and the next free default name built with <paramref name="defaultNameFormat"/>.</summary>
    public void OpenNew(Func<int, string> defaultNameFormat)
    {
        ArgumentNullException.ThrowIfNull(defaultNameFormat);
        Open(LevelEditing.NewLevel(NewId(), _repository.NextDefaultName(defaultNameFormat)), editingId: null);
    }

    /// <summary>Opens a saved custom level. Returns <c>false</c> (and changes nothing) for unknown ids and standard levels.</summary>
    public bool Open(string id)
    {
        if (id is null || !_repository.IsCustom(id) || _repository.Find(id) is not { } level)
        {
            return false;
        }

        Open(level, level.Id);
        return true;
    }

    /// <summary>
    /// Opens an unsaved copy of a standard level with a fresh id and the next free default name. The standard level stays untouched; the copy
    /// counts as not dirty until it is changed. Returns <c>false</c> (and changes nothing) for ids that are not standard levels.
    /// </summary>
    public bool OpenTemplate(string templateId, Func<int, string> defaultNameFormat)
    {
        ArgumentNullException.ThrowIfNull(defaultNameFormat);
        if (_repository.Standard.FirstOrDefault(level => level.Id == templateId) is not { } template)
        {
            return false;
        }

        Open(LevelEditing.FromTemplate(template, NewId(), _repository.NextDefaultName(defaultNameFormat)), editingId: null);
        return true;
    }

    /// <summary>Removes all obstacles and items and switches «all strawberries needed» off. Start, goal and size stay.</summary>
    public void ClearObjects()
    {
        ClearMessages();
        if (Draft.Obstacles.Count > 0 || Draft.Items.Count > 0 || Draft.RequireAllItems)
        {
            Replace(LevelEditing.ClearObjects(Draft));
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Deletes a saved custom level. When it is the level open in the editor, the editor starts a new level (not dirty), because the
    /// draft would otherwise be saved again under the deleted id. Returns <c>false</c> when nothing was deleted (unknown id or storage failure).
    /// </summary>
    public async Task<bool> DeleteAsync(string id, Func<int, string> defaultNameFormat, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(defaultNameFormat);
        if (!await _repository.DeleteAsync(id, cancellationToken))
        {
            return false;
        }

        if (EditingId == id)
        {
            OpenNew(defaultNameFormat);
        }

        return true;
    }

    public void SelectTool(EditorTool tool)
    {
        if (!Enum.IsDefined(tool))
        {
            return;
        }

        Tool = tool;
        ClearMessages();
        Changed?.Invoke();
    }

    /// <summary>Applies the active tool to a cell.</summary>
    public void Apply(Position position)
    {
        var result = LevelEditing.Apply(Draft, Tool, position);
        ClearMessages();
        Hint = result.Hint;
        if (!ReferenceEquals(result.Level, Draft))
        {
            Replace(result.Level);
        }

        Changed?.Invoke();
    }

    public void Resize(int width, int height)
    {
        ClearMessages();
        var clampedWidth = Math.Clamp(width, LevelLimits.MinGridSize, LevelLimits.MaxGridSize);
        var clampedHeight = Math.Clamp(height, LevelLimits.MinGridSize, LevelLimits.MaxGridSize);
        if (clampedWidth != Draft.Width || clampedHeight != Draft.Height)
        {
            var result = LevelEditing.Resize(Draft, clampedWidth, clampedHeight);
            RemovedCount = result.RemovedCount;
            Replace(result.Level);
        }

        Changed?.Invoke();
    }

    /// <summary>Sets the name from what the child typed: cleaned and cut to 50 characters. Nothing left means the level cannot be saved.</summary>
    public void SetName(string? raw)
    {
        var name = LevelEditing.CleanName(raw);
        LastSave = null;
        if (name != Draft.Name)
        {
            Replace(Draft with { Name = name });
        }

        Changed?.Invoke();
    }

    /// <summary>Switches «all strawberries needed». Turning it on without a strawberry does nothing.</summary>
    public void SetRequireAllItems(bool value)
    {
        ClearMessages();
        var level = LevelEditing.SetRequireAllItems(Draft, value);
        if (!ReferenceEquals(level, Draft))
        {
            Replace(level);
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Saves the draft through the repository (new level or overwrite by id). On <see cref="SaveOutcome.Saved"/> the draft is no longer dirty
    /// and <see cref="EditingId"/> is set; on any other outcome the draft stays open and dirty. Returns <see cref="SaveOutcome.Invalid"/>
    /// without doing anything while the check forbids saving or another save is running.
    /// </summary>
    public async Task<SaveOutcome> SaveAsync(CancellationToken cancellationToken)
    {
        if (!CanSave)
        {
            return SaveOutcome.Invalid;
        }

        var saving = Draft;
        var generation = _generation;
        IsSaving = true;
        Changed?.Invoke();
        SaveOutcome outcome;
        try
        {
            outcome = await _repository.SaveAsync(saving, cancellationToken);
        }
        finally
        {
            IsSaving = false;
        }

        if (generation == _generation)
        {
            if (outcome == SaveOutcome.Saved)
            {
                EditingId = saving.Id;
                IsDirty = !ReferenceEquals(Draft, saving);
            }

            if (ReferenceEquals(Draft, saving))
            {
                LastSave = outcome;
            }
        }

        Changed?.Invoke();
        return outcome;
    }

    private void Open(LevelDefinition level, string? editingId)
    {
        _generation++;
        Draft = level;
        EditingId = editingId;
        IsDirty = false;
        IsSaving = false;
        ClearMessages();
        Check = LevelCheck.Evaluate(level);
        Changed?.Invoke();
    }

    private void Replace(LevelDefinition level)
    {
        Draft = level;
        IsDirty = true;
        Check = LevelCheck.Evaluate(level);
    }

    private void ClearMessages()
    {
        Hint = EditHint.None;
        RemovedCount = 0;
        LastSave = null;
    }

    private string NewId() => LevelIdGenerator.NewId(id => _repository.Find(id) is not null);
}
