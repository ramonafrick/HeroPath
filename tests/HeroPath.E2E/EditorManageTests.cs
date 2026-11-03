using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class EditorManageTests(AppFixture app)
{
    private const string LevelName = "Mein Weg";

    private static async Task OpenEditorFromStartPageAsync(IPage page, BrowserErrors errors)
    {
        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.GetByTestId("start-build").ClickAsync();
        await WaitForEditorAsync(page, errors);
    }

    private static async Task WaitForEditorAsync(IPage page, BrowserErrors errors)
    {
        await page.WaitForRenderedAsync("[data-testid=editor-page]", errors);
        await Expect(page.GetByTestId("edit-cell-0-0")).ToBeVisibleAsync();
    }

    private static async Task SwitchLanguageAsync(IPage page, string language)
    {
        await page.GetByTestId($"language-{language}").ClickAsync();
        await Expect(page.GetByTestId($"language-{language}")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    private static async Task SaveAsync(IPage page)
    {
        await page.GetByTestId("editor-save").ClickAsync();
        await Expect(page.GetByTestId("editor-saved")).ToHaveAttributeAsync("data-outcome", "Saved");
    }

    [Fact]
    public async Task Template_CopiesAStandardLevel_AndLeavesTheOriginalUntouched()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenEditorFromStartPageAsync(page, errors);

        await page.GetByTestId("editor-template").SelectOptionAsync("easy-1");

        await Expect(page.GetByTestId("editor-name")).ToHaveValueAsync("Mein Level 1");
        await Expect(page.GetByTestId("edit-cell-5-5")).ToHaveAttributeAsync("data-kind", "tree");
        await Expect(page.GetByTestId("edit-cell-2-5")).ToHaveAttributeAsync("data-kind", "start");
        await Expect(page.GetByTestId("confirm-dialog")).ToHaveCountAsync(0);
        await LayoutAssertions.AssertNoPageScrollAsync(page);
        await Screenshots.CaptureAsync(page, "editor-template.laptop.de");
        await SwitchLanguageAsync(page, "en");
        await Expect(page.GetByTestId("editor-template-easy-1")).ToHaveTextAsync("Easy 1");
        await Screenshots.CaptureAsync(page, "editor-template.laptop.en");
        await SwitchLanguageAsync(page, "de");

        await page.GetByTestId("editor-tool-rock").ClickAsync();
        await page.GetByTestId("edit-cell-5-3").ClickAsync();
        await page.GetByTestId("editor-name").FillAsync(LevelName);
        await SaveAsync(page);

        await page.GetByTestId("editor-back").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("custom-levels").Locator("[data-testid^=level-card-]")).ToHaveTextAsync(LevelName);
        await Expect(page.GetByTestId("level-card-easy-1")).ToBeVisibleAsync();

        await page.GetByTestId("level-card-easy-1").ClickAsync();
        await page.GetByTestId("start-go").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
        await Expect(page.GetByTestId("cell-5-3")).ToHaveAttributeAsync("data-kind", "empty");
        await Expect(page.GetByTestId("cell-5-5")).ToHaveAttributeAsync("data-kind", "tree");
        await Expect(page.GetByTestId("play-edit")).ToHaveCountAsync(0);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task Delete_AsksFirst_CancelAndEscapeKeepTheLevel_ConfirmRemovesIt()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenEditorFromStartPageAsync(page, errors);
        await page.GetByTestId("editor-name").FillAsync(LevelName);
        await SaveAsync(page);
        var deleteButton = page.Locator("[data-testid^=custom-level-delete-]");
        await Expect(deleteButton).ToHaveCountAsync(1);

        await deleteButton.ClickAsync();

        var dialog = page.GetByTestId("confirm-dialog");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog).ToHaveAttributeAsync("role", "dialog");
        await Expect(dialog).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(page.GetByTestId("confirm-message")).ToContainTextAsync($"Level «{LevelName}» wirklich löschen?");
        await Expect(page.GetByTestId("confirm-yes")).ToBeFocusedAsync();
        await AssertButtonsBigEnoughAsync(page);
        await Screenshots.CaptureAsync(page, "confirm-delete.laptop.de");
        await page.GetByTestId("confirm-no").ClickAsync();
        await SwitchLanguageAsync(page, "en");
        await deleteButton.ClickAsync();
        await Expect(page.GetByTestId("confirm-title")).ToHaveTextAsync("Delete level?");
        await Expect(page.GetByTestId("confirm-no")).ToHaveTextAsync("Cancel");
        await Screenshots.CaptureAsync(page, "confirm-delete.laptop.en");
        await page.GetByTestId("confirm-no").ClickAsync();
        await SwitchLanguageAsync(page, "de");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(deleteButton).ToHaveCountAsync(1);

        await deleteButton.ClickAsync();
        await Expect(dialog).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(deleteButton).ToHaveCountAsync(1);

        await deleteButton.ClickAsync();
        await page.GetByTestId("confirm-yes").ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(deleteButton).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("editor-name")).ToHaveValueAsync("Mein Level 1");

        await page.ReloadAsync();
        await page.WaitForRenderedAsync("[data-testid=editor-page]", errors);
        await Expect(page.Locator("[data-testid^=custom-level-delete-]")).ToHaveCountAsync(0);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task Clear_AsksFirst_AndKeepsStartGoalAndSize()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenEditorFromStartPageAsync(page, errors);
        await page.GetByTestId("editor-template").SelectOptionAsync("easy-2");
        await Expect(page.GetByTestId("edit-cell-3-6")).ToHaveAttributeAsync("data-kind", "tree");

        await page.GetByTestId("editor-clear").ClickAsync();
        await Expect(page.GetByTestId("confirm-dialog")).ToBeVisibleAsync();
        await page.GetByTestId("confirm-no").ClickAsync();
        await Expect(page.GetByTestId("edit-cell-3-6")).ToHaveAttributeAsync("data-kind", "tree");

        await page.GetByTestId("editor-clear").ClickAsync();
        await page.GetByTestId("confirm-yes").ClickAsync();

        await Expect(page.GetByTestId("edit-cell-3-6")).ToHaveAttributeAsync("data-kind", "empty");
        await Expect(page.GetByTestId("edit-cell-1-6")).ToHaveAttributeAsync("data-kind", "start");
        await Expect(page.GetByTestId("editor-width")).ToHaveTextAsync("10");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task TryOut_WithoutAHero_PreselectsTheLevelOnTheStartPage_AndPlayCanOpenTheEditorAgain()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenEditorFromStartPageAsync(page, errors);
        await page.GetByTestId("editor-name").FillAsync(LevelName);

        await page.GetByTestId("editor-try").ClickAsync();

        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("confirm-dialog")).ToHaveCountAsync(0);
        var card = page.GetByTestId("custom-levels").Locator("[data-testid^=level-card-]");
        await Expect(card).ToHaveTextAsync(LevelName);
        await Expect(card).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByTestId("level-card-free-field")).ToHaveAttributeAsync("aria-pressed", "false");
        await Screenshots.CaptureAsync(page, "start-preselected.laptop.de");

        await page.GetByTestId("start-go").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
        await Expect(page.GetByTestId("level-name")).ToHaveTextAsync(LevelName);
        await Expect(page.GetByTestId("play-edit")).ToBeVisibleAsync();
        await LayoutAssertions.AssertNoPageScrollAsync(page);
        var edit = await page.GetByTestId("play-edit").BoundingBoxAsync();
        Assert.NotNull(edit);
        Assert.True(edit.Height >= 44 - 0.5, $"play-edit is {edit.Height}px high");
        await Screenshots.CaptureAsync(page, "play-edit-button.laptop.de");
        await SwitchLanguageAsync(page, "en");
        await Expect(page.GetByTestId("play-edit")).ToHaveTextAsync("Edit");
        await Screenshots.CaptureAsync(page, "play-edit-button.laptop.en");
        await SwitchLanguageAsync(page, "de");

        await page.GetByTestId("play-edit").ClickAsync();
        await WaitForEditorAsync(page, errors);
        await Expect(page.GetByTestId("editor-name")).ToHaveValueAsync(LevelName);
        await Expect(page.GetByTestId("confirm-dialog")).ToHaveCountAsync(0);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task TryOut_WithAHero_GoesStraightToPlay_AndTheHeroIsKept()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await page.StartGameAsync(errors, "easy-1", hero: "lisa", name: "Zoe");
        await page.GetByTestId("hero-change").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.GetByTestId("start-build").ClickAsync();
        await WaitForEditorAsync(page, errors);

        await page.GetByTestId("editor-try").ClickAsync();

        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
        await Expect(page.GetByTestId("level-name")).ToHaveTextAsync("Mein Level 1");
        await Expect(page.GetByTestId("hero-badge-name")).ToHaveTextAsync("Zoe");
        await Expect(page.GetByTestId("hero").GetByTestId("graphic-hero-lisa")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("level-picker")).Not.ToHaveValueAsync("easy-1");

        await page.GetByTestId("play-edit").ClickAsync();
        await WaitForEditorAsync(page, errors);
        await Expect(page.GetByTestId("editor-name")).ToHaveValueAsync("Mein Level 1");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task Leaving_ADirtyEditor_AsksFirst_CancelStays_ConfirmLeaves_AndACleanEditorNeverAsks()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenEditorFromStartPageAsync(page, errors);

        await page.GetByTestId("editor-back").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("confirm-dialog")).ToHaveCountAsync(0);

        await page.GetByTestId("start-build").ClickAsync();
        await WaitForEditorAsync(page, errors);
        await page.GetByTestId("editor-tool-tree").ClickAsync();
        await page.GetByTestId("edit-cell-4-4").ClickAsync();

        await page.GetByTestId("editor-back").ClickAsync();

        var dialog = page.GetByTestId("confirm-dialog");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(page.GetByTestId("confirm-message")).ToHaveTextAsync("Deine Änderungen sind noch nicht gespeichert. Trotzdem weggehen?");
        await Expect(page.GetByTestId("confirm-yes")).ToBeFocusedAsync();
        await AssertButtonsBigEnoughAsync(page);
        await Screenshots.CaptureAsync(page, "confirm-leave.laptop.de");
        await page.GetByTestId("confirm-no").ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        await SwitchLanguageAsync(page, "en");
        await page.GetByTestId("editor-back").ClickAsync();
        await Expect(page.GetByTestId("confirm-message")).ToHaveTextAsync("Your changes are not saved yet. Leave anyway?");
        await Screenshots.CaptureAsync(page, "confirm-leave.laptop.en");
        await page.GetByTestId("confirm-no").ClickAsync();
        await SwitchLanguageAsync(page, "de");
        await Expect(page.GetByTestId("editor-page")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("edit-cell-4-4")).ToHaveAttributeAsync("data-kind", "tree");

        await page.GetByTestId("editor-back").ClickAsync();
        await Expect(dialog).ToBeVisibleAsync();
        await page.GetByTestId("confirm-yes").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(dialog).ToHaveCountAsync(0);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task Saving_ADirtyEditor_ThenLeaving_DoesNotAsk()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenEditorFromStartPageAsync(page, errors);
        await page.GetByTestId("edit-cell-4-4").ClickAsync();
        await SaveAsync(page);

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/editor/custom-[0-9a-f]{12}$"));
        await page.GetByTestId("editor-back").ClickAsync();

        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("confirm-dialog")).ToHaveCountAsync(0);

        Assert.Empty(errors.Errors);
    }

    private static async Task AssertButtonsBigEnoughAsync(IPage page)
    {
        foreach (var testId in new[] { "confirm-yes", "confirm-no" })
        {
            var box = await page.GetByTestId(testId).BoundingBoxAsync();
            Assert.NotNull(box);
            Assert.True(box.Height >= 43.5 && box.Width >= 43.5, $"{testId} is {box.Width}x{box.Height}px");
        }
    }
}
