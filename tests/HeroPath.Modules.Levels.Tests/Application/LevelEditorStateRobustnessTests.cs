using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Application.Contracts;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;

namespace HeroPath.Modules.Levels.Tests.Application;

public class LevelEditorStateRobustnessTests
{
    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private static string DefaultName(int number) => $"Mein Level {number}";

    [Fact]
    public async Task SaveAsync_Cancelled_EndsNotSavingAndRaisesChanged()
    {
        var state = new LevelEditorState(new LevelRepository(new CancellingStore(), [], LevelJson.Read));
        state.SetName("Mein Level");
        var changes = 0;
        var savingAtLastChange = true;
        state.Changed += () =>
        {
            changes++;
            savingAtLastChange = state.IsSaving;
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() => state.SaveAsync(Token));

        Assert.False(state.IsSaving);
        Assert.False(savingAtLastChange);
        Assert.Equal(2, changes);
        Assert.True(state.CanSave);
        Assert.True(state.IsDirty);
    }

    [Fact]
    public void SetName_OnlyChangesTheNameValidation_KeepsTheSolverResult()
    {
        var state = new LevelEditorState(new LevelRepository(new StorageCustomLevelStore(new InMemoryAppStorage()), [], LevelJson.Read));
        state.OpenNew(DefaultName);
        var steps = state.Check.MinimumSteps;

        state.SetName("Anderer Name");
        Assert.Equal(steps, state.Check.MinimumSteps);
        Assert.Equal(LevelCheckStatus.Valid, state.Check.Status);

        state.SetName(string.Empty);
        Assert.Equal(LevelCheckStatus.Invalid, state.Check.Status);

        state.SetName("Wieder da");
        Assert.Equal(LevelCheckStatus.Valid, state.Check.Status);
        Assert.Equal(steps, state.Check.MinimumSteps);
    }

    [Fact]
    public void Apply_AfterNameChange_SolvesTheNewGrid()
    {
        var state = new LevelEditorState(new LevelRepository(new StorageCustomLevelStore(new InMemoryAppStorage()), [], LevelJson.Read));
        state.OpenNew(DefaultName);
        state.SetName("Name");
        var before = state.Check.MinimumSteps;

        state.Resize(5, 5);

        Assert.NotEqual(before, state.Check.MinimumSteps);
        Assert.Equal(LevelCheckStatus.Valid, state.Check.Status);
    }

    private sealed class CancellingStore : ICustomLevelStore
    {
        public Task<IReadOnlyList<LevelDefinition>> LoadAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<LevelDefinition>>([]);

        public Task<bool> SaveAllAsync(IReadOnlyList<LevelDefinition> levels, CancellationToken cancellationToken) =>
            throw new OperationCanceledException();
    }
}
