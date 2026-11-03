using System.Diagnostics;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;
using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Modules.Levels.Tests.Application;

public class LevelPerformanceTests
{
    private const int TargetMilliseconds = 200;

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    /// <summary>20x20, ten strawberries spread over the board, all needed: the largest state space the editor allows.</summary>
    internal static LevelDefinition WorstCase(string id = "worst") => TenStrawberries(id, new Position(19, 0));

    private static LevelDefinition TenStrawberries(string id, Position goal) => new()
    {
        Id = id,
        Name = "Worst case",
        Width = 20,
        Height = 20,
        Start = new Position(0, 19),
        Goal = goal,
        RequireAllItems = true,
        Items = [.. Enumerable.Range(0, LevelLimits.MaxStrawberries).Select(i => new LevelItem(1 + i * 2, 1 + i * 7 % 18, ItemKind.Strawberry))],
    };

    [Fact]
    public void Evaluate_WorstCase20x20_StaysUnderTarget()
    {
        var level = WorstCase();
        LevelCheck.Evaluate(level);

        var median = Median(5, () => LevelCheck.Evaluate(level));

        Xunit.TestContext.Current.TestOutputHelper!.WriteLine($"PERF LevelCheck.Evaluate worst case: median {median:F2} ms (target {TargetMilliseconds} ms)");
        Assert.True(median < TargetMilliseconds, $"Median {median:F2} ms.");
    }

    [Fact]
    public void Evaluate_GoalNextToStart_StaysUnderTarget()
    {
        var level = TenStrawberries("tour", new Position(1, 19));
        LevelCheck.Evaluate(level);

        var median = Median(5, () => LevelCheck.Evaluate(level));

        Xunit.TestContext.Current.TestOutputHelper!.WriteLine($"PERF LevelCheck.Evaluate tour (goal next to start): median {median:F2} ms (target {TargetMilliseconds} ms)");
        Assert.True(median < TargetMilliseconds, $"Median {median:F2} ms.");
    }

    [Fact]
    public async Task Load_TwentyWorstCaseLevels_StaysUnderTarget()
    {
        var storage = new InMemoryAppStorage();
        storage.Values[StorageKeys.CustomLevels] = LevelJson.WriteAll([.. Enumerable.Range(0, LevelLimits.MaxCustomLevels).Select(i => WorstCase($"worst-{i}"))]);
        var store = new StorageCustomLevelStore(storage);

        var stopwatch = Stopwatch.StartNew();
        var levels = await store.LoadAsync(Token);
        stopwatch.Stop();

        Xunit.TestContext.Current.TestOutputHelper!.WriteLine($"PERF Load of 20 worst-case levels: {stopwatch.Elapsed.TotalMilliseconds:F1} ms");
        Assert.Equal(LevelLimits.MaxCustomLevels, levels.Count);
        Assert.True(stopwatch.Elapsed.TotalMilliseconds < TargetMilliseconds, $"Load took {stopwatch.Elapsed.TotalMilliseconds:F1} ms.");
    }

    private static double Median(int runs, Action action)
    {
        var times = new List<double>();
        for (var i = 0; i < runs; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            times.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        times.Sort();
        return times[runs / 2];
    }
}
