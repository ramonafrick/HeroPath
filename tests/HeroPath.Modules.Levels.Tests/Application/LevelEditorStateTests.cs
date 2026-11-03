using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Application.Contracts;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;

namespace HeroPath.Modules.Levels.Tests.Application;

public class LevelEditorStateTests
{
    private static readonly LevelDefinition[] _standard = [CustomLevelSamples.Valid("std-a") with { Name = null }];

    private readonly InMemoryAppStorage _storage = new();
    private readonly LevelRepository _repository;
    private readonly LevelEditorState _state;

    public LevelEditorStateTests()
    {
        _repository = new(new StorageCustomLevelStore(_storage), _standard, LevelJson.Read);
        _state = new(_repository);
    }

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private static string DefaultName(int number) => $"Mein Level {number}";

    private void ClickAll(EditorTool tool, params Position[] positions)
    {
        _state.SelectTool(tool);
        foreach (var position in positions)
        {
            _state.Apply(position);
        }
    }

    [Fact]
    public void OpenNew_StartsAFreshTenByTenLevelWithDefaultNameAndId()
    {
        _state.OpenNew(DefaultName);

        Assert.Equal(10, _state.Draft.Width);
        Assert.Equal(10, _state.Draft.Height);
        Assert.Equal(new Position(0, 9), _state.Draft.Start);
        Assert.Equal(new Position(9, 0), _state.Draft.Goal);
        Assert.Equal("Mein Level 1", _state.Draft.Name);
        Assert.Matches("^custom-[0-9a-f]{12}$", _state.Draft.Id);
        Assert.Null(_state.EditingId);
        Assert.False(_state.IsDirty);
        Assert.True(_state.Check.CanSave);
        Assert.Equal(18, _state.Check.MinimumSteps);
    }

    [Fact]
    public async Task OpenNew_UsesTheNextFreeDefaultName()
    {
        _state.OpenNew(DefaultName);
        await _state.SaveAsync(Token);

        _state.OpenNew(DefaultName);

        Assert.Equal("Mein Level 2", _state.Draft.Name);
    }

    [Fact]
    public async Task Open_CustomLevel_LoadsItAndKeepsItsId()
    {
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Burgweg"), Token);

        var opened = _state.Open("custom-a");

        Assert.True(opened);
        Assert.Equal("custom-a", _state.Draft.Id);
        Assert.Equal("custom-a", _state.EditingId);
        Assert.Equal("Burgweg", _state.Draft.Name);
        Assert.False(_state.IsDirty);
    }

    [Theory]
    [InlineData("nope")]
    [InlineData("std-a")]
    [InlineData("")]
    public void Open_UnknownOrStandardLevel_ReturnsFalseAndChangesNothing(string id)
    {
        _state.OpenNew(DefaultName);
        var draft = _state.Draft;

        Assert.False(_state.Open(id));

        Assert.Same(draft, _state.Draft);
        Assert.Null(_state.EditingId);
    }

    [Fact]
    public void Apply_PlacesAnObstacle_MarksDirtyAndRechecks()
    {
        _state.OpenNew(DefaultName);

        ClickAll(EditorTool.Tree, new Position(5, 5));

        Assert.Equal(ObstacleKind.Tree, _state.Draft.ToGrid().GetObstacle(new(5, 5)));
        Assert.True(_state.IsDirty);
        Assert.Equal(EditHint.None, _state.Hint);
    }

    [Fact]
    public void Apply_WallAroundTheGoal_MakesTheLevelUnsolvable()
    {
        _state.OpenNew(DefaultName);

        ClickAll(EditorTool.Rock, new Position(8, 0), new Position(9, 1));

        Assert.Equal(LevelCheckStatus.GoalUnreachable, _state.Check.Status);
        Assert.False(_state.CanSave);
    }

    [Fact]
    public void Apply_RefusedClick_KeepsTheDraftAndSetsTheHint()
    {
        _state.OpenNew(DefaultName);
        var draft = _state.Draft;

        ClickAll(EditorTool.Tree, _state.Draft.Start);

        Assert.Same(draft, _state.Draft);
        Assert.Equal(EditHint.ObstacleOnStart, _state.Hint);
        Assert.False(_state.IsDirty);
    }

    [Fact]
    public void Apply_NextValidClick_ClearsTheHint()
    {
        _state.OpenNew(DefaultName);
        ClickAll(EditorTool.Tree, _state.Draft.Start, new(4, 4));

        Assert.Equal(EditHint.None, _state.Hint);
    }

    [Fact]
    public void Apply_ElevenStrawberries_RefusesTheLastOne()
    {
        _state.OpenNew(DefaultName);

        ClickAll(EditorTool.Strawberry, [.. Enumerable.Range(0, 11).Select(i => new Position(i % 10, 4 + i / 10))]);

        Assert.Equal(LevelLimits.MaxStrawberries, _state.Draft.Items.Count);
        Assert.Equal(EditHint.TooManyStrawberries, _state.Hint);
    }

    [Fact]
    public void Resize_Shrinking_ReportsRemovedThings_AndClearsOnTheNextAction()
    {
        _state.OpenNew(DefaultName);
        ClickAll(EditorTool.Tree, new Position(7, 3), new Position(8, 3));

        _state.Resize(5, 10);

        Assert.Equal(5, _state.Draft.Width);
        Assert.Equal(2, _state.RemovedCount);
        Assert.True(_state.IsDirty);

        _state.SelectTool(EditorTool.Rock);
        Assert.Equal(0, _state.RemovedCount);
    }

    [Fact]
    public void Resize_ToTheSameSize_ChangesNothing()
    {
        _state.OpenNew(DefaultName);
        var draft = _state.Draft;

        _state.Resize(10, 10);

        Assert.Same(draft, _state.Draft);
        Assert.False(_state.IsDirty);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(99, 20)]
    public void Resize_ClampsToTheLimits(int requested, int expected)
    {
        _state.OpenNew(DefaultName);

        _state.Resize(requested, requested);

        Assert.Equal(expected, _state.Draft.Width);
        Assert.Equal(expected, _state.Draft.Height);
        Assert.True(_state.Check.CanSave);
    }

    [Fact]
    public void SetName_CleansTheText()
    {
        _state.OpenNew(DefaultName);

        _state.SetName("  Mein\u0007 Weg​  ");

        Assert.Equal("Mein Weg", _state.Draft.Name);
        Assert.True(_state.IsDirty);
    }

    [Fact]
    public void SetName_TooLong_IsCutToFiftyCharacters()
    {
        _state.OpenNew(DefaultName);

        _state.SetName(new string('a', 80));

        Assert.Equal(LevelLimits.MaxNameLength, _state.Draft.Name!.Length);
        Assert.True(_state.Check.CanSave);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SetName_Empty_ReportsInvalidAndBlocksSaving(string? raw)
    {
        _state.OpenNew(DefaultName);

        _state.SetName(raw);

        Assert.Equal(string.Empty, _state.Draft.Name);
        Assert.Equal(LevelCheckStatus.Invalid, _state.Check.Status);
        Assert.Contains(_state.Check.Errors, e => e.Kind == LevelValidationErrorKind.InvalidName);
        Assert.False(_state.CanSave);
    }

    [Fact]
    public void SetRequireAllItems_NeedsAStrawberry_AndSwitchesOffWithTheLastOne()
    {
        _state.OpenNew(DefaultName);
        _state.SetRequireAllItems(true);
        Assert.False(_state.Draft.RequireAllItems);

        ClickAll(EditorTool.Strawberry, new Position(3, 3));
        _state.SetRequireAllItems(true);
        Assert.True(_state.Draft.RequireAllItems);

        ClickAll(EditorTool.Strawberry, new Position(3, 3));
        Assert.False(_state.Draft.RequireAllItems);
    }

    [Fact]
    public async Task Save_NewLevel_StoresItAndKeepsTheIdForTheNextSave()
    {
        _state.OpenNew(DefaultName);
        var id = _state.Draft.Id;
        ClickAll(EditorTool.Tree, new Position(4, 4));

        var outcome = await _state.SaveAsync(Token);

        Assert.Equal(SaveOutcome.Saved, outcome);
        Assert.False(_state.IsDirty);
        Assert.Equal(id, _state.EditingId);
        Assert.Equal(SaveOutcome.Saved, _state.LastSave);
        Assert.Equal([id], _repository.Custom.Select(l => l.Id));
        Assert.Equal(id, _state.Draft.Id);
    }

    [Fact]
    public async Task Save_Twice_OverwritesInsteadOfAddingASecondLevel()
    {
        _state.OpenNew(DefaultName);
        await _state.SaveAsync(Token);
        ClickAll(EditorTool.Tree, new Position(4, 4));
        Assert.True(_state.IsDirty);

        await _state.SaveAsync(Token);

        Assert.Single(_repository.Custom);
        Assert.Single(_repository.Custom[0].Obstacles);
        Assert.False(_state.IsDirty);
    }

    [Fact]
    public async Task Save_EditingAnExistingLevel_OverwritesItInPlace()
    {
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Eins"), Token);
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-b", "Zwei"), Token);
        _state.Open("custom-a");
        _state.SetName("Eins neu");

        var outcome = await _state.SaveAsync(Token);

        Assert.Equal(SaveOutcome.Saved, outcome);
        Assert.Equal(["Eins neu", "Zwei"], _repository.Custom.Select(l => l.Name));
    }

    [Fact]
    public async Task Save_Unsolvable_IsRefusedAndStoresNothing()
    {
        _state.OpenNew(DefaultName);
        ClickAll(EditorTool.Rock, new Position(8, 0), new Position(9, 1));

        var outcome = await _state.SaveAsync(Token);

        Assert.Equal(SaveOutcome.Invalid, outcome);
        Assert.Empty(_repository.Custom);
        Assert.Empty(_storage.Values);
    }

    [Fact]
    public async Task Save_TwentyFirstLevel_IsRefusedWithLimitReachedAndStaysOpen()
    {
        for (var i = 0; i < LevelLimits.MaxCustomLevels; i++)
        {
            await _repository.SaveAsync(CustomLevelSamples.Valid($"custom-{i}"), Token);
        }

        _state.OpenNew(DefaultName);
        ClickAll(EditorTool.Tree, new Position(4, 4));
        var outcome = await _state.SaveAsync(Token);

        Assert.Equal(SaveOutcome.LimitReached, outcome);
        Assert.Equal(SaveOutcome.LimitReached, _state.LastSave);
        Assert.True(_state.IsDirty);
        Assert.Null(_state.EditingId);
        Assert.Equal(LevelLimits.MaxCustomLevels, _repository.Custom.Count);
    }

    [Fact]
    public async Task Save_StorageFailure_KeepsTheDraftOpenAndDirty()
    {
        _state.OpenNew(DefaultName);
        ClickAll(EditorTool.Tree, new Position(4, 4));
        var draft = _state.Draft;
        _storage.FailWrites = true;

        var outcome = await _state.SaveAsync(Token);

        Assert.Equal(SaveOutcome.StorageFailed, outcome);
        Assert.Equal(SaveOutcome.StorageFailed, _state.LastSave);
        Assert.Same(draft, _state.Draft);
        Assert.True(_state.IsDirty);
        Assert.Null(_state.EditingId);
        Assert.Empty(_repository.Custom);

        _storage.FailWrites = false;
        Assert.Equal(SaveOutcome.Saved, await _state.SaveAsync(Token));
        Assert.False(_state.IsDirty);
    }

    [Fact]
    public async Task Save_Result_IsClearedByTheNextChange()
    {
        _state.OpenNew(DefaultName);
        await _state.SaveAsync(Token);

        _state.SelectTool(EditorTool.Pond);

        Assert.Null(_state.LastSave);
    }

    [Fact]
    public async Task Save_WhileAnotherSaveRuns_DoesNothing()
    {
        var store = new SlowStore();
        var state = new LevelEditorState(new LevelRepository(store, _standard, LevelJson.Read));
        state.OpenNew(DefaultName);

        var first = state.SaveAsync(Token);
        Assert.True(state.IsSaving);
        Assert.False(state.CanSave);
        Assert.Equal(SaveOutcome.Invalid, await state.SaveAsync(Token));

        store.Release();
        Assert.Equal(SaveOutcome.Saved, await first);
        Assert.Equal(1, store.Writes);
        Assert.False(state.IsSaving);
    }

    [Fact]
    public async Task Save_EditedWhileSaving_StaysDirty()
    {
        var store = new SlowStore();
        var state = new LevelEditorState(new LevelRepository(store, _standard, LevelJson.Read));
        state.OpenNew(DefaultName);

        var save = state.SaveAsync(Token);
        state.SetName("Anders");
        store.Release();
        await save;

        Assert.True(state.IsDirty);
        Assert.Null(state.LastSave);
        Assert.Equal(state.Draft.Id, state.EditingId);
    }

    [Fact]
    public void Changed_IsRaisedByEveryKindOfChange()
    {
        var raised = 0;
        _state.Changed += () => raised++;

        _state.OpenNew(DefaultName);
        _state.SelectTool(EditorTool.Rock);
        _state.Apply(new(3, 3));
        _state.Resize(8, 8);
        _state.SetName("x");
        _state.SetRequireAllItems(false);

        Assert.Equal(6, raised);
    }

    private sealed class SlowStore : ICustomLevelStore
    {
        private readonly TaskCompletionSource _gate = new();

        public int Writes { get; private set; }

        public void Release() => _gate.SetResult();

        public Task<IReadOnlyList<LevelDefinition>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LevelDefinition>>([]);

        public async Task<bool> SaveAllAsync(IReadOnlyList<LevelDefinition> levels, CancellationToken cancellationToken)
        {
            Writes++;
            await _gate.Task;
            return true;
        }
    }
}
