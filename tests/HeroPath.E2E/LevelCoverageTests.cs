using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class LevelCoverageTests(AppFixture app)
{
    private static readonly string[] _levelOrder = ["free-field", "easy-1", "easy-2", "strawberries", "small-maze", "long-road", "castle-maze"];

    [Fact]
    public async Task SolveAllLevels_ShowsAllSolvedMessage_InGermanAndEnglish()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.StartGameAsync(errors, _levelOrder[0]);
        foreach (var id in _levelOrder)
        {
            if (id != _levelOrder[0])
            {
                await page.GetByTestId("level-picker").SelectOptionAsync(id);
                await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "ready");
            }

            await LevelSolutions.SolveAsync(page, id);
            await Expect(page.GetByTestId($"level-picker-option-{id}")).ToHaveAttributeAsync("data-solved", "true");
        }

        await Expect(page.GetByTestId("message")).ToContainTextAsync("Du hast alle Level geschafft!");
        await Expect(page.GetByTestId("next-level")).ToHaveCountAsync(0);
        await LayoutAssertions.AssertNoPageScrollAsync(page);
        await Screenshots.CaptureAsync(page, "all-solved.laptop.de");

        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("message")).ToContainTextAsync("You've solved all the levels!");
        await Screenshots.CaptureAsync(page, "all-solved.laptop.en");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task StartPage_English_ShowsLargePreviewsAndGoButton_OnLaptop()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("start-go")).ToHaveTextAsync("Let's go!");
        await Expect(page.GetByTestId("start-go")).ToBeInViewportAsync();

        var box = await page.GetByTestId("level-preview-castle-maze").GetByTestId("board-preview").BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box.Width >= 15 * 14, $"The castle maze preview is only {box.Width} px wide (cells below 14 px).");

        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > window.innerWidth"));
        await Screenshots.CaptureAsync(page, "start-page.laptop.en");

        Assert.Empty(errors.Errors);
    }

    [Theory]
    [InlineData("castle-maze", "laptop", 1366, 768)]
    [InlineData("castle-maze", "desktop", 1920, 1080)]
    [InlineData("long-road", "laptop", 1366, 768)]
    [InlineData("long-road", "desktop", 1920, 1080)]
    [InlineData("small-maze", "laptop", 1366, 768)]
    [InlineData("small-maze", "desktop", 1920, 1080)]
    public async Task PlayPage_LargeLevels_FitWithoutPageScroll_InBothLanguages(string levelId, string viewport, int width, int height)
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", width, height);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.StartGameAsync(errors, levelId);
        await LayoutAssertions.AssertNoPageScrollAsync(page);
        await Screenshots.CaptureAsync(page, $"play-{levelId}.{viewport}.de");

        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("control-play")).ToHaveTextAsync("Play");
        await LayoutAssertions.AssertNoPageScrollAsync(page);
        await Screenshots.CaptureAsync(page, $"play-{levelId}.{viewport}.en");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task PlayPage_WideBoard_UsesMoreWidth_AndKeepsProgramUsable_OnLaptop()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.StartGameAsync(errors, "long-road");
        var box = await page.GetByTestId("game-board").BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box.Width / 15 >= 48, $"The long road cells are only {box.Width / 15} px wide.");

        await LevelSolutions.SolveAsync(page, "long-road");
        await LayoutAssertions.AssertRepeatHeadersOnOneLineAsync(page);
        await LayoutAssertions.AssertNoPageScrollAsync(page);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task LevelPicker_LongestSolvedNames_AreNotClipped_OnLaptop()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.StartGameAsync(errors, "strawberries");
        await LevelSolutions.SolveAsync(page, "strawberries");

        foreach (var language in new[] { "de", "en" })
        {
            await page.GetByTestId($"language-{language}").ClickAsync();
            var clipped = await page.GetByTestId("level-picker").EvaluateAsync<bool>(@"select => {
                const style = getComputedStyle(select);
                const context = document.createElement('canvas').getContext('2d');
                context.font = style.font;
                const text = select.options[select.selectedIndex].text;
                const room = select.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight) - 24;
                return context.measureText(text).width > room;
            }");
            await Screenshots.CaptureAsync(page, $"level-picker-solved.laptop.{language}");
            Assert.False(clipped, $"The level picker clips its selected name in {language}.");
        }

        Assert.Empty(errors.Errors);
    }
}
