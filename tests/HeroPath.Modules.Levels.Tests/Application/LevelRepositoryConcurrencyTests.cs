using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;

namespace HeroPath.Modules.Levels.Tests.Application;

public class LevelRepositoryConcurrencyTests
{
    private static readonly LevelDefinition[] _standard = [CustomLevelSamples.Valid("std-a") with { Name = null }];

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private static string DefaultName(int number) => $"Mein Level {number}";

    private static LevelRepository NewRepository(GatedLevelStore store) => new(store, _standard, LevelJson.Read);

    [Fact]
    public async Task Save_TwoOverlappingSaves_KeepBothLevels()
    {
        var store = new GatedLevelStore();
        var repository = NewRepository(store);

        var first = repository.SaveAsync(CustomLevelSamples.Valid("custom-a"), Token);
        var second = repository.SaveAsync(CustomLevelSamples.Valid("custom-b"), Token);
        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);
        store.OpenGate();

        Assert.Equal([SaveOutcome.Saved, SaveOutcome.Saved], await Task.WhenAll(first, second));
        Assert.Equal(["custom-a", "custom-b"], repository.Custom.Select(l => l.Id));
        Assert.Equal(["custom-a", "custom-b"], store.Stored.Select(l => l.Id));
    }

    [Fact]
    public async Task Save_OverlappingSaveAndDelete_KeepsTheResultOfBoth()
    {
        var store = new GatedLevelStore([CustomLevelSamples.Valid("custom-a"), CustomLevelSamples.Valid("custom-b")]);
        var repository = NewRepository(store);
        await repository.LoadAsync(Token);

        var save = repository.SaveAsync(CustomLevelSamples.Valid("custom-c"), Token);
        var delete = repository.DeleteAsync("custom-a", Token);
        store.OpenGate();

        Assert.Equal(SaveOutcome.Saved, await save);
        Assert.True(await delete);
        Assert.Equal(["custom-b", "custom-c"], repository.Custom.Select(l => l.Id));
        Assert.Equal(["custom-b", "custom-c"], store.Stored.Select(l => l.Id));
    }

    [Fact]
    public async Task Import_OverlappingSave_KeepsBothLevels()
    {
        var store = new GatedLevelStore();
        var repository = NewRepository(store);
        var json = LevelJson.Write(CustomLevelSamples.Valid("file-id", "Imported"));

        var save = repository.SaveAsync(CustomLevelSamples.Valid("custom-a"), Token);
        var import = repository.ImportAsync(json, DefaultName, Token);
        store.OpenGate();

        Assert.Equal(SaveOutcome.Saved, await save);
        Assert.Equal(ImportOutcome.Imported, (await import).Outcome);
        Assert.Equal(2, repository.Custom.Count);
        Assert.Equal(2, store.Stored.Count);
    }

    [Fact]
    public async Task Save_TwoNewLevelsWithOneSlotLeft_OnlyOneIsSaved()
    {
        var store = new GatedLevelStore([.. Enumerable.Range(0, LevelLimits.MaxCustomLevels - 1).Select(i => CustomLevelSamples.Valid($"custom-{i}"))]);
        var repository = NewRepository(store);
        await repository.LoadAsync(Token);

        var first = repository.SaveAsync(CustomLevelSamples.Valid("custom-x"), Token);
        var second = repository.SaveAsync(CustomLevelSamples.Valid("custom-y"), Token);
        store.OpenGate();

        Assert.Equal([SaveOutcome.Saved, SaveOutcome.LimitReached], await Task.WhenAll(first, second));
        Assert.Equal(LevelLimits.MaxCustomLevels, repository.Custom.Count);
    }

    [Fact]
    public async Task Save_OverlappingSavesOfTheSameId_EndWithTheLaterOne()
    {
        var store = new GatedLevelStore();
        var repository = NewRepository(store);

        var first = repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "First"), Token);
        var second = repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Second"), Token);
        store.OpenGate();
        await Task.WhenAll(first, second);

        Assert.Equal("Second", Assert.Single(repository.Custom).Name);
        Assert.Equal("Second", Assert.Single(store.Stored).Name);
    }

    [Fact]
    public async Task Save_BeforeLoad_LoadsStoredLevelsFirstAndKeepsThem()
    {
        var store = new GatedLevelStore([CustomLevelSamples.Valid("custom-old")]);
        store.OpenGate();
        var repository = NewRepository(store);

        await repository.SaveAsync(CustomLevelSamples.Valid("custom-new"), Token);
        await repository.LoadAsync(Token);

        Assert.Equal(["custom-old", "custom-new"], store.Stored.Select(l => l.Id));
        Assert.Equal(["custom-old", "custom-new"], repository.Custom.Select(l => l.Id));
        Assert.Equal(1, store.Loads);
    }

    [Fact]
    public async Task Changed_IsRaisedAfterTheGateIsReleased()
    {
        var store = new GatedLevelStore();
        store.OpenGate();
        var repository = NewRepository(store);
        Task<SaveOutcome>? nested = null;
        var entered = false;
        repository.Changed += () =>
        {
            if (entered)
            {
                return;
            }

            entered = true;
            nested = repository.SaveAsync(CustomLevelSamples.Valid("custom-nested"), Token);
            Assert.True(nested.IsCompleted);
        };

        await repository.SaveAsync(CustomLevelSamples.Valid("custom-a"), Token);

        Assert.Equal(SaveOutcome.Saved, await nested!);
        Assert.Equal(2, repository.Custom.Count);
    }
}
