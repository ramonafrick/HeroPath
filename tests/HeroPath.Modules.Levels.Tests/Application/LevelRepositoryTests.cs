using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Application.Contracts;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Modules.Levels.Tests.Application;

public class LevelRepositoryTests
{
    private static readonly LevelDefinition[] _standard =
    [
        CustomLevelSamples.Valid("std-a") with { Name = null },
        CustomLevelSamples.Valid("std-b") with { Name = null },
    ];

    private readonly InMemoryAppStorage _storage = new();
    private readonly LevelRepository _repository;

    public LevelRepositoryTests()
    {
        _repository = NewRepository(_storage);
    }

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private static LevelRepository NewRepository(IAppStorage storage) =>
        new(new StorageCustomLevelStore(storage), _standard, LevelJson.Read);

    private static string DefaultName(int number) => $"Mein Level {number}";

    private static string Stored(InMemoryAppStorage storage) => storage.Values.GetValueOrDefault(StorageKeys.CustomLevels, string.Empty);

    [Fact]
    public void New_HasOnlyStandardLevels()
    {
        Assert.Equal(["std-a", "std-b"], _repository.Standard.Select(l => l.Id));
        Assert.Empty(_repository.Custom);
        Assert.Equal(["std-a", "std-b"], _repository.All.Select(l => l.Id));
        Assert.NotNull(_repository.Find("std-b"));
        Assert.Null(_repository.Find("custom-a"));
        Assert.False(_repository.IsCustom("std-a"));
    }

    [Fact]
    public async Task Load_ReadsStoredLevels_StandardFirst()
    {
        _storage.Values[StorageKeys.CustomLevels] = LevelJson.WriteAll([CustomLevelSamples.Valid("custom-a"), CustomLevelSamples.Valid("custom-b")]);

        await _repository.LoadAsync(Token);

        Assert.Equal(["std-a", "std-b", "custom-a", "custom-b"], _repository.All.Select(l => l.Id));
        Assert.Equal(["custom-a", "custom-b"], _repository.Custom.Select(l => l.Id));
        Assert.True(_repository.IsCustom("custom-a"));
        Assert.NotNull(_repository.Find("custom-b"));
    }

    [Fact]
    public async Task Load_BrokenStorage_GivesNoCustomLevels()
    {
        _storage.Values[StorageKeys.CustomLevels] = "{ not json";

        await _repository.LoadAsync(Token);

        Assert.Empty(_repository.Custom);
        Assert.Equal(2, _repository.All.Count);
    }

    [Fact]
    public async Task Load_StoredLevelWithAStandardId_IsDropped()
    {
        _storage.Values[StorageKeys.CustomLevels] = LevelJson.WriteAll([CustomLevelSamples.Valid("std-a"), CustomLevelSamples.Valid("custom-a")]);

        await _repository.LoadAsync(Token);

        Assert.Equal(["custom-a"], _repository.Custom.Select(l => l.Id));
        Assert.Null(_repository.Find("std-a")!.Name);
    }

    [Fact]
    public async Task Load_StoreThrows_GivesNoCustomLevels()
    {
        var repository = new LevelRepository(new ThrowingStore(), _standard, LevelJson.Read);

        await repository.LoadAsync(Token);

        Assert.Empty(repository.Custom);
    }

    [Fact]
    public async Task Load_StorageThrows_GivesNoCustomLevels()
    {
        var repository = NewRepository(new ThrowingAppStorage());

        await repository.LoadAsync(Token);

        Assert.Empty(repository.Custom);
    }

    [Fact]
    public async Task Load_IsIdempotent_AndDoesNotOverwriteLaterChanges()
    {
        await _repository.LoadAsync(Token);
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a"), Token);
        _storage.Values[StorageKeys.CustomLevels] = "[]";

        await _repository.LoadAsync(Token);

        Assert.Equal(["custom-a"], _repository.Custom.Select(l => l.Id));
    }

    [Fact]
    public async Task Save_NewLevel_IsAppendedAndStored()
    {
        var outcome = await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a"), Token);

        Assert.Equal(SaveOutcome.Saved, outcome);
        Assert.Equal(["std-a", "std-b", "custom-a"], _repository.All.Select(l => l.Id));
        Assert.Contains("custom-a", Stored(_storage));
        var reloaded = NewRepository(_storage);
        await reloaded.LoadAsync(Token);
        Assert.Equal(["custom-a"], reloaded.Custom.Select(l => l.Id));
    }

    [Fact]
    public async Task Save_ExistingId_OverwritesAndKeepsThePosition()
    {
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "One"), Token);
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-b", "Two"), Token);

        var outcome = await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Renamed"), Token);

        Assert.Equal(SaveOutcome.Saved, outcome);
        Assert.Equal(["custom-a", "custom-b"], _repository.Custom.Select(l => l.Id));
        Assert.Equal("Renamed", _repository.Custom[0].Name);
        Assert.Contains("Renamed", Stored(_storage));
        Assert.DoesNotContain("One", Stored(_storage));
    }

    [Fact]
    public async Task Save_TwentyFirstNewLevel_IsRefusedButOverwritingStillWorks()
    {
        for (var i = 1; i <= LevelLimits.MaxCustomLevels; i++)
        {
            Assert.Equal(SaveOutcome.Saved, await _repository.SaveAsync(CustomLevelSamples.Valid($"custom-{i}"), Token));
        }

        var extra = await _repository.SaveAsync(CustomLevelSamples.Valid("custom-extra"), Token);
        var overwrite = await _repository.SaveAsync(CustomLevelSamples.Valid("custom-3", "Changed"), Token);

        Assert.Equal(SaveOutcome.LimitReached, extra);
        Assert.Equal(SaveOutcome.Saved, overwrite);
        Assert.Equal(LevelLimits.MaxCustomLevels, _repository.Custom.Count);
        Assert.Null(_repository.Find("custom-extra"));
    }

    [Fact]
    public async Task Save_InvalidLevel_IsInvalidAndStoresNothing()
    {
        var invalid = CustomLevelSamples.Valid("custom-a") with { Goal = new Position(0, 4) };

        Assert.Equal(SaveOutcome.Invalid, await _repository.SaveAsync(invalid, Token));
        Assert.Empty(_repository.Custom);
        Assert.Empty(Stored(_storage));
    }

    [Fact]
    public async Task Save_UnsolvableLevel_IsInvalid()
    {
        Assert.Equal(SaveOutcome.Invalid, await _repository.SaveAsync(CustomLevelSamples.Unsolvable("custom-a"), Token));
        Assert.Empty(_repository.Custom);
    }

    [Fact]
    public async Task Save_LevelWithoutName_IsInvalid()
    {
        Assert.Equal(SaveOutcome.Invalid, await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a") with { Name = null }, Token));
    }

    [Fact]
    public async Task Save_StandardId_IsInvalidAndStandardLevelStaysUnchanged()
    {
        var outcome = await _repository.SaveAsync(CustomLevelSamples.Valid("std-a", "Hijack"), Token);

        Assert.Equal(SaveOutcome.Invalid, outcome);
        Assert.Empty(_repository.Custom);
        Assert.Null(_repository.Find("std-a")!.Name);
    }

    [Fact]
    public async Task Save_InvalidWinsOverLimit()
    {
        for (var i = 1; i <= LevelLimits.MaxCustomLevels; i++)
        {
            await _repository.SaveAsync(CustomLevelSamples.Valid($"custom-{i}"), Token);
        }

        Assert.Equal(SaveOutcome.Invalid, await _repository.SaveAsync(CustomLevelSamples.Unsolvable("custom-new"), Token));
    }

    [Fact]
    public async Task Save_StorageFails_ReportsItAndKeepsMemoryAndStorage()
    {
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Old"), Token);
        var before = Stored(_storage);
        var changes = 0;
        _repository.Changed += () => changes++;
        _storage.FailWrites = true;

        var added = await _repository.SaveAsync(CustomLevelSamples.Valid("custom-b"), Token);
        var overwritten = await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "New"), Token);

        Assert.Equal(SaveOutcome.StorageFailed, added);
        Assert.Equal(SaveOutcome.StorageFailed, overwritten);
        Assert.Equal(["custom-a"], _repository.Custom.Select(l => l.Id));
        Assert.Equal("Old", _repository.Custom[0].Name);
        Assert.Equal(before, Stored(_storage));
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task Save_StoreThrows_IsStorageFailed()
    {
        var repository = new LevelRepository(new ThrowingStore(), _standard, LevelJson.Read);

        Assert.Equal(SaveOutcome.StorageFailed, await repository.SaveAsync(CustomLevelSamples.Valid("custom-a"), Token));
        Assert.Empty(repository.Custom);
    }

    [Fact]
    public async Task Delete_RemovesTheLevelFromMemoryAndStorage()
    {
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a"), Token);
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-b"), Token);

        var deleted = await _repository.DeleteAsync("custom-a", Token);

        Assert.True(deleted);
        Assert.Equal(["custom-b"], _repository.Custom.Select(l => l.Id));
        Assert.DoesNotContain("custom-a", Stored(_storage));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("std-a")]
    public async Task Delete_UnknownOrStandardId_ReturnsFalseAndChangesNothing(string id)
    {
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a"), Token);
        var changes = 0;
        _repository.Changed += () => changes++;

        Assert.False(await _repository.DeleteAsync(id, Token));

        Assert.Equal(2 + 1, _repository.All.Count);
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task Delete_StorageFails_KeepsTheLevel()
    {
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a"), Token);
        var before = Stored(_storage);
        _storage.FailWrites = true;

        Assert.False(await _repository.DeleteAsync("custom-a", Token));

        Assert.Equal(["custom-a"], _repository.Custom.Select(l => l.Id));
        Assert.Equal(before, Stored(_storage));
    }

    [Fact]
    public async Task Import_ValidFile_BecomesANewLevelWithANewId()
    {
        var file = LevelJson.Write(CustomLevelSamples.Valid("custom-fromfile", "Imported"));

        var result = await _repository.ImportAsync(file, DefaultName, Token);

        Assert.Equal(ImportOutcome.Imported, result.Outcome);
        Assert.NotNull(result.Level);
        Assert.NotEqual("custom-fromfile", result.Level.Id);
        Assert.Matches("^custom-[0-9a-f]{12}$", result.Level.Id);
        Assert.Equal("Imported", result.Level.Name);
        Assert.Same(result.Level, _repository.Find(result.Level.Id));
        Assert.True(_repository.IsCustom(result.Level.Id));
        Assert.Contains(result.Level.Id, Stored(_storage));
    }

    [Fact]
    public async Task Import_SameFileTwice_GivesTwoLevelsAndNeverOverwrites()
    {
        var file = LevelJson.Write(CustomLevelSamples.Valid("custom-fromfile", "Same"));

        var first = await _repository.ImportAsync(file, DefaultName, Token);
        var second = await _repository.ImportAsync(file, DefaultName, Token);

        Assert.NotEqual(first.Level!.Id, second.Level!.Id);
        Assert.Equal(2, _repository.Custom.Count);
    }

    [Fact]
    public async Task Import_StandardIdInFile_GetsANewId()
    {
        var file = LevelJson.Write(CustomLevelSamples.Valid("std-a", "Fake"));

        var result = await _repository.ImportAsync(file, DefaultName, Token);

        Assert.Equal(ImportOutcome.Imported, result.Outcome);
        Assert.NotEqual("std-a", result.Level!.Id);
        Assert.Null(_repository.Find("std-a")!.Name);
    }

    [Fact]
    public async Task Import_PaddedName_IsCleaned()
    {
        var file = LevelJson.Write(CustomLevelSamples.Valid("custom-x", "  Padded  "));

        var result = await _repository.ImportAsync(file, DefaultName, Token);

        Assert.Equal("Padded", result.Level!.Name);
    }

    [Fact]
    public async Task Import_MissingName_UsesTheNextDefaultName()
    {
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Mein Level 1"), Token);

        var result = await _repository.ImportAsync(LevelJson.Write(CustomLevelSamples.Valid("custom-x") with { Name = null }), DefaultName, Token);

        Assert.Equal("Mein Level 2", result.Level!.Name);
    }

    [Fact]
    public async Task Import_EmptyName_IsAnInvalidFile()
    {
        var file = LevelJson.Write(CustomLevelSamples.Valid("custom-x") with { Name = "x" }).Replace("\"name\": \"x\"", "\"name\": \"\"");

        var result = await _repository.ImportAsync(file, DefaultName, Token);

        Assert.Equal(ImportOutcome.InvalidFile, result.Outcome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"id\":\"x\"}")]
    public async Task Import_NotALevel_IsInvalidFile(string? json)
    {
        var result = await _repository.ImportAsync(json, DefaultName, Token);

        Assert.Equal(ImportOutcome.InvalidFile, result.Outcome);
        Assert.Null(result.Level);
        Assert.Empty(_repository.Custom);
        Assert.Empty(Stored(_storage));
    }

    [Fact]
    public async Task Import_TooLargeFile_IsInvalidFile()
    {
        var huge = LevelJson.Write(CustomLevelSamples.Valid("custom-x", new string('x', LevelJson.MaxJsonLength)));

        Assert.Equal(ImportOutcome.InvalidFile, (await _repository.ImportAsync(huge, DefaultName, Token)).Outcome);
    }

    [Fact]
    public async Task Import_TooManyStrawberries_IsInvalidFile()
    {
        var file = LevelJson.Write(CustomLevelSamples.TooManyStrawberries("custom-x"));

        Assert.Equal(ImportOutcome.InvalidFile, (await _repository.ImportAsync(file, DefaultName, Token)).Outcome);
    }

    [Fact]
    public async Task Import_UnsolvableLevel_IsNotSavable()
    {
        var result = await _repository.ImportAsync(LevelJson.Write(CustomLevelSamples.Unsolvable("custom-x")), DefaultName, Token);

        Assert.Equal(ImportOutcome.NotSavable, result.Outcome);
        Assert.Empty(_repository.Custom);
    }

    [Fact]
    public async Task Import_WhenFull_IsLimitReached()
    {
        for (var i = 1; i <= LevelLimits.MaxCustomLevels; i++)
        {
            await _repository.SaveAsync(CustomLevelSamples.Valid($"custom-{i}"), Token);
        }

        var result = await _repository.ImportAsync(LevelJson.Write(CustomLevelSamples.Valid("custom-x")), DefaultName, Token);

        Assert.Equal(ImportOutcome.LimitReached, result.Outcome);
        Assert.Equal(LevelLimits.MaxCustomLevels, _repository.Custom.Count);
    }

    [Fact]
    public async Task Import_StorageFails_ReportsItAndAddsNothing()
    {
        _storage.FailWrites = true;

        var result = await _repository.ImportAsync(LevelJson.Write(CustomLevelSamples.Valid("custom-x")), DefaultName, Token);

        Assert.Equal(ImportOutcome.StorageFailed, result.Outcome);
        Assert.Null(result.Level);
        Assert.Empty(_repository.Custom);
    }

    [Fact]
    public async Task NextDefaultName_SkipsTakenNames()
    {
        Assert.Equal("Mein Level 1", _repository.NextDefaultName(DefaultName));
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a", "Mein Level 1"), Token);
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-b", "Mein Level 3"), Token);

        Assert.Equal("Mein Level 2", _repository.NextDefaultName(DefaultName));
    }

    [Fact]
    public async Task Changed_IsRaisedOnlyAfterASuccessfulChange()
    {
        var changes = 0;
        _repository.Changed += () => changes++;

        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a"), Token);
        Assert.Equal(1, changes);

        await _repository.SaveAsync(CustomLevelSamples.Unsolvable("custom-b"), Token);
        await _repository.DeleteAsync("unknown", Token);
        await _repository.ImportAsync("garbage", DefaultName, Token);
        Assert.Equal(1, changes);

        await _repository.ImportAsync(LevelJson.Write(CustomLevelSamples.Valid("custom-x")), DefaultName, Token);
        Assert.Equal(2, changes);

        await _repository.DeleteAsync("custom-a", Token);
        Assert.Equal(3, changes);

        _storage.FailWrites = true;
        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-c"), Token);
        Assert.Equal(3, changes);
    }

    [Fact]
    public async Task All_ListHandedOutEarlier_StaysUnchanged()
    {
        var before = _repository.All;

        await _repository.SaveAsync(CustomLevelSamples.Valid("custom-a"), Token);

        Assert.Equal(2, before.Count);
        Assert.Equal(3, _repository.All.Count);
    }

    private sealed class ThrowingStore : ICustomLevelStore
    {
        public Task<IReadOnlyList<LevelDefinition>> LoadAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("boom");

        public Task<bool> SaveAllAsync(IReadOnlyList<LevelDefinition> levels, CancellationToken cancellationToken) => throw new InvalidOperationException("boom");
    }
}
