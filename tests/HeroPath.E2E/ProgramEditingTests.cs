using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class ProgramEditingTests(AppFixture app)
{
    [Fact]
    public async Task Click_AddsAndRemovesBlocks()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var desktop = await app.NewContextAsync();
        await RunAsync(await desktop.NewPageAsync(), "desktop", "program.editing.desktop");

        await using var laptop = await app.NewContextAsync("de-CH", 1366, 768);
        await RunAsync(await laptop.NewPageAsync(), "laptop", "program.editing.laptop");
    }

    private static async Task RunAsync(IPage page, string viewport, string namePrefix)
    {
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);

        await Expect(page.GetByTestId("program-empty")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(0);

        foreach (var direction in new[] { "up", "up", "right", "right" })
        {
            await page.GetByTestId($"palette-{direction}").ClickAsync();
        }

        await ExpectOrderAsync(page, ["up", "up", "right", "right"]);
        await Expect(page.GetByTestId("program-empty")).ToHaveCountAsync(0);

        if (viewport == "laptop")
        {
            await AssertNoPageScrollAsync(page);
        }

        await Screenshots.CaptureAsync(page, namePrefix + ".de");

        await page.GetByTestId("language-en").ClickAsync();
        await ExpectOrderAsync(page, ["up", "up", "right", "right"]);
        await Screenshots.CaptureAsync(page, namePrefix + ".en");

        await page.GetByTestId("program-remove-1").ClickAsync();
        await ExpectOrderAsync(page, ["up", "right", "right"]);

        await page.GetByTestId("program-clear").ClickAsync();
        await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("program-empty")).ToBeVisibleAsync();

        if (viewport == "laptop")
        {
            await AssertNoPageScrollAsync(page);
        }

        Assert.Empty(errors.Errors);
    }

    private static async Task ExpectOrderAsync(IPage page, string[] directions)
    {
        var blocks = page.GetByTestId("program-block");
        await Expect(blocks).ToHaveCountAsync(directions.Length);
        for (var index = 0; index < directions.Length; index++)
        {
            await Expect(blocks.Nth(index)).ToHaveAttributeAsync("data-index", index.ToString());
            await Expect(blocks.Nth(index)).ToHaveAttributeAsync("data-direction", directions[index]);
        }
    }

    private static async Task AssertNoPageScrollAsync(IPage page)
    {
        var scrolls = await page.EvaluateAsync<bool>(
            "document.documentElement.scrollHeight > window.innerHeight || document.documentElement.scrollWidth > window.innerWidth");
        Assert.False(scrolls, "Board, palette and program must fit a 1366x768 screen without page scrolling.");
    }
}
