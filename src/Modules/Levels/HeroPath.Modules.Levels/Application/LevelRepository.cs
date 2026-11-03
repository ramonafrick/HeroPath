using HeroPath.Modules.Levels.Application.Contracts;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Application;

/// <summary>
/// The standard levels plus the child's custom levels. Standard levels are fixed; custom levels are loaded once with
/// <see cref="LoadAsync"/> and changed through <see cref="SaveAsync"/>, <see cref="DeleteAsync"/> and <see cref="ImportAsync"/>, which write
/// the whole list to the <see cref="ICustomLevelStore"/> first and only then update the memory, so a failed write changes nothing.
/// The three changes and the load run one after another (a gate), so overlapping calls never overwrite each other with a stale list;
/// a change before the load first loads the stored levels. <see cref="Changed"/> is raised after the gate is released.
/// Level lists are replaced, never modified, so a list handed out earlier stays consistent.
/// </summary>
public sealed class LevelRepository
{
    private readonly ICustomLevelStore _store;
    private readonly Func<string?, LevelReadResult> _readLevel;
    private readonly HashSet<string> _standardIds;
    private IReadOnlyList<LevelDefinition> _custom = [];
    private IReadOnlyList<LevelDefinition> _all;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _loaded;

    /// <param name="store">Where custom levels are kept.</param>
    /// <param name="standard">The standard levels in play order (passed in because Application must not use Infrastructure).</param>
    /// <param name="readLevel">Parses and validates level JSON for imports (<c>LevelJson.Read</c>), passed in for the same reason.</param>
    public LevelRepository(ICustomLevelStore store, IReadOnlyList<LevelDefinition> standard, Func<string?, LevelReadResult> readLevel)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(standard);
        ArgumentNullException.ThrowIfNull(readLevel);
        _store = store;
        _readLevel = readLevel;
        Standard = [.. standard];
        _standardIds = [.. Standard.Select(level => level.Id)];
        _all = Standard;
    }

    /// <summary>Raised after the custom levels changed (saved, deleted, imported, or loaded with at least one level).</summary>
    public event Action? Changed;

    /// <summary>The standard levels in play order.</summary>
    public IReadOnlyList<LevelDefinition> Standard { get; }

    /// <summary>The custom levels in saved order.</summary>
    public IReadOnlyList<LevelDefinition> Custom => _custom;

    /// <summary>Standard levels first, then the custom levels.</summary>
    public IReadOnlyList<LevelDefinition> All => _all;

    public LevelDefinition? Find(string id) => _all.FirstOrDefault(level => level.Id == id);

    public bool IsCustom(string id) => IndexOf(id) >= 0;

    /// <summary>The next free default name built with <paramref name="format"/>, for example "My level 2".</summary>
    public string NextDefaultName(Func<int, string> format)
    {
        ArgumentNullException.ThrowIfNull(format);
        return format(LevelEditing.NextDefaultNumber(_custom.Select(level => level.Name), format));
    }

    /// <summary>Loads the custom levels once; later calls do nothing. A failing storage means no custom levels.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var changed = false;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            changed = await EnsureLoadedAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        RaiseChanged(changed);
    }

    /// <summary>Saves a new custom level (appended) or overwrites the one with the same id (keeps its position).</summary>
    public async Task<SaveOutcome> SaveAsync(LevelDefinition level, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(level);
        if (level.Name is null || _standardIds.Contains(level.Id) || !LevelCheck.Evaluate(level).CanSave)
        {
            return SaveOutcome.Invalid;
        }

        var changed = false;
        SaveOutcome outcome;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            changed = await EnsureLoadedAsync(cancellationToken);
            var index = IndexOf(level.Id);
            if (index < 0 && _custom.Count >= LevelLimits.MaxCustomLevels)
            {
                outcome = SaveOutcome.LimitReached;
            }
            else
            {
                List<LevelDefinition> next = [.. _custom];
                if (index < 0)
                {
                    next.Add(level);
                }
                else
                {
                    next[index] = level;
                }

                var written = await WriteAsync(next, cancellationToken);
                changed |= written;
                outcome = written ? SaveOutcome.Saved : SaveOutcome.StorageFailed;
            }
        }
        finally
        {
            _gate.Release();
        }

        RaiseChanged(changed);
        return outcome;
    }

    /// <summary>Deletes a custom level. Returns <c>false</c> for an unknown id and when the storage could not be written (the list stays).</summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var changed = false;
        var deleted = false;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            changed = await EnsureLoadedAsync(cancellationToken);
            var index = IndexOf(id);
            if (index >= 0)
            {
                List<LevelDefinition> next = [.. _custom];
                next.RemoveAt(index);
                deleted = await WriteAsync(next, cancellationToken);
                changed |= deleted;
            }
        }
        finally
        {
            _gate.Release();
        }

        RaiseChanged(changed);
        return deleted;
    }

    /// <summary>
    /// Imports level JSON from a file as a new custom level: validated like a saved level, always a new id (the id in the file is dropped),
    /// name cleaned or, when missing or empty, a default name built with <paramref name="defaultNameFormat"/>.
    /// </summary>
    public async Task<ImportResult> ImportAsync(string? json, Func<int, string> defaultNameFormat, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(defaultNameFormat);
        if (_readLevel(json).Level is not { } read)
        {
            return new(ImportOutcome.InvalidFile);
        }

        if (!LevelCheck.Evaluate(read).CanSave)
        {
            return new(ImportOutcome.NotSavable);
        }

        var changed = false;
        ImportResult result;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            changed = await EnsureLoadedAsync(cancellationToken);
            if (_custom.Count >= LevelLimits.MaxCustomLevels)
            {
                result = new(ImportOutcome.LimitReached);
            }
            else
            {
                var name = LevelEditing.CleanName(read.Name);
                var level = read with
                {
                    Id = LevelIdGenerator.NewId(id => _standardIds.Contains(id) || IndexOf(id) >= 0),
                    Name = name.Length > 0 ? name : NextDefaultName(defaultNameFormat),
                };

                var written = await WriteAsync([.. _custom, level], cancellationToken);
                changed |= written;
                result = written ? new(ImportOutcome.Imported, level) : new(ImportOutcome.StorageFailed);
            }
        }
        finally
        {
            _gate.Release();
        }

        RaiseChanged(changed);
        return result;
    }

    private int IndexOf(string id)
    {
        var custom = _custom;
        for (var i = 0; i < custom.Count; i++)
        {
            if (custom[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Loads once, inside the gate, so a change never writes a list that does not know the stored levels. Returns whether levels were found.</summary>
    private async Task<bool> EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
        {
            return false;
        }

        IReadOnlyList<LevelDefinition> loaded;
        try
        {
            loaded = await _store.LoadAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            loaded = [];
        }

        _loaded = true;
        var seen = new HashSet<string>(_standardIds);
        List<LevelDefinition> custom = [.. loaded.Where(level => seen.Add(level.Id))];
        Replace(custom);
        return custom.Count > 0;
    }

    private async Task<bool> WriteAsync(List<LevelDefinition> next, CancellationToken cancellationToken)
    {
        bool written;
        try
        {
            written = await _store.SaveAllAsync(next, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            written = false;
        }

        if (written)
        {
            Replace(next);
        }

        return written;
    }

    private void Replace(IReadOnlyList<LevelDefinition> custom)
    {
        _all = [.. Standard, .. custom];
        _custom = custom;
    }

    private void RaiseChanged(bool changed)
    {
        if (changed)
        {
            Changed?.Invoke();
        }
    }
}
