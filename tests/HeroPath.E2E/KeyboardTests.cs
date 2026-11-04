using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class KeyboardTests(AppFixture app)
{
    private static readonly LocatorAssertionsToHaveAttributeOptions _slow = new() { Timeout = 20_000 };

    private const string RecordDefaultPreventedScript = """
        window.__prevented = [];
        window.addEventListener("keydown", e => window.__prevented.push(e.defaultPrevented));
        """;

    private static async Task StartPlayAsync(IPage page, BrowserErrors errors)
    {
        await page.StartGameAsync(errors);
        await WaitForShortcutsAsync(page);
    }

    private static Task WaitForShortcutsAsync(IPage page) =>
        Expect(page.Locator("main.play-page")).ToHaveAttributeAsync("data-shortcuts", "active");

    private static ILocator Blocks(IPage page) => page.GetByTestId("program-block");

    private static ILocator Status(IPage page) => page.GetByTestId("status");

    private static async Task ExpectDirectionsAsync(IPage page, params string[] directions)
    {
        await Expect(Blocks(page)).ToHaveCountAsync(directions.Length);
        for (var index = 0; index < directions.Length; index++)
        {
            await Expect(Blocks(page).Nth(index)).ToHaveAttributeAsync("data-direction", directions[index]);
        }
    }

    private static async Task PressAllAsync(IPage page, params string[] keys)
    {
        foreach (var key in keys)
        {
            await page.Keyboard.PressAsync(key);
        }
    }

    private static async Task<bool[]> PreventedAsync(IPage page) => await page.EvaluateAsync<bool[]>("window.__prevented");

    [Fact]
    public async Task Level_CanBeSolvedWithTheKeyboardOnly()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await StartPlayAsync(page, errors);

        await PressAllAsync(page, "ArrowUp", "ArrowUp", "ArrowRight", "ArrowRight", "ArrowRight", "ArrowRight", "ArrowRight", "ArrowUp");
        await ExpectDirectionsAsync(page, "up", "up", "right", "right", "right", "right", "right", "up");

        await page.Keyboard.PressAsync("Space");
        await Expect(Status(page)).ToHaveAttributeAsync("data-state", "succeeded", _slow);
        await Expect(page.GetByTestId("success-confetti")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("r");
        await Expect(Status(page)).ToHaveAttributeAsync("data-state", "ready");
        await ExpectDirectionsAsync(page, "up", "up", "right", "right", "right", "right", "right", "up");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task HoldingAKey_AddsOnlyOneBlock()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await StartPlayAsync(page, errors);

        for (var i = 0; i < 6; i++)
        {
            await page.Keyboard.DownAsync("ArrowRight");
        }

        await page.Keyboard.UpAsync("ArrowRight");
        await ExpectDirectionsAsync(page, "right");

        await page.Keyboard.DownAsync("ArrowUp");
        await page.Keyboard.DownAsync("ArrowUp");
        await page.Keyboard.UpAsync("ArrowUp");
        await ExpectDirectionsAsync(page, "right", "up");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task FocusInSelectAndTempoSlider_FiresNoShortcut()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await StartPlayAsync(page, errors);
        await page.GetByTestId("palette-up").ClickAsync();
        await page.EvaluateAsync(RecordDefaultPreventedScript);

        await page.GetByTestId("control-tempo").FocusAsync();
        await PressAllAsync(page, "ArrowRight", "ArrowLeft", "ArrowUp", "ArrowDown", "d", "r", "Delete", "Escape");
        await ExpectDirectionsAsync(page, "up");
        await Expect(Status(page)).ToHaveAttributeAsync("data-state", "ready");

        await page.GetByTestId("level-picker").FocusAsync();
        await PressAllAsync(page, "ArrowDown", "ArrowUp", "ArrowRight", "ArrowLeft", "Delete", "Backspace", "Escape");
        await Expect(page.GetByTestId("level-picker")).ToBeFocusedAsync();

        var prevented = await PreventedAsync(page);
        Assert.Equal(15, prevented.Length);
        Assert.DoesNotContain(true, prevented);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task FocusInNameField_TypesTextAndFiresNoShortcut()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await StartPlayAsync(page, errors);
        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("hero-change").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.EvaluateAsync(RecordDefaultPreventedScript);

        var name = page.GetByTestId("hero-name");
        await name.FillAsync("Zoe");
        await name.PressAsync("End");
        await page.Keyboard.TypeAsync(" d r");
        await PressAllAsync(page, "Backspace", "Delete", "Escape");
        await page.Keyboard.TypeAsync("Rd ");

        await Expect(name).ToHaveValueAsync("Zoe d Rd ");
        Assert.DoesNotContain(true, await PreventedAsync(page));

        await page.GetByTestId("start-go").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
        await WaitForShortcutsAsync(page);
        await ExpectDirectionsAsync(page, "up");
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task SpaceOnAFocusedButton_OnlyTriggersThatButton()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await StartPlayAsync(page, errors);
        await PressAllAsync(page, "ArrowUp", "ArrowRight");
        await ExpectDirectionsAsync(page, "up", "right");

        var select = page.GetByTestId("program-select-1");
        await select.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(select).ToHaveAttributeAsync("aria-pressed", "true");
        await page.Keyboard.PressAsync("Space");
        await Expect(select).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Status(page)).ToHaveAttributeAsync("data-state", "ready");
        await ExpectDirectionsAsync(page, "up", "right");

        await page.GetByTestId("palette-left").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await ExpectDirectionsAsync(page, "up", "right", "left");
        await Expect(Status(page)).ToHaveAttributeAsync("data-state", "ready");

        await page.GetByTestId("control-play").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(Status(page)).Not.ToHaveAttributeAsync("data-state", "ready");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task SelectedBlock_CanBeMovedDeletedAndDeselectedByKeyboard()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await StartPlayAsync(page, errors);
        await PressAllAsync(page, "ArrowUp", "ArrowDown", "ArrowLeft");

        await page.GetByTestId("program-select-3").ClickAsync();
        await page.Keyboard.PressAsync("Shift+ArrowUp");
        await ExpectDirectionsAsync(page, "up", "left", "down");
        await page.Keyboard.PressAsync("Shift+ArrowUp");
        await ExpectDirectionsAsync(page, "left", "up", "down");
        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await ExpectDirectionsAsync(page, "up", "left", "down");

        await page.Keyboard.PressAsync("Backspace");
        await ExpectDirectionsAsync(page, "up", "down");

        await page.GetByTestId("program-select-1").ClickAsync();
        await Expect(page.GetByTestId("program-select-1")).ToHaveAttributeAsync("aria-pressed", "true");
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByTestId("program-select-1")).ToHaveAttributeAsync("aria-pressed", "false");
        await page.Keyboard.PressAsync("Delete");
        await ExpectDirectionsAsync(page, "up", "down");

        await page.GetByTestId("program-select-2").ClickAsync();
        await page.Keyboard.PressAsync("Delete");
        await ExpectDirectionsAsync(page, "up");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task DebugAndReset_WorkByKeyboard()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await StartPlayAsync(page, errors);
        await PressAllAsync(page, "ArrowUp", "ArrowUp");

        await page.Keyboard.PressAsync("d");
        await Expect(Status(page)).ToHaveAttributeAsync("data-state", "paused");
        await Expect(Blocks(page).Nth(0)).ToHaveAttributeAsync("data-current", "true");
        await page.Keyboard.PressAsync("Shift+D");
        await Expect(Blocks(page).Nth(1)).ToHaveAttributeAsync("data-current", "true");

        await page.Keyboard.PressAsync("ArrowDown");
        await ExpectDirectionsAsync(page, "up", "up");

        await page.Keyboard.PressAsync("R");
        await Expect(Status(page)).ToHaveAttributeAsync("data-state", "ready");
        await page.Keyboard.PressAsync("ArrowDown");
        await ExpectDirectionsAsync(page, "up", "up", "down");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task ResetByKey_StopsARunningPlay_AndEditingKeysAreLockedMeanwhile()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await StartPlayAsync(page, errors);
        await PressAllAsync(page, "ArrowUp", "ArrowUp", "ArrowUp");
        await page.GetByTestId("control-tempo").FillAsync("0");
        await page.EvaluateAsync("document.activeElement.blur()");

        await page.Keyboard.PressAsync("Space");
        await Expect(Status(page)).ToHaveAttributeAsync("data-state", "running");
        await page.Keyboard.PressAsync("ArrowLeft");
        await ExpectDirectionsAsync(page, "up", "up", "up");
        await page.Keyboard.PressAsync("r");
        await Expect(Status(page)).ToHaveAttributeAsync("data-state", "ready");
        await page.Keyboard.PressAsync("ArrowLeft");
        await ExpectDirectionsAsync(page, "up", "up", "up", "left");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task ModifierKeys_DoNotTriggerShortcuts()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await StartPlayAsync(page, errors);
        await page.GetByTestId("palette-up").ClickAsync();
        await page.EvaluateAsync("document.activeElement.blur()");
        await page.EvaluateAsync(RecordDefaultPreventedScript);

        await PressAllAsync(page, "Control+ArrowUp", "Alt+ArrowLeft", "Meta+ArrowDown", "Control+d", "Alt+d", "Meta+r", "Control+Space", "Shift+ArrowLeft");

        await ExpectDirectionsAsync(page, "up");
        await Expect(Status(page)).ToHaveAttributeAsync("data-state", "ready");
        Assert.DoesNotContain(true, await PreventedAsync(page));
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task StartAndEditorPage_HaveNoShortcuts()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await StartPlayAsync(page, errors);
        await page.Keyboard.PressAsync("ArrowUp");
        await ExpectDirectionsAsync(page, "up");

        await page.GetByTestId("hero-change").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.EvaluateAsync("document.activeElement.blur()");
        await page.EvaluateAsync(RecordDefaultPreventedScript);
        await PressAllAsync(page, "ArrowUp", "ArrowDown", "ArrowLeft", "d", "r", "Space");

        await page.GetByTestId("start-build").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=editor-panel]", errors);
        await page.EvaluateAsync("document.activeElement.blur()");
        await PressAllAsync(page, "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "d", "r", "Delete", "Escape");
        Assert.DoesNotContain(true, await PreventedAsync(page));

        await page.GetByTestId("editor-back").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.GetByTestId("level-card-easy-2").ClickAsync();
        await page.GetByTestId("start-go").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
        await WaitForShortcutsAsync(page);
        await ExpectDirectionsAsync(page, "up");

        Assert.Empty(errors.Errors);
    }

    [Theory]
    [InlineData("de", "language-de", 1366, 768, "laptop")]
    [InlineData("en", "language-en", 1366, 768, "laptop")]
    [InlineData("de", "language-de", 1920, 1080, "desktop")]
    public async Task ShortcutHelp_IsCollapsedFitsTheScreenAndOpens(string lang, string languageButton, int width, int height, string viewport)
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", width, height);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await StartPlayAsync(page, errors);
        await page.GetByTestId(languageButton).ClickAsync();

        var help = page.GetByTestId("shortcut-help");
        await Expect(help).ToBeVisibleAsync();
        await Expect(help).Not.ToHaveAttributeAsync("open", "");
        await Screenshots.CaptureAsync(page, $"shortcut-help-closed.{viewport}.{lang}");
        await LayoutAssertions.AssertNoPageScrollAsync(page);
        await LayoutAssertions.AssertSidePanelFitsAsync(page);

        await page.GetByTestId("shortcut-help-toggle").ClickAsync();
        await Expect(help).ToHaveAttributeAsync("open", "");
        await Expect(page.Locator("[data-testid=shortcut-help] kbd").First).ToBeVisibleAsync();
        await LayoutAssertions.AssertInViewportAsync(page, "[data-testid=shortcut-help-body]");
        await Screenshots.CaptureAsync(page, $"shortcut-help.{viewport}.{lang}");
        await LayoutAssertions.AssertNoPageScrollAsync(page);

        await page.Keyboard.PressAsync("ArrowUp");
        await ExpectDirectionsAsync(page, "up");
        await Screenshots.CaptureAsync(page, $"shortcut-help-with-block.{viewport}.{lang}");

        Assert.Empty(errors.Errors);
    }
}
