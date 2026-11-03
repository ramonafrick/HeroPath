using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class LevelSwitchTests(AppFixture app)
{
    private static readonly string[] _easy1Solution = ["right", "right", "up", "right", "right", "right", "down"];

    [Fact]
    public async Task SolveLevel_NextLevel_PickerSwitch_AndSolvedBadge()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.StartGameAsync(errors, "easy-1");
        await Expect(page.GetByTestId("level-picker")).ToHaveValueAsync("easy-1");
        await Expect(page.GetByTestId("next-level")).ToHaveCountAsync(0);

        foreach (var direction in _easy1Solution)
        {
            await page.GetByTestId($"palette-{direction}").ClickAsync();
        }

        await page.GetByTestId("control-tempo").FillAsync("2");
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded");
        await Expect(page.GetByTestId("next-level")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("level-picker-option-easy-1")).ToHaveAttributeAsync("data-solved", "true");
        await Screenshots.CaptureAsync(page, "play-success-next.laptop.de");
        await AssertNoPageScrollAsync(page);

        await page.GetByTestId("next-level").ClickAsync();
        await Expect(page.GetByTestId("level-name")).ToHaveTextAsync("Einfach 2");
        await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("next-level")).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "ready");

        await page.GetByTestId("palette-up").ClickAsync();
        await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(1);
        await page.GetByTestId("level-picker").SelectOptionAsync("small-maze");
        await Expect(page.GetByTestId("level-name")).ToHaveTextAsync("Kleines Labyrinth");
        await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(0);
        await AssertNoPageScrollAsync(page);
        await Screenshots.CaptureAsync(page, "level-picker.laptop.de");

        await page.GetByTestId("hero-change").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("level-card-easy-1")).ToHaveAttributeAsync("data-solved", "true");
        await Expect(page.GetByTestId("level-solved-easy-1")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("level-card-easy-2")).ToHaveAttributeAsync("data-solved", "false");
        await Screenshots.CaptureAsync(page, "start-page-solved.laptop.de");

        Assert.Empty(errors.Errors);
    }

    private static async Task AssertNoPageScrollAsync(IPage page)
    {
        var scrolls = await page.EvaluateAsync<bool>(
            "document.documentElement.scrollHeight > window.innerHeight || document.documentElement.scrollWidth > window.innerWidth");
        Assert.False(scrolls, "The play page must fit a 1366x768 screen without page scrolling.");
    }
}
