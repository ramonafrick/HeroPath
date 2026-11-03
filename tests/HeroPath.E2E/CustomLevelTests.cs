using System.Text.Json;
using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class CustomLevelTests(AppFixture app)
{
    private const string LevelId = "custom-e2e0001";
    private const string LevelName = "Mein <b>Burgweg</b>";

    private const string StoredLevels = """
        [
          {"schemaVersion":1,"id":"custom-e2e0001","name":"Mein <b>Burgweg</b>","width":5,"height":5,
           "start":{"x":0,"y":4},"goal":{"x":2,"y":4},"requireAllItems":false,
           "obstacles":[{"x":1,"y":4,"kind":"tree"}],"items":[]},
          {"schemaVersion":1,"id":"custom-broken","name":"Kaputt","width":99,"height":5,
           "start":{"x":0,"y":0},"goal":{"x":1,"y":1},"requireAllItems":false,"obstacles":[],"items":[]},
          "not a level"
        ]
        """;

    [Theory]
    [InlineData(1366, 768, "laptop")]
    [InlineData(1920, 1080, "desktop")]
    public async Task CustomLevel_ShowsOnStartPage_CanBePlayedAndSolved_AndSurvivesReload(int width, int height, string viewport)
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", width, height);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await page.AddInitScriptAsync(
            $"if (localStorage.getItem('heropath.levels') === null) localStorage.setItem('heropath.levels', {JsonSerializer.Serialize(StoredLevels)});");

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);

        var section = page.GetByTestId("custom-levels");
        await Expect(section).ToBeVisibleAsync();
        await Expect(section.GetByTestId($"level-card-{LevelId}")).ToHaveTextAsync(LevelName);
        await Expect(section.GetByTestId($"level-preview-{LevelId}")).ToBeVisibleAsync();
        var jump = page.GetByTestId("start-jump-custom");
        await Expect(jump).ToHaveTextAsync("Zu meinen Levels");
        var jumpBox = await jump.BoundingBoxAsync();
        Assert.True(jumpBox is { Height: >= 43.5f }, "start-jump-custom is smaller than 44 px.");
        await LayoutAssertions.AssertInViewportAsync(page, "[data-testid=start-jump-custom]");
        await Expect(page.GetByTestId("level-card-custom-broken")).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("level-card-easy-1")).ToBeVisibleAsync();
        await Screenshots.CaptureAsync(page, $"start-custom-levels.{viewport}.de");

        await jump.ClickAsync();
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/#custom-levels$"));
        await LayoutAssertions.AssertInViewportAsync(page, "[data-testid=custom-levels]");

        await page.GetByTestId($"level-card-{LevelId}").ClickAsync();
        await page.GetByTestId("start-go").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);

        await Expect(page.GetByTestId("level-name")).ToHaveTextAsync(LevelName);
        await Expect(page.GetByTestId("level-picker")).ToHaveValueAsync(LevelId);
        await Expect(page.GetByTestId("level-picker-group-custom")).ToHaveCountAsync(1);
        await Expect(page.GetByTestId("game-board").GetByTestId("cell-1-4")).ToHaveAttributeAsync("data-kind", "tree");

        foreach (var block in new[] { "up", "right", "right", "down" })
        {
            await page.GetByTestId($"palette-{block}").ClickAsync();
        }

        await page.GetByTestId("control-tempo").FillAsync("2");
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded");
        await Expect(page.GetByTestId($"level-picker-option-{LevelId}")).ToHaveAttributeAsync("data-solved", "true");
        await Expect(page.GetByTestId("next-level")).ToHaveCountAsync(0);
        await Screenshots.CaptureAsync(page, $"play-custom-level.{viewport}.de");

        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("control-play")).ToHaveTextAsync("Play");
        await Expect(page.GetByTestId("level-name")).ToHaveTextAsync(LevelName);
        await Screenshots.CaptureAsync(page, $"play-custom-level.{viewport}.en");

        await page.GetByTestId("hero-change").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("custom-levels").GetByRole(AriaRole.Heading)).ToHaveTextAsync("My levels");
        await Expect(page.GetByTestId($"level-card-{LevelId}")).ToHaveAttributeAsync("data-solved", "true");
        await Expect(page.GetByTestId($"level-solved-{LevelId}")).ToBeVisibleAsync();
        await Screenshots.CaptureAsync(page, $"start-custom-levels.{viewport}.en");

        await page.ReloadAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId($"level-card-{LevelId}")).ToHaveTextAsync(LevelName);
        await Expect(page.GetByTestId($"level-card-{LevelId}")).ToHaveAttributeAsync("data-solved", "false");
        await Expect(page.GetByTestId("level-card-custom-broken")).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("level-card-easy-1")).ToBeVisibleAsync();

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task NoCustomLevels_HideTheSection_AndBrokenStorageDoesNotCrash()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await page.AddInitScriptAsync("if (localStorage.getItem('heropath.levels') === null) localStorage.setItem('heropath.levels', '{ this is not json');");

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);

        await Expect(page.GetByTestId("custom-levels")).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("level-card-easy-1")).ToBeVisibleAsync();
        Assert.Empty(errors.Errors);
    }
}
