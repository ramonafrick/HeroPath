using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class DragDropTests(AppFixture app)
{
    [Fact]
    public async Task PaletteToPositions_InsertsBetweenBlocksAndIntoRepeat()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenAsync(page, errors);

        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("palette-down").ClickAsync();
        await ExpectDirectionsAsync(page, ["up", "down"]);

        await BeginDragAsync(page, page.GetByTestId("palette-left"), Zone(page, "root:1"));
        await Expect(Zone(page, "root:1")).ToHaveAttributeAsync("data-valid", "true");
        await Expect(Zone(page, "root:1")).ToHaveAttributeAsync("data-hover", "true");
        await Screenshots.CaptureAsync(page, "drag.insert-line.desktop.de");
        await page.Mouse.UpAsync();
        await ExpectDirectionsAsync(page, ["up", "left", "down"]);
        await Expect(Zone(page, "root:0")).Not.ToHaveAttributeAsync("data-valid", "true");

        await DragAsync(page, page.GetByTestId("palette-repeat"), Zone(page, "root:3"));
        await ExpectPathsAsync(page, ["1", "2", "3", "4"]);
        await Expect(page.GetByTestId("program-block").Nth(3)).ToHaveAttributeAsync("data-kind", "repeat");

        await DragAsync(page, page.GetByTestId("palette-right"), Zone(page, "4:0"));
        await ExpectPathsAsync(page, ["1", "2", "3", "4", "4.1"]);
        await Expect(page.GetByTestId("repeat-body-4").Locator("[data-testid=program-block]")).ToHaveCountAsync(1);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task InsertLine_IsShownInEnglish()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("en");
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenAsync(page, errors);
        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("language-en")).ToHaveAttributeAsync("aria-pressed", "true");

        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("palette-down").ClickAsync();
        await BeginDragAsync(page, page.GetByTestId("palette-left"), Zone(page, "root:1"));
        await Expect(Zone(page, "root:1")).ToHaveAttributeAsync("data-hover", "true");
        await Screenshots.CaptureAsync(page, "drag.insert-line.desktop.en");
        await page.Mouse.UpAsync();
        await ExpectDirectionsAsync(page, ["up", "left", "down"]);

        Assert.Empty(errors.Errors);
    }

    [Theory]
    [InlineData("de-CH", "de")]
    [InlineData("en", "en")]
    public async Task InsertLine_IsShownOnLaptop(string locale, string language)
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync(locale, 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenAsync(page, errors);
        await page.GetByTestId($"language-{language}").ClickAsync();
        await Expect(page.GetByTestId($"language-{language}")).ToHaveAttributeAsync("aria-pressed", "true");

        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("palette-down").ClickAsync();
        await BeginDragAsync(page, page.GetByTestId("palette-left"), Zone(page, "root:1"));
        await Expect(Zone(page, "root:1")).ToHaveAttributeAsync("data-valid", "true");
        await Expect(Zone(page, "root:1")).ToHaveAttributeAsync("data-hover", "true");
        await Screenshots.CaptureAsync(page, $"drag.insert-line.laptop.{language}");
        await page.Mouse.UpAsync();
        await ExpectDirectionsAsync(page, ["up", "left", "down"]);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task DragBlockOntoPalette_ShowsDeleteOverlayOnLaptop()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenAsync(page, errors);

        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("palette-down").ClickAsync();
        await BeginDragAsync(page, Row(page, "2"), page.GetByTestId("palette"));
        await Expect(page.GetByTestId("palette-delete")).ToBeVisibleAsync();
        await Screenshots.CaptureAsync(page, "drag.delete.laptop.de");
        await page.Mouse.UpAsync();
        await ExpectPathsAsync(page, ["1"]);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task InvalidTarget_IsShownInEnglish()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("en");
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenAsync(page, errors);
        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("language-en")).ToHaveAttributeAsync("aria-pressed", "true");

        await page.GetByTestId("palette-repeat").ClickAsync();
        await page.GetByTestId("program-select-1").ClickAsync();
        await page.GetByTestId("palette-repeat").ClickAsync();
        await ExpectPathsAsync(page, ["1", "1.1"]);

        await BeginDragAsync(page, page.GetByTestId("palette-repeat"), Zone(page, "1.1:0"));
        await Expect(Zone(page, "1.1:0")).ToHaveAttributeAsync("data-valid", "false");
        await Expect(Zone(page, "1.1:0")).ToHaveAttributeAsync("data-hover", "true");
        await Screenshots.CaptureAsync(page, "drag.invalid.desktop.en");
        await page.Mouse.UpAsync();
        await ExpectPathsAsync(page, ["1", "1.1"]);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task Reorder_IntoAndOutOfRepeat()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenAsync(page, errors);

        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("palette-down").ClickAsync();
        await page.GetByTestId("palette-repeat").ClickAsync();
        await ExpectPathsAsync(page, ["1", "2", "3"]);

        await DragAsync(page, Row(page, "1"), Zone(page, "root:2"));
        await ExpectPathsAsync(page, ["1", "2", "3"]);
        await Expect(page.GetByTestId("program-block").Nth(0)).ToHaveAttributeAsync("data-direction", "down");
        await Expect(page.GetByTestId("program-block").Nth(1)).ToHaveAttributeAsync("data-direction", "up");

        await DragAsync(page, Row(page, "1"), Zone(page, "3:0"));
        await ExpectPathsAsync(page, ["1", "2", "2.1"]);
        await Expect(page.GetByTestId("program-block").Nth(2)).ToHaveAttributeAsync("data-direction", "down");

        await DragAsync(page, Row(page, "2.1"), Zone(page, "root:0"));
        await ExpectPathsAsync(page, ["1", "2", "3"]);
        await Expect(page.GetByTestId("program-block").Nth(0)).ToHaveAttributeAsync("data-direction", "down");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task DragBlockOntoPalette_DeletesIt()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenAsync(page, errors);

        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("palette-repeat").ClickAsync();
        await page.GetByTestId("program-select-2").ClickAsync();
        await page.GetByTestId("palette-down").ClickAsync();
        await ExpectPathsAsync(page, ["1", "2", "2.1"]);

        await BeginDragAsync(page, Row(page, "2"), page.GetByTestId("palette"));
        await Expect(page.GetByTestId("palette-delete")).ToBeVisibleAsync();
        await Screenshots.CaptureAsync(page, "drag.delete.desktop.de");
        await page.Mouse.UpAsync();

        await ExpectPathsAsync(page, ["1"]);
        await Expect(page.GetByTestId("palette-delete")).ToHaveCountAsync(0);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task InvalidTarget_ShowsNoAndChangesNothing()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenAsync(page, errors);

        await page.GetByTestId("palette-repeat").ClickAsync();
        await page.GetByTestId("program-select-1").ClickAsync();
        await page.GetByTestId("palette-repeat").ClickAsync();
        await ExpectPathsAsync(page, ["1", "1.1"]);

        await BeginDragAsync(page, page.GetByTestId("palette-repeat"), Zone(page, "1.1:0"));
        await Expect(Zone(page, "1.1:0")).ToHaveAttributeAsync("data-valid", "false");
        await Expect(Zone(page, "1.1:0")).ToHaveAttributeAsync("data-hover", "true");
        await Expect(Zone(page, "root:0")).ToHaveAttributeAsync("data-valid", "true");
        await Screenshots.CaptureAsync(page, "drag.invalid.desktop.de");
        await page.Mouse.UpAsync();

        await ExpectPathsAsync(page, ["1", "1.1"]);
        await Expect(Zone(page, "root:0")).Not.ToHaveAttributeAsync("data-valid", "true");

        await BeginDragAsync(page, Row(page, "1"), Zone(page, "1:0"));
        await Expect(Zone(page, "1:0")).ToHaveAttributeAsync("data-valid", "false");
        await page.Mouse.UpAsync();
        await ExpectPathsAsync(page, ["1", "1.1"]);

        Assert.Empty(errors.Errors);
    }

    private static async Task OpenAsync(IPage page, BrowserErrors errors)
    {
        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
    }

    private static ILocator Zone(IPage page, string slot) => page.Locator($"[data-testid=drop-zone][data-slot='{slot}']");

    private static ILocator Row(IPage page, string path) => page.Locator($"[data-testid='block-row-{path}']");

    private static async Task DragAsync(IPage page, ILocator source, ILocator target)
    {
        await BeginDragAsync(page, source, target);
        await page.Mouse.UpAsync();
    }

    /// <summary>Presses on <paramref name="source"/> and moves over <paramref name="target"/> (several moves: dragover needs at least two) without releasing.</summary>
    private static async Task BeginDragAsync(IPage page, ILocator source, ILocator target)
    {
        // The previous drop ends its drag in a render that may still be pending: zones left over from it would be measured at their old size.
        await Expect(page.Locator("[data-testid=drop-zone][data-valid]")).ToHaveCountAsync(0);
        await source.HoverAsync();
        await page.Mouse.DownAsync();
        var start = (await source.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(start.X + (start.Width / 2) + 8, start.Y + (start.Height / 2) + 8, new() { Steps = 4 });

        // Every zone grows in the render that follows dragstart (idle 0.625 rem, end zones 0, then 1.5 rem). The zones render one after the other,
        // so measuring while only some have grown puts the pointer next to the target. Wait until none is left idle.
        await Expect(page.Locator("[data-testid=drop-zone]:not([data-valid])")).ToHaveCountAsync(0);
        var box = await StableBoxAsync(page, target);
        var x = box.X + (box.Width / 2);
        var y = box.Y + (box.Height / 2);
        await page.Mouse.MoveAsync(x, y, new() { Steps = 12 });
        await page.Mouse.MoveAsync(x + 1, y, new() { Steps = 3 });
        await page.Mouse.MoveAsync(x, y, new() { Steps = 3 });

        if (await target.GetAttributeAsync("data-testid") == "drop-zone")
        {
            await Expect(target).ToHaveAttributeAsync("data-hover", "true");
        }
    }

    /// <summary>Measures <paramref name="target"/> after two animation frames, until two consecutive measurements agree (the list is still settling after the drag started).</summary>
    private static async Task<LocatorBoundingBoxResult> StableBoxAsync(IPage page, ILocator target)
    {
        LocatorBoundingBoxResult? previous = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
            var box = (await target.BoundingBoxAsync())!;
            if (previous is not null && previous.X == box.X && previous.Y == box.Y && previous.Width == box.Width && previous.Height == box.Height)
            {
                return box;
            }

            previous = box;
        }

        throw new InvalidOperationException("The drop target never stopped moving.");
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

    private static async Task ExpectDirectionsAsync(IPage page, string[] directions)
    {
        var blocks = page.GetByTestId("program-block");
        await Expect(blocks).ToHaveCountAsync(directions.Length);
        for (var index = 0; index < directions.Length; index++)
        {
            await Expect(blocks.Nth(index)).ToHaveAttributeAsync("data-direction", directions[index]);
        }
    }
}
