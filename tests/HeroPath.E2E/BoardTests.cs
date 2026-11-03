using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class BoardTests(AppFixture app)
{
    [Fact]
    public async Task PlayPage_ShowsDemoLevel()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var desktop = await app.NewContextAsync();
        await CaptureAsync(await desktop.NewPageAsync(), "desktop", "board.start.desktop");

        await using var laptop = await app.NewContextAsync("de-CH", 1366, 768);
        await CaptureAsync(await laptop.NewPageAsync(), "laptop", "board.start.laptop");
    }

    private static async Task CaptureAsync(IPage page, string viewport, string namePrefix)
    {
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);

        await Expect(page.GetByTestId("game-board")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("side-panel")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("hero")).ToHaveAttributeAsync("data-x", "1");
        await Expect(page.GetByTestId("hero")).ToHaveAttributeAsync("data-y", "6");
        await Expect(page.GetByTestId("cell-1-6")).ToHaveAttributeAsync("data-kind", "start");
        await Expect(page.GetByTestId("cell-6-3")).ToHaveAttributeAsync("data-kind", "goal");
        await Expect(page.GetByTestId("cell-3-6")).ToHaveAttributeAsync("data-kind", "tree");
        await Expect(page.GetByTestId("cell-4-3")).ToHaveAttributeAsync("data-kind", "rock");
        await Expect(page.GetByTestId("cell-6-5")).ToHaveAttributeAsync("data-kind", "pond");
        await Expect(page.GetByTestId("level-name")).ToHaveTextAsync("Einfach 2");

        if (viewport == "laptop")
        {
            var scrolls = await page.EvaluateAsync<bool>(
                "document.documentElement.scrollHeight > window.innerHeight || document.documentElement.scrollWidth > window.innerWidth");
            Assert.False(scrolls, "The board must fit a 1366x768 screen without scrolling.");
        }

        await Screenshots.CaptureAsync(page, namePrefix + ".de");

        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("level-name")).ToHaveTextAsync("Easy 2");
        await Expect(page.GetByTestId("hero")).ToHaveAttributeAsync("data-x", "1");
        await Screenshots.CaptureAsync(page, namePrefix + ".en");

        Assert.Empty(errors.Errors);
    }
}
