using System.Text.Json;
using HeroPath.E2E.Infrastructure;
using HeroPath.Modules.Levels.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class LevelFileTests(AppFixture app)
{
    private const string LevelName = "Käse-Weg";

    private const string StoredLevels = """
        [
          {"schemaVersion":1,"id":"custom-e2efile1","name":"Käse-Weg","width":5,"height":5,
           "start":{"x":0,"y":4},"goal":{"x":2,"y":4},"requireAllItems":false,
           "obstacles":[{"x":1,"y":4,"kind":"tree"}],"items":[{"x":0,"y":2,"kind":"strawberry"}]}
        ]
        """;

    private static async Task OpenEditorAsync(IPage page, BrowserErrors errors)
    {
        await page.AddInitScriptAsync(
            $"if (localStorage.getItem('heropath.levels') === null) localStorage.setItem('heropath.levels', {JsonSerializer.Serialize(StoredLevels)});");
        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.GetByTestId("start-build").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=editor-page]", errors);
        await Expect(page.GetByTestId("edit-cell-0-0")).ToBeVisibleAsync();
    }

    private static async Task SwitchLanguageAsync(IPage page, string language)
    {
        await page.GetByTestId($"language-{language}").ClickAsync();
        await Expect(page.GetByTestId($"language-{language}")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    private static string NewTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "heropath-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    [Fact]
    public async Task ExportThenImport_CreatesASecondLevelWithTheSameName_AndAGarbageFileChangesNothing()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        var directory = NewTempDirectory();
        try
        {
            await using var context = await app.NewContextAsync("de-CH", 1366, 768);
            var page = await context.NewPageAsync();
            var errors = BrowserErrors.Track(page);
            await OpenEditorAsync(page, errors);

            var export = page.GetByTestId("custom-level-export-custom-e2efile1");
            await Expect(export).ToBeVisibleAsync();
            var box = await export.BoundingBoxAsync();
            Assert.True(box is { Height: >= 44 }, "Export button is smaller than 44 px.");

            var download = await page.RunAndWaitForDownloadAsync(() => export.ClickAsync());
            Assert.Equal("kaese-weg.heropath.json", download.SuggestedFilename);
            var exported = Path.Combine(directory, download.SuggestedFilename);
            await download.SaveAsAsync(exported);
            var json = await File.ReadAllTextAsync(exported, TestContext.Current.CancellationToken);
            var read = LevelJson.Read(json);
            Assert.NotNull(read.Level);
            Assert.Equal(LevelName, read.Level.Name);
            Assert.Equal("custom-e2efile1", read.Level.Id);
            Assert.Equal(5, read.Level.Width);
            Assert.Single(read.Level.Obstacles);
            Assert.Single(read.Level.Items);
            await Expect(page.GetByTestId("editor-export-failed")).ToHaveCountAsync(0);

            var importInput = page.GetByTestId("editor-import");
            await importInput.SetInputFilesAsync(exported);
            var message = page.GetByTestId("editor-import-message");
            await Expect(message).ToHaveAttributeAsync("data-state", "ok");
            await Expect(message).ToHaveTextAsync("«Käse-Weg (2)» ist jetzt in deiner Liste.");
            await Expect(page.GetByTestId("custom-level-list").GetByText(LevelName, new() { Exact = true })).ToHaveCountAsync(1);
            await Expect(page.GetByTestId("custom-level-list").GetByText(LevelName + " (2)", new() { Exact = true })).ToHaveCountAsync(1);
            await Expect(page.Locator("[data-testid^=custom-level-export-]")).ToHaveCountAsync(2);
            await Expect(page.GetByTestId("editor-status")).ToHaveAttributeAsync("data-state", "solvable");
            await Expect(page.GetByTestId("editor-name")).Not.ToHaveValueAsync(LevelName + " (2)");
            await LayoutAssertions.AssertVisibleInEditorPanelAsync(page, "[data-testid=editor-import-message]", "[data-testid^=custom-level-custom-]");
            await Expect(page.GetByTestId("editor-export-hint")).ToHaveTextAsync("Der Name deines Levels steht in der Datei. Verwende nicht deinen echten Namen.");
            await Screenshots.CaptureAsync(page, "editor-import-ok.laptop.de");
            await SwitchLanguageAsync(page, "en");
            await Expect(message).ToHaveTextAsync("“Käse-Weg (2)” is now in your list.");
            await Screenshots.CaptureAsync(page, "editor-import-ok.laptop.en");
            await SwitchLanguageAsync(page, "de");

            var garbage = Path.Combine(directory, "kaputt.heropath.json");
            await File.WriteAllTextAsync(garbage, "{ this is not a level <script>", TestContext.Current.CancellationToken);
            await importInput.SetInputFilesAsync(garbage);
            await Expect(message).ToHaveAttributeAsync("data-state", "error");
            await Expect(message).ToHaveTextAsync("Das ist kein Level für dieses Spiel. Probier eine andere Datei.");
            await Expect(page.Locator("[data-testid^=custom-level-export-]")).ToHaveCountAsync(2);
            Assert.DoesNotContain("kaputt", await page.ContentAsync());
            await Screenshots.CaptureAsync(page, "editor-import-error.laptop.de");
            await SwitchLanguageAsync(page, "en");
            await Expect(message).ToHaveTextAsync("That is not a level for this game. Try another file.");
            await Screenshots.CaptureAsync(page, "editor-import-error.laptop.en");
            await SwitchLanguageAsync(page, "de");

            var tooLarge = Path.Combine(directory, "gross.heropath.json");
            await File.WriteAllTextAsync(tooLarge, new string(' ', 300 * 1024), TestContext.Current.CancellationToken);
            await importInput.SetInputFilesAsync(tooLarge);
            await Expect(message).ToHaveTextAsync("Die Datei ist zu gross.");
            await LayoutAssertions.AssertVisibleInEditorPanelAsync(page, "[data-testid=editor-import-message]");
            await Screenshots.CaptureAsync(page, "editor-import-too-large.laptop.de");
            await SwitchLanguageAsync(page, "en");
            await Expect(message).ToHaveTextAsync("The file is too large.");
            await Screenshots.CaptureAsync(page, "editor-import-too-large.laptop.en");
            await SwitchLanguageAsync(page, "de");
            await Expect(page.Locator("[data-testid^=custom-level-export-]")).ToHaveCountAsync(2);

            await page.GotoAsync("/");
            await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
            await Expect(page.GetByTestId("custom-levels").GetByText(LevelName, new() { Exact = true })).ToHaveCountAsync(1);
            await Expect(page.GetByTestId("custom-levels").GetByText(LevelName + " (2)", new() { Exact = true })).ToHaveCountAsync(1);

            Assert.Empty(errors.Errors);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Export_WhenTheDownloadCannotStart_ShowsAMessage()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await OpenEditorAsync(page, errors);

        // file-download.js calls URL.createObjectURL when the export runs, so a throwing replacement makes the JS side fail like a locked-down browser.
        await page.EvaluateAsync("() => { URL.createObjectURL = () => { throw new Error('blocked'); }; }");
        await page.GetByTestId("custom-level-export-custom-e2efile1").ClickAsync();

        var failed = page.GetByTestId("editor-export-failed");
        await Expect(failed).ToBeVisibleAsync();
        await Expect(failed).ToHaveTextAsync("Die Datei konnte nicht gespeichert werden. Probier es noch einmal.");
        await LayoutAssertions.AssertVisibleInEditorPanelAsync(page, "[data-testid=editor-export-failed]");
        await Screenshots.CaptureAsync(page, "editor-export-failed.laptop.de");
        await SwitchLanguageAsync(page, "en");
        await Expect(failed).ToBeVisibleAsync();
        await Screenshots.CaptureAsync(page, "editor-export-failed.laptop.en");

        Assert.Empty(errors.Errors);
    }
}
