using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class RunFlowTests(AppFixture app)
{
    private static readonly string[] _demoSolution = ["up", "up", "right", "right", "right", "right", "right", "up"];

    [Fact]
    public async Task Play_ReachesCastle()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await RunInBothViewportsAsync(async (page, viewport) =>
        {
            await AddBlocksAsync(page, _demoSolution);
            await SetFastTempoAsync(page);

            await page.GetByTestId("control-play").ClickAsync();

            await ExpectStatusAsync(page, "succeeded");
            await ExpectHeroAsync(page, 6, 3);
            await ExpectDisabledAsync(page, "control-play", true);
            await ExpectDisabledAsync(page, "control-debug", true);
            await ExpectDisabledAsync(page, "palette-up", true);
            await ExpectDisabledAsync(page, "program-clear", true);
            await AssertFitsAsync(page, viewport);
            await CaptureBothLanguagesAsync(page, $"run.success.{viewport}");
        });
    }

    [Fact]
    public async Task Debug_StepsAndContinuesWithPlay()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await RunInBothViewportsAsync(async (page, viewport) =>
        {
            await AddBlocksAsync(page, _demoSolution);
            await SetFastTempoAsync(page);

            await page.GetByTestId("control-debug").ClickAsync();
            await page.GetByTestId("control-debug").ClickAsync();

            await ExpectStatusAsync(page, "paused");
            await ExpectHeroAsync(page, 1, 4);
            await Expect(page.GetByTestId("program-block").Nth(1)).ToHaveAttributeAsync("data-current", "true");
            await Expect(page.Locator("[data-testid=program-block][data-current=true]")).ToHaveCountAsync(1);
            await ExpectDisabledAsync(page, "palette-up", true);
            await ExpectDisabledAsync(page, "control-play", false);
            await AssertFitsAsync(page, viewport);
            await CaptureBothLanguagesAsync(page, $"run.debug-paused.{viewport}");

            await page.GetByTestId("control-play").ClickAsync();

            await ExpectStatusAsync(page, "succeeded");
            await ExpectHeroAsync(page, 6, 3);
        });
    }

    [Fact]
    public async Task Play_HitsObstacle()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await RunInBothViewportsAsync(async (page, viewport) =>
        {
            await AddBlocksAsync(page, ["right", "right"]);
            await SetFastTempoAsync(page);

            await page.GetByTestId("control-play").ClickAsync();

            await ExpectStatusAsync(page, "failed");
            await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-error", "Obstacle");
            await ExpectHeroAsync(page, 2, 6);
            await Expect(page.GetByTestId("cell-3-6")).ToHaveAttributeAsync("data-marked", "true");
            await Expect(page.GetByTestId("program-block").Nth(1)).ToHaveAttributeAsync("data-error", "true");
            await ExpectDisabledAsync(page, "control-play", true);
            await ExpectDisabledAsync(page, "control-debug", true);
            await AssertFitsAsync(page, viewport);
            await CaptureBothLanguagesAsync(page, $"run.error-obstacle.{viewport}");
        });
    }

    [Fact]
    public async Task Play_LeavesBoard()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await RunInBothViewportsAsync(async (page, viewport) =>
        {
            await AddBlocksAsync(page, ["left", "left"]);
            await SetFastTempoAsync(page);

            await page.GetByTestId("control-play").ClickAsync();

            await ExpectStatusAsync(page, "failed");
            await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-error", "OutOfBounds");
            await ExpectHeroAsync(page, 0, 6);
            await Expect(page.GetByTestId("cell-0-6")).ToHaveAttributeAsync("data-marked", "true");
            await Expect(page.GetByTestId("program-block").Nth(1)).ToHaveAttributeAsync("data-error", "true");
            await AssertFitsAsync(page, viewport);
            await CaptureBothLanguagesAsync(page, $"run.error-edge.{viewport}");
        });
    }

    [Fact]
    public async Task Reset_RestoresStart_AndUnlocksEditor()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await RunInBothViewportsAsync(async (page, viewport) =>
        {
            await AddBlocksAsync(page, ["right", "right"]);
            await SetFastTempoAsync(page);
            await page.GetByTestId("control-play").ClickAsync();
            await ExpectStatusAsync(page, "failed");

            await page.GetByTestId("control-reset").ClickAsync();

            await ExpectStatusAsync(page, "ready");
            await ExpectHeroAsync(page, 1, 6);
            await Expect(page.Locator("[data-marked=true]")).ToHaveCountAsync(0);
            await Expect(page.Locator("[data-current=true]")).ToHaveCountAsync(0);
            await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(2);
            await ExpectDisabledAsync(page, "palette-up", false);
            await ExpectDisabledAsync(page, "control-play", false);
            await AssertFitsAsync(page, viewport);
            await CaptureBothLanguagesAsync(page, $"run.reset.{viewport}");

            await page.GetByTestId("program-remove-0").ClickAsync();
            await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(1);
            await page.GetByTestId("program-clear").ClickAsync();
            await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(0);
            await ExpectDisabledAsync(page, "control-play", true);
            await ExpectDisabledAsync(page, "control-debug", true);
            await ExpectDisabledAsync(page, "control-reset", false);
        });
    }

    [Fact]
    public async Task LanguageSwitch_DuringPause_KeepsProgramAndHero()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenAsync(page, errors);

        await AddBlocksAsync(page, _demoSolution);
        await page.GetByTestId("control-debug").ClickAsync();
        await page.GetByTestId("control-debug").ClickAsync();
        await ExpectStatusAsync(page, "paused");
        await Screenshots.CaptureAsync(page, "run.language-paused.desktop.de");

        await page.GetByTestId("language-en").ClickAsync();

        await Expect(page.GetByTestId("language-en")).ToHaveAttributeAsync("aria-pressed", "true");
        await ExpectStatusAsync(page, "paused");
        await ExpectHeroAsync(page, 1, 4);
        await ExpectOrderAsync(page, _demoSolution);
        await Expect(page.GetByTestId("program-block").Nth(1)).ToHaveAttributeAsync("data-current", "true");
        await Screenshots.CaptureAsync(page, "run.language-paused.desktop.en");

        Assert.Empty(errors.Errors);
    }

    private async Task RunInBothViewportsAsync(Func<IPage, string, Task> scenario)
    {
        await using var desktop = await app.NewContextAsync();
        await RunAsync(await desktop.NewPageAsync(), "desktop", scenario);

        await using var laptop = await app.NewContextAsync("de-CH", 1366, 768);
        await RunAsync(await laptop.NewPageAsync(), "laptop", scenario);
    }

    private static async Task RunAsync(IPage page, string viewport, Func<IPage, string, Task> scenario)
    {
        var errors = BrowserErrors.Track(page);
        await OpenAsync(page, errors);

        await scenario(page, viewport);

        Assert.Empty(errors.Errors);
    }

    private static async Task OpenAsync(IPage page, BrowserErrors errors)
    {
        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
    }

    private static async Task AddBlocksAsync(IPage page, string[] directions)
    {
        foreach (var direction in directions)
        {
            await page.GetByTestId($"palette-{direction}").ClickAsync();
        }

        await ExpectOrderAsync(page, directions);
    }

    private static async Task SetFastTempoAsync(IPage page)
    {
        await page.GetByTestId("control-tempo").FillAsync("2");
        await Expect(page.GetByTestId("control-tempo")).ToHaveValueAsync("2");
    }

    private static async Task ExpectStatusAsync(IPage page, string state) =>
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", state);

    private static async Task ExpectHeroAsync(IPage page, int x, int y)
    {
        await Expect(page.GetByTestId("hero")).ToHaveAttributeAsync("data-x", x.ToString());
        await Expect(page.GetByTestId("hero")).ToHaveAttributeAsync("data-y", y.ToString());
    }

    private static async Task ExpectDisabledAsync(IPage page, string testId, bool disabled)
    {
        if (disabled)
        {
            await Expect(page.GetByTestId(testId)).ToBeDisabledAsync();
        }
        else
        {
            await Expect(page.GetByTestId(testId)).ToBeEnabledAsync();
        }
    }

    private static async Task ExpectOrderAsync(IPage page, string[] directions)
    {
        var blocks = page.GetByTestId("program-block");
        await Expect(blocks).ToHaveCountAsync(directions.Length);
        for (var index = 0; index < directions.Length; index++)
        {
            await Expect(blocks.Nth(index)).ToHaveAttributeAsync("data-direction", directions[index]);
        }
    }

    private static async Task CaptureBothLanguagesAsync(IPage page, string namePrefix)
    {
        await Screenshots.CaptureAsync(page, namePrefix + ".de");
        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("language-en")).ToHaveAttributeAsync("aria-pressed", "true");
        await Screenshots.CaptureAsync(page, namePrefix + ".en");
        await page.GetByTestId("language-de").ClickAsync();
        await Expect(page.GetByTestId("language-de")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    private static async Task AssertFitsAsync(IPage page, string viewport)
    {
        var small = await page.EvaluateAsync<string[]>(@"() => [...document.querySelectorAll('[data-testid=side-panel] button')]
            .filter(b => { const r = b.getBoundingClientRect(); return r.height < 43.5 || r.width < 43.5; })
            .map(b => b.dataset.testid)");
        Assert.Empty(small);

        var clipped = await page.EvaluateAsync<string[]>(@"() => [...document.querySelectorAll('[data-testid=side-panel] button, [data-testid=program-block]')]
            .filter(e => e.scrollWidth > e.clientWidth + 1)
            .map(e => e.dataset.testid)");
        Assert.Empty(clipped);

        if (viewport != "laptop")
        {
            return;
        }

        var scrolls = await page.EvaluateAsync<bool>(
            "document.documentElement.scrollHeight > window.innerHeight || document.documentElement.scrollWidth > window.innerWidth");
        Assert.False(scrolls, "Board, palette, program and controls must fit a 1366x768 screen without page scrolling.");
    }
}
