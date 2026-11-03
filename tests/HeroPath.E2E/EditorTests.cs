using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class EditorTests(AppFixture app)
{
    private const string LevelName = "Mein Burgweg";

    private static async Task OpenEditorAsync(IPage page, BrowserErrors errors)
    {
        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.GetByTestId("start-build").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=editor-page]", errors);
        await Expect(page.GetByTestId("edit-cell-0-0")).ToBeVisibleAsync();
    }

    private static async Task ClickCellsAsync(IPage page, string tool, params (int X, int Y)[] cells)
    {
        await page.GetByTestId($"editor-tool-{tool}").ClickAsync();
        foreach (var (x, y) in cells)
        {
            await page.GetByTestId($"edit-cell-{x}-{y}").ClickAsync();
        }
    }

    private static async Task ShrinkToFiveByFiveAsync(IPage page)
    {
        for (var i = 0; i < 5; i++)
        {
            await page.GetByTestId("editor-width-minus").ClickAsync();
            await page.GetByTestId("editor-height-minus").ClickAsync();
        }
    }

    private static async Task SwitchToEnglishAsync(IPage page)
    {
        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("editor-save")).ToHaveTextAsync("Save");
    }

    [Fact]
    public async Task BuildSaveAndPlay_ALevelFromTheStartPageToTheCheckMark()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await OpenEditorAsync(page, errors);

        await Expect(page.GetByTestId("editor-name")).ToHaveValueAsync("Mein Level 1");
        await Expect(page.GetByTestId("editor-width")).ToHaveTextAsync("10");
        await Expect(page.GetByTestId("editor-status")).ToHaveAttributeAsync("data-state", "solvable");
        await Expect(page.GetByTestId("editor-status")).ToHaveAttributeAsync("data-min-steps", "18");
        await Expect(page.GetByTestId("editor-tool-tree")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByTestId("editor-name-hint")).ToHaveTextAsync("Bitte verwende nicht deinen echten Namen.");
        await Expect(page.GetByTestId("editor-require-all")).ToBeDisabledAsync();
        await Expect(page.GetByTestId("custom-level-list")).ToBeVisibleAsync();
        await LayoutAssertions.AssertNoPageScrollAsync(page);
        await AssertTargetsAreBigEnoughAsync(page);
        await Screenshots.CaptureAsync(page, "editor-new.laptop.de");

        await SwitchToEnglishAsync(page);
        await Expect(page.GetByTestId("editor-name-hint")).ToHaveTextAsync("Please do not use your real name.");
        await Screenshots.CaptureAsync(page, "editor-new.laptop.en");
        await page.GetByTestId("language-de").ClickAsync();
        await Expect(page.GetByTestId("editor-save")).ToHaveTextAsync("Speichern");

        await ShrinkToFiveByFiveAsync(page);
        await Expect(page.GetByTestId("editor-width")).ToHaveTextAsync("5");
        await Expect(page.GetByTestId("editor-height")).ToHaveTextAsync("5");
        await Expect(page.GetByTestId("editor-status")).ToHaveAttributeAsync("data-min-steps", "8");
        await Expect(page.GetByTestId("edit-cell-0-4")).ToHaveAttributeAsync("data-kind", "start");
        await Expect(page.GetByTestId("edit-cell-4-0")).ToHaveAttributeAsync("data-kind", "goal");

        await ClickCellsAsync(page, "tree", (1, 4));
        await ClickCellsAsync(page, "rock", (2, 2));
        await ClickCellsAsync(page, "pond", (3, 1));
        await ClickCellsAsync(page, "strawberry", (1, 3));
        await ClickCellsAsync(page, "pizza", (3, 3));
        await page.GetByTestId("editor-require-all").CheckAsync();
        await page.GetByTestId("editor-name").FillAsync(LevelName);

        await Expect(page.GetByTestId("edit-cell-1-4")).ToHaveAttributeAsync("data-kind", "tree");
        await Expect(page.GetByTestId("edit-cell-1-3")).ToHaveAttributeAsync("data-item", "strawberry");
        await Expect(page.GetByTestId("edit-cell-3-3")).ToHaveAttributeAsync("data-item", "pizza");
        await Expect(page.GetByTestId("editor-status")).ToHaveAttributeAsync("data-state", "solvable");
        await Expect(page.GetByTestId("editor-status")).ToHaveAttributeAsync("data-min-steps", "8");
        await Expect(page.GetByTestId("editor-save")).ToBeEnabledAsync();

        await page.GetByTestId("editor-save").ClickAsync();
        await Expect(page.GetByTestId("editor-saved")).ToHaveAttributeAsync("data-outcome", "Saved");
        await Expect(page.GetByTestId("custom-level-list").GetByRole(AriaRole.Button).Filter(new() { HasText = LevelName })).ToBeVisibleAsync();
        await Screenshots.CaptureAsync(page, "editor-built.laptop.de");

        await SwitchToEnglishAsync(page);
        await Expect(page.GetByTestId("editor-saved")).ToContainTextAsync("Saved");
        await Screenshots.CaptureAsync(page, "editor-built.laptop.en");
        await page.GetByTestId("language-de").ClickAsync();

        await page.GetByTestId("editor-back").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        var section = page.GetByTestId("custom-levels");
        var card = section.Locator("[data-testid^=level-card-]");
        await Expect(card).ToHaveCountAsync(1);
        await Expect(card).ToHaveTextAsync(LevelName);

        await card.ClickAsync();
        await page.GetByTestId("start-go").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
        await Expect(page.GetByTestId("level-name")).ToHaveTextAsync(LevelName);
        await Expect(page.GetByTestId("game-board").GetByTestId("cell-1-4")).ToHaveAttributeAsync("data-kind", "tree");

        foreach (var block in new[] { "up", "right", "right", "right", "right", "up", "up", "up" })
        {
            await page.GetByTestId($"palette-{block}").ClickAsync();
        }

        await page.GetByTestId("control-tempo").FillAsync("2");
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded");
        await Expect(page.GetByTestId("level-picker").Locator("option[data-solved=true]")).ToHaveCountAsync(1);

        await page.GetByTestId("hero-change").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.Locator("[data-testid^=level-solved-custom-]")).ToHaveCountAsync(1);

        await page.ReloadAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("custom-levels").Locator("[data-testid^=level-card-]")).ToHaveTextAsync(LevelName);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task Editor_OnDesktop_FitsAndKeepsTheBoardReadable()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await OpenEditorAsync(page, errors);

        await LayoutAssertions.AssertNoPageScrollAsync(page);
        var cell = await page.GetByTestId("edit-cell-0-0").BoundingBoxAsync();
        Assert.NotNull(cell);
        Assert.InRange(cell.Width, 40, 44.5);
        await Screenshots.CaptureAsync(page, "editor-new.desktop.de");
        await SwitchToEnglishAsync(page);
        await Screenshots.CaptureAsync(page, "editor-new.desktop.en");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task Editor_RefusesAndExplains_WalledGoal_Shrinking_AndTooManyStrawberries()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenEditorAsync(page, errors);

        await ClickCellsAsync(page, "rock", (8, 0), (9, 1));

        var status = page.GetByTestId("editor-status");
        await Expect(status).ToHaveAttributeAsync("data-state", "unsolvable");
        await Expect(status).Not.ToHaveAttributeAsync("data-min-steps", new Regex(".*"));
        await Expect(status).ToContainTextAsync("Der Held kommt nicht zur Burg");
        await Expect(page.GetByTestId("editor-save")).ToBeDisabledAsync();
        await Expect(page.GetByTestId("editor-save-reason")).ToBeVisibleAsync();
        await Screenshots.CaptureAsync(page, "editor-unsolvable.laptop.de");
        await SwitchToEnglishAsync(page);
        await Expect(status).ToContainTextAsync("The hero cannot reach the castle");
        await Screenshots.CaptureAsync(page, "editor-unsolvable.laptop.en");
        await page.GetByTestId("language-de").ClickAsync();

        await page.GetByTestId("edit-cell-9-1").ClickAsync();
        await Expect(status).ToHaveAttributeAsync("data-state", "solvable");
        await page.GetByTestId("edit-cell-8-0").ClickAsync();
        await Expect(page.GetByTestId("editor-save")).ToBeEnabledAsync();

        await page.GetByTestId("editor-name").FillAsync(string.Empty);
        await Expect(status).ToHaveAttributeAsync("data-state", "invalid");
        await Expect(page.GetByTestId("editor-save")).ToBeDisabledAsync();
        await page.GetByTestId("editor-name").FillAsync("Test");
        await Expect(page.GetByTestId("editor-save")).ToBeEnabledAsync();

        await ClickCellsAsync(page, "tree", (9, 5), (9, 6), (8, 5));
        await page.GetByTestId("editor-width-minus").ClickAsync();
        await Expect(page.GetByTestId("editor-width")).ToHaveTextAsync("9");
        await Expect(page.GetByTestId("editor-hint")).ToHaveTextAsync("2 Dinge wurden entfernt.");
        await Screenshots.CaptureAsync(page, "editor-resized.laptop.de");
        await SwitchToEnglishAsync(page);
        await Expect(page.GetByTestId("editor-hint")).ToHaveTextAsync("2 things were removed.");
        await Screenshots.CaptureAsync(page, "editor-resized.laptop.en");
        await page.GetByTestId("language-de").ClickAsync();

        await page.GetByTestId("editor-tool-strawberry").ClickAsync();
        for (var i = 0; i < 11; i++)
        {
            await page.GetByTestId($"edit-cell-{i % 8}-{1 + i / 8}").ClickAsync();
        }

        await Expect(page.GetByTestId("editor-hint")).ToHaveTextAsync("Es passen höchstens 10 Erdbeeren in ein Level.");
        await Expect(page.Locator("[data-testid^=edit-cell-][data-item=strawberry]")).ToHaveCountAsync(10);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task Editor_TwentyByTwenty_WithTenStrawberries_ChecksFastAndStaysClickable()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenEditorAsync(page, errors);

        for (var i = 0; i < 10; i++)
        {
            await page.GetByTestId("editor-width-plus").ClickAsync();
            await page.GetByTestId("editor-height-plus").ClickAsync();
        }

        await Expect(page.GetByTestId("editor-width")).ToHaveTextAsync("20");
        await Expect(page.GetByTestId("editor-height")).ToHaveTextAsync("20");
        await Expect(page.GetByTestId("editor-width-plus")).ToBeDisabledAsync();
        await LayoutAssertions.AssertNoPageScrollAsync(page);

        await ClickCellsAsync(page, "strawberry", (1, 9), (2, 9), (3, 9), (4, 9), (1, 8), (2, 8), (3, 8), (4, 8), (1, 7));
        await page.GetByTestId("editor-require-all").CheckAsync();
        var status = page.GetByTestId("editor-status");
        await Expect(status).ToHaveAttributeAsync("data-state", "solvable");
        var before = await status.GetAttributeAsync("data-min-steps");
        Assert.NotNull(before);

        var stopwatch = Stopwatch.StartNew();
        await page.GetByTestId("edit-cell-19-19").ClickAsync();
        await Expect(status).Not.ToHaveAttributeAsync("data-min-steps", before, new() { Timeout = 10_000 });
        stopwatch.Stop();

        var after = await status.GetAttributeAsync("data-min-steps");
        var report = string.Create(
            CultureInfo.InvariantCulture,
            $"20x20 with 10 strawberries and all required: from the 10th strawberry click until the status changed from {before} to {after} minimum steps took {stopwatch.ElapsedMilliseconds} ms (includes the Playwright click).");
        Console.WriteLine(report);
        Directory.CreateDirectory(E2EEnvironment.ScreenshotDirectory);
        await File.WriteAllTextAsync(Path.Combine(E2EEnvironment.ScreenshotDirectory, "editor-20x20.timing.txt"), report, Xunit.TestContext.Current.CancellationToken);
        Xunit.TestContext.Current.TestOutputHelper?.WriteLine(report);
        Assert.InRange(stopwatch.ElapsedMilliseconds, 0, 1000);

        await Expect(page.GetByTestId("edit-cell-19-19")).ToHaveAttributeAsync("data-item", "strawberry");
        var cell = await page.GetByTestId("edit-cell-10-10").BoundingBoxAsync();
        Assert.NotNull(cell);
        Assert.True(cell.Width >= 28, $"A cell of the 20x20 board must stay clickable, but is {cell.Width} px wide.");
        await Screenshots.CaptureAsync(page, "editor-20x20.laptop.de");
        await SwitchToEnglishAsync(page);
        await Screenshots.CaptureAsync(page, "editor-20x20.laptop.en");

        Assert.Empty(errors.Errors);
    }

    private static async Task AssertTargetsAreBigEnoughAsync(IPage page)
    {
        var small = await page.EvaluateAsync<string[]>(@"() => [...document.querySelectorAll('[data-testid=level-editor] button, [data-testid=level-editor] input[type=text], [data-testid=editor-back]')]
            .filter(e => !e.dataset.testid?.startsWith('edit-cell-'))
            .filter(e => { const r = e.getBoundingClientRect(); return r.width < 43.5 || r.height < 43.5; })
            .map(e => `${e.dataset.testid}: ${e.getBoundingClientRect().width}x${e.getBoundingClientRect().height}`)");
        Assert.Empty(small);
    }
}
