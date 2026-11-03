namespace HeroPath.E2E.Infrastructure;

/// <summary>Builds the known solutions of the standard levels with palette clicks (docs/plan.md, Level-Skizzen) and plays them.</summary>
public static class LevelSolutions
{
    private static readonly Dictionary<string, string[]> _simple = new()
    {
        ["free-field"] = ["right", "right", "right", "right", "right", "up", "up", "up", "up"],
        ["easy-1"] = ["right", "right", "up", "right", "right", "right", "down"],
        ["easy-2"] = ["up", "up", "right", "right", "right", "right", "right", "up"],
        ["strawberries"] = ["right", "right", "up", "up", "up", "right", "right", "right", "up", "up"],
        ["small-maze"] = ["right", "right", "right", "up", "up", "right", "right", "down", "right", "right"],
    };

    /// <summary>Fills the program of the loaded <paramref name="levelId"/> with its solution, plays it at fast tempo and waits for success.</summary>
    public static async Task SolveAsync(IPage page, string levelId)
    {
        if (_simple.TryGetValue(levelId, out var moves))
        {
            foreach (var move in moves)
            {
                await page.GetByTestId($"palette-{move}").ClickAsync();
            }
        }
        else if (levelId == "long-road")
        {
            await AddRepeatAsync(page, "1", "right", 14);
            await page.GetByTestId("palette-up").ClickAsync();
            await page.GetByTestId("palette-up").ClickAsync();
            await AddRepeatAsync(page, "4", "left", 14);
        }
        else if (levelId == "castle-maze")
        {
            await page.GetByTestId("palette-repeat").ClickAsync();
            await page.GetByTestId("program-select-1").ClickAsync();
            await page.GetByTestId("palette-repeat").ClickAsync();
            await FillInnerRepeatAsync(page, "1.1", "right");
            await page.GetByTestId("program-select-1").ClickAsync();
            await page.GetByTestId("palette-repeat").ClickAsync();
            await FillInnerRepeatAsync(page, "1.2", "up");
            await SetCountAsync(page, "1", 6);
        }
        else
        {
            throw new ArgumentException($"No known solution for '{levelId}'.", nameof(levelId));
        }

        await page.GetByTestId("control-tempo").FillAsync("2");
        await page.GetByTestId("control-play").ClickAsync();
        await Assertions.Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded", new() { Timeout = 30000 });
    }

    private static async Task AddRepeatAsync(IPage page, string path, string move, int count)
    {
        await page.GetByTestId("palette-repeat").ClickAsync();
        await page.GetByTestId($"program-select-{path}").ClickAsync();
        await page.GetByTestId($"palette-{move}").ClickAsync();
        await SetCountAsync(page, path, count);
        await page.GetByTestId($"program-select-{path}").ClickAsync();
    }

    private static async Task FillInnerRepeatAsync(IPage page, string path, string move)
    {
        await page.GetByTestId($"program-select-{path}").ClickAsync();
        await page.GetByTestId($"palette-{move}").ClickAsync();
        await SetCountAsync(page, path, 2);
    }

    private static async Task SetCountAsync(IPage page, string path, int count)
    {
        var presses = Math.Abs(count - 3);
        var button = page.GetByTestId(count > 3 ? $"repeat-plus-{path}" : $"repeat-minus-{path}");
        for (var i = 0; i < presses; i++)
        {
            await button.ClickAsync();
        }

        await Assertions.Expect(page.GetByTestId($"repeat-count-{path}")).ToHaveTextAsync(count.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
