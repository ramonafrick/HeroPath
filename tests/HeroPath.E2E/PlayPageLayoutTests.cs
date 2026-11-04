using System.Globalization;
using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class PlayPageLayoutTests(AppFixture app)
{
    private static readonly string[] _levelOrder = ["free-field", "easy-1", "easy-2", "strawberries", "small-maze", "long-road", "castle-maze"];

    private static readonly string[] _pizzaDetour =
    [
        "right", "right", "right", "right", "up", "up", "up", "left", "left", "right", "right", "right", "up", "up",
    ];

    [Theory]
    [InlineData("de", "language-de", 1366, 768, "laptop")]
    [InlineData("en", "language-en", 1366, 768, "laptop")]
    [InlineData("de", "language-de", 1920, 1080, "desktop")]
    [InlineData("en", "language-en", 1920, 1080, "desktop")]
    public async Task SuccessWithLongMessageAndLongName_FitsWithoutPageScroll(string lang, string languageButton, int width, int height, string viewport)
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", width, height);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        var name = new string('W', 20);

        await page.StartGameAsync(errors, _levelOrder[0], "lisa", name);
        await page.GetByTestId("sound-toggle").ClickAsync();
        await page.GetByTestId(languageButton).ClickAsync();
        foreach (var id in _levelOrder)
        {
            if (id != _levelOrder[0])
            {
                await page.GetByTestId("level-picker").SelectOptionAsync(id);
                await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "ready");
            }

            await LevelSolutions.SolveAsync(page, id);
        }

        await page.GetByTestId("level-picker").SelectOptionAsync("strawberries");
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "ready");
        foreach (var direction in _pizzaDetour)
        {
            await page.GetByTestId($"palette-{direction}").ClickAsync();
        }

        await page.GetByTestId("control-tempo").FillAsync("2");
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded", new() { Timeout = 30000 });

        var message = page.GetByTestId("message");
        await Expect(message).ToContainTextAsync(name);
        await Expect(message).ToContainTextAsync(lang == "de" ? "Pizza-Stück" : "pizza");
        await Expect(message).ToContainTextAsync(lang == "de" ? "alle Level geschafft" : "all the levels");
        await Expect(page.GetByTestId("next-level")).ToBeVisibleAsync();

        await LayoutAssertions.AssertNoPageScrollAsync(page);
        await LayoutAssertions.AssertSidePanelFitsAsync(page);
        Directory.CreateDirectory(E2EEnvironment.ScreenshotDirectory);
        var spare = await SpareRoomAsync(page);
        await File.WriteAllTextAsync(
            Path.Combine(E2EEnvironment.ScreenshotDirectory, $"play-layout.message-long.{viewport}.{lang}.txt"),
            $"{viewport}.{lang}: {spare}{Environment.NewLine}",
            TestContext.Current.CancellationToken);
        var clipped = await page.EvaluateAsync<bool>("() => { const live = document.querySelector('.message-live'); return live.scrollHeight > live.clientHeight + 1; }");
        Assert.False(clipped, $"The message must not scroll inside its box: {spare}");
        Assert.True(spare.PanelBottom - spare.ToolsBottom >= 0 && spare.NextBottom <= spare.PanelBottom, $"Side panel too tight: {spare}");
        await Screenshots.CaptureAsync(page, $"message-long.{viewport}.{lang}");

        await page.GetByTestId("shortcut-help-toggle").ClickAsync();
        await LayoutAssertions.AssertShortcutHelpLeavesControlsFreeAsync(page);
        await LayoutAssertions.AssertNoPageScrollAsync(page);
        await Screenshots.CaptureAsync(page, $"message-long-help.{viewport}.{lang}");

        Assert.Empty(errors.Errors);
    }

    private static async Task<SpareRoom> SpareRoomAsync(IPage page)
    {
        var values = await page.EvaluateAsync<double[]>(@"() => {
            const rect = selector => document.querySelector(selector).getBoundingClientRect();
            return [rect('.app-header').height, rect('[data-testid=side-panel]').bottom, rect('[data-testid=side-actions]').bottom,
                rect('[data-testid=next-level]').bottom, rect('[data-testid=message]').height, window.innerHeight];
        }");
        return new SpareRoom(values[0], values[1], values[2], values[3], values[4], values[5]);
    }

    private sealed record SpareRoom(double Header, double PanelBottom, double ToolsBottom, double NextBottom, double MessageHeight, double Viewport)
    {
        public override string ToString() => string.Create(
            CultureInfo.InvariantCulture,
            $"header {Header:0.#} px, side panel ends at {PanelBottom:0.#}, actions end at {ToolsBottom:0.#} (spare {PanelBottom - ToolsBottom:0.#} px), next-level ends at {NextBottom:0.#}, message {MessageHeight:0.#} px high, viewport {Viewport:0.#}");
    }
}
