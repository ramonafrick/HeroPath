using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class RepeatEditingTests(AppFixture app)
{
    [Fact]
    public async Task ClickOnly_BuildsNestedProgram()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var desktop = await app.NewContextAsync();
        await RunAsync(await desktop.NewPageAsync(), "desktop");

        await using var laptop = await app.NewContextAsync("de-CH", 1366, 768);
        await RunAsync(await laptop.NewPageAsync(), "laptop");
    }

    [Fact]
    public async Task TenBlocks_ShowAboutEightRowsWithoutPageScroll()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var laptop = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await laptop.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
        for (var index = 0; index < 10; index++)
        {
            await page.GetByTestId(index % 2 == 0 ? "palette-up" : "palette-right").ClickAsync();
        }

        await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(10);
        await AssertNoPageScrollAsync(page);
        await Screenshots.CaptureAsync(page, "program.ten-blocks.laptop.de");
        var visibleRows = await page.EvaluateAsync<int>(@"() => {
            const list = document.querySelector('[data-testid=program]').getBoundingClientRect();
            return [...document.querySelectorAll('[data-testid=program-block]')]
                .filter(b => { const r = b.getBoundingClientRect(); return r.top >= list.top - 1 && r.bottom <= list.bottom + 1; }).length;
        }");
        Assert.True(visibleRows >= 8, $"Expected about 8 visible program rows on 1366x768, found {visibleRows}.");

        Assert.Empty(errors.Errors);
    }

    private static async Task RunAsync(IPage page, string viewport)
    {
        var errors = BrowserErrors.Track(page);
        var blocks = page.GetByTestId("program-block");

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
        await ExpectInsertTargetAsync(page, "root");
        await Expect(page.GetByTestId("palette-repeat")).ToBeEnabledAsync();

        await page.GetByTestId("palette-right").ClickAsync();
        await page.GetByTestId("palette-repeat").ClickAsync();
        await ExpectPathsAsync(page, ["1", "2"]);
        await Expect(page.GetByTestId("repeat-count-2")).ToHaveTextAsync("3");
        await Expect(page.GetByTestId("program-up")).ToHaveCountAsync(0);

        await page.GetByTestId("program-select-2").ClickAsync();
        await Expect(blocks.Nth(1)).ToHaveAttributeAsync("data-selected", "true");
        await Expect(page.GetByTestId("program-select-2")).ToHaveAttributeAsync("aria-pressed", "true");
        await ExpectInsertTargetAsync(page, "2");
        await Expect(page.GetByTestId("program-up")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("program-down")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("program-remove")).ToBeVisibleAsync();
        await LayoutAssertions.AssertRepeatHeadersOnOneLineAsync(page);

        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("palette-up").ClickAsync();
        await ExpectPathsAsync(page, ["1", "2", "2.1", "2.2"]);
        await Expect(page.GetByTestId("repeat-body-2").Locator("[data-testid=program-block]")).ToHaveCountAsync(2);

        await page.GetByTestId("repeat-plus-2").ClickAsync();
        await Expect(page.GetByTestId("repeat-count-2")).ToHaveTextAsync("4");
        await page.GetByTestId("repeat-minus-2").ClickAsync();
        await page.GetByTestId("repeat-minus-2").ClickAsync();
        await Expect(page.GetByTestId("repeat-count-2")).ToHaveTextAsync("2");

        await page.GetByTestId("palette-repeat").ClickAsync();
        await ExpectPathsAsync(page, ["1", "2", "2.1", "2.2", "2.3"]);
        await Expect(blocks.Nth(4)).ToHaveAttributeAsync("data-kind", "repeat");

        await page.GetByTestId("program-select-2.3").ClickAsync();
        await ExpectInsertTargetAsync(page, "2.3");
        await Expect(page.GetByTestId("palette-repeat")).ToBeDisabledAsync();
        await Expect(page.GetByTestId("palette-down")).ToBeEnabledAsync();

        await page.GetByTestId("palette-down").ClickAsync();
        await ExpectPathsAsync(page, ["1", "2", "2.1", "2.2", "2.3", "2.3.1"]);
        await Expect(page.GetByTestId("palette-repeat")).ToBeDisabledAsync();
        await AssertFitsAsync(page, viewport);
        await CaptureBothLanguagesAsync(page, $"program.repeat.{viewport}");

        await page.GetByTestId("program-select-2.3.1").ClickAsync();
        await ExpectInsertTargetAsync(page, "2.3");
        await page.GetByTestId("program-up").ClickAsync();
        await ExpectPathsAsync(page, ["1", "2", "2.1", "2.2", "2.3", "2.4"]);
        await Expect(blocks.Nth(4)).ToHaveAttributeAsync("data-kind", "move");
        await Expect(blocks.Nth(4)).ToHaveAttributeAsync("data-direction", "down");
        await Expect(blocks.Nth(4)).ToHaveAttributeAsync("data-selected", "true");
        await Expect(blocks.Nth(5)).ToHaveAttributeAsync("data-kind", "repeat");
        await Expect(page.GetByTestId("palette-repeat")).ToBeEnabledAsync();

        await page.GetByTestId("program-remove").ClickAsync();
        await ExpectPathsAsync(page, ["1", "2", "2.1", "2.2", "2.3"]);
        await Expect(page.Locator("[data-testid=program-block][data-selected=true]")).ToHaveCountAsync(0);
        await ExpectInsertTargetAsync(page, "root");
        await Expect(page.GetByTestId("program-remove")).ToHaveCountAsync(0);

        await page.GetByTestId("program-select-2").ClickAsync();
        await page.GetByTestId("program-remove").ClickAsync();
        await ExpectPathsAsync(page, ["1"]);
        await AssertFitsAsync(page, viewport);

        Assert.Empty(errors.Errors);
    }

    private static async Task ExpectPathsAsync(IPage page, string[] paths)
    {
        var blocks = page.GetByTestId("program-block");
        await Expect(blocks).ToHaveCountAsync(paths.Length);
        for (var index = 0; index < paths.Length; index++)
        {
            await Expect(blocks.Nth(index)).ToHaveAttributeAsync("data-path", paths[index]);
        }
    }

    private static async Task ExpectInsertTargetAsync(IPage page, string target) =>
        await Expect(page.GetByTestId("insert-target")).ToHaveAttributeAsync("data-target", target);

    private static async Task CaptureBothLanguagesAsync(IPage page, string namePrefix)
    {
        await LayoutAssertions.AssertRepeatHeadersOnOneLineAsync(page);
        await Screenshots.CaptureAsync(page, namePrefix + ".de");
        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("language-en")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByTestId("palette-repeat")).ToContainTextAsync("Repeat");
        await LayoutAssertions.AssertRepeatHeadersOnOneLineAsync(page);
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

        if (viewport == "laptop")
        {
            await AssertNoPageScrollAsync(page);
        }
    }

    private static async Task AssertNoPageScrollAsync(IPage page)
    {
        var scrolls = await page.EvaluateAsync<bool>(
            "document.documentElement.scrollHeight > window.innerHeight || document.documentElement.scrollWidth > window.innerWidth");
        Assert.False(scrolls, "Board, palette and program must fit a 1366x768 screen without page scrolling.");
    }
}
