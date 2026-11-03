using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Modules.Levels.Tests.Infrastructure;

public class StorageCustomLevelStoreTests
{
    private readonly InMemoryAppStorage _storage = new();
    private readonly StorageCustomLevelStore _store;

    public StorageCustomLevelStoreTests()
    {
        _store = new StorageCustomLevelStore(_storage);
    }

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private async Task<IReadOnlyList<LevelDefinition>> LoadAsync(string? stored)
    {
        if (stored is not null)
        {
            _storage.Values[StorageKeys.CustomLevels] = stored;
        }

        return await _store.LoadAsync(Token);
    }

    private static string Array(params string[] elements) => "[" + string.Join(",", elements) + "]";

    private static string Json(LevelDefinition level) => LevelJson.Write(level);

    [Fact]
    public async Task Load_NothingStored_ReturnsEmpty()
    {
        Assert.Empty(await LoadAsync(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[]")]
    [InlineData("garbage{")]
    [InlineData("{\"id\":\"custom-a\"}")]
    [InlineData("\"text\"")]
    [InlineData("42")]
    [InlineData("null")]
    public async Task Load_EmptyOrNotAnArray_ReturnsEmpty(string stored)
    {
        Assert.Empty(await LoadAsync(stored));
    }

    [Fact]
    public async Task Load_ValidLevels_KeepOrderAndContent()
    {
        var first = CustomLevelSamples.Valid("custom-a", "Erstes");
        var second = CustomLevelSamples.Valid("custom-b", "Zweites") with { RequireAllItems = true };

        var levels = await LoadAsync(Array(Json(first), Json(second)));

        Assert.Equal([first, second], levels, LevelComparer.Instance);
    }

    [Fact]
    public async Task Load_MixedWithBrokenEntries_KeepsOnlyTheUsableOnes()
    {
        var stored = Array(
            Json(CustomLevelSamples.Valid("custom-a")),
            "42",
            "{}",
            "\"x\"",
            "null",
            "[]",
            Json(CustomLevelSamples.Unsolvable("custom-unsolvable")),
            Json(CustomLevelSamples.TooManyStrawberries("custom-many")),
            Json(CustomLevelSamples.Valid("custom-nameless") with { Name = null }),
            Json(CustomLevelSamples.Valid("custom-b")));

        var levels = await LoadAsync(stored);

        Assert.Equal(["custom-a", "custom-b"], levels.Select(l => l.Id));
    }

    [Theory]
    [InlineData("\"schemaVersion\": 1", "\"schemaVersion\": 2")]
    [InlineData("\"width\": 5", "\"width\": 99")]
    [InlineData("\"width\": 5", "\"width\": -1")]
    [InlineData("\"kind\": \"tree\"", "\"kind\": 0")]
    [InlineData("\"kind\": \"tree\"", "\"kind\": \"lava\"")]
    [InlineData("\"id\": \"custom-a\"", "\"id\": \"../Evil Id\"")]
    [InlineData("\"name\": \"Level custom-a\"", "\"name\": \"   \"")]
    public async Task Load_ManipulatedEntry_IsSkipped(string find, string replace)
    {
        var original = Json(CustomLevelSamples.Valid("custom-a"));
        Assert.Contains(find, original);

        var levels = await LoadAsync(Array(original.Replace(find, replace), Json(CustomLevelSamples.Valid("custom-b"))));

        Assert.Equal(["custom-b"], levels.Select(l => l.Id));
    }

    [Fact]
    public async Task Load_OversizedEntry_IsSkipped()
    {
        var huge = CustomLevelSamples.Valid("custom-huge", new string('x', LevelJson.MaxJsonLength));

        var levels = await LoadAsync(Array(Json(huge), Json(CustomLevelSamples.Valid("custom-b"))));

        Assert.Equal(["custom-b"], levels.Select(l => l.Id));
    }

    [Fact]
    public async Task Load_WholeValueTooLarge_ReturnsEmpty()
    {
        var padding = new string(' ', (LevelLimits.MaxCustomLevels + 1) * LevelJson.MaxJsonLength);

        Assert.Empty(await LoadAsync(Array(Json(CustomLevelSamples.Valid("custom-a"))) + padding));
    }

    [Fact]
    public async Task Load_TooDeeplyNested_ReturnsEmptyWithoutThrowing()
    {
        var nested = new string('[', 200) + new string(']', 200);

        Assert.Empty(await LoadAsync(nested));
    }

    [Fact]
    public async Task Load_DuplicateIds_KeepsTheFirst()
    {
        var levels = await LoadAsync(Array(
            Json(CustomLevelSamples.Valid("custom-a", "First")),
            Json(CustomLevelSamples.Valid("custom-a", "Second")),
            Json(CustomLevelSamples.Valid("custom-b"))));

        Assert.Equal(["custom-a", "custom-b"], levels.Select(l => l.Id));
        Assert.Equal("First", levels[0].Name);
    }

    [Fact]
    public async Task Load_MoreThanTheMaximum_TakesTheFirstTwenty()
    {
        var elements = Enumerable.Range(1, LevelLimits.MaxCustomLevels + 5).Select(i => Json(CustomLevelSamples.Valid($"custom-{i}")));

        var levels = await LoadAsync(Array([.. elements]));

        Assert.Equal(LevelLimits.MaxCustomLevels, levels.Count);
        Assert.Equal("custom-1", levels[0].Id);
        Assert.Equal($"custom-{LevelLimits.MaxCustomLevels}", levels[^1].Id);
    }

    [Fact]
    public async Task SaveAll_ThenLoad_RoundTrips()
    {
        LevelDefinition[] levels =
        [
            CustomLevelSamples.Valid("custom-a", "Umlaute äöü <b>fett</b>"),
            CustomLevelSamples.Valid("custom-b") with { RequireAllItems = true, Items = [new LevelItem(1, 4, ItemKind.Strawberry), new LevelItem(3, 3, ItemKind.Pizza)] },
        ];

        var saved = await _store.SaveAllAsync(levels, Token);
        var loaded = await _store.LoadAsync(Token);

        Assert.True(saved);
        Assert.Equal(levels, loaded, LevelComparer.Instance);
        Assert.DoesNotContain('\n', _storage.Values[StorageKeys.CustomLevels]);
    }

    [Fact]
    public async Task SaveAll_EmptyList_WritesAnEmptyArray()
    {
        Assert.True(await _store.SaveAllAsync([], Token));

        Assert.Equal("[]", _storage.Values[StorageKeys.CustomLevels]);
    }

    [Fact]
    public async Task SaveAll_StorageRefusesWrites_ReturnsFalseAndKeepsTheOldValue()
    {
        _storage.Values[StorageKeys.CustomLevels] = "[]";
        _storage.FailWrites = true;

        var saved = await _store.SaveAllAsync([CustomLevelSamples.Valid("custom-a")], Token);

        Assert.False(saved);
        Assert.Equal("[]", _storage.Values[StorageKeys.CustomLevels]);
    }
}
