using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class SoundToggleTests(AppFixture app)
{
    private static readonly string[] _demoSolution = ["up", "up", "right", "right", "right", "right", "right", "up"];

    private const string CountOscillatorsScript = """
        window.__oscillators = 0;
        const original = AudioContext.prototype.createOscillator;
        AudioContext.prototype.createOscillator = function () {
            window.__oscillators++;
            return original.call(this);
        };
        """;

    [Fact]
    public async Task Toggle_StartsOff_SwitchesOn_AndSurvivesReload()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=sound-toggle]", errors);
        var toggle = page.GetByTestId("sound-toggle");
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(page.GetByTestId("sound-icon-off")).ToBeVisibleAsync();
        await AssertTargetSizeAsync(page);
        await Screenshots.CaptureAsync(page, "sound-off.laptop.de");

        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByTestId("sound-icon-on")).ToBeVisibleAsync();
        await Screenshots.CaptureAsync(page, "sound-on.laptop.de");

        await page.GetByTestId("language-en").ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-label", "Sound");
        await Expect(toggle).ToHaveAttributeAsync("title", "Turn sound off");
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        await Screenshots.CaptureAsync(page, "sound-on.laptop.en");

        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");
        await Screenshots.CaptureAsync(page, "sound-off.laptop.en");
        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");

        await page.ReloadAsync();
        await page.WaitForRenderedAsync("[data-testid=sound-toggle]", errors);
        await Expect(page.GetByTestId("sound-toggle")).ToHaveAttributeAsync("aria-pressed", "true");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task Play_WithSoundOn_MakesTonesWithoutBrowserErrors()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await page.AddInitScriptAsync(CountOscillatorsScript);

        await page.StartGameAsync(errors);
        await page.GetByTestId("sound-toggle").ClickAsync();
        await Expect(page.GetByTestId("sound-toggle")).ToHaveAttributeAsync("aria-pressed", "true");
        foreach (var direction in _demoSolution)
        {
            await page.GetByTestId($"palette-{direction}").ClickAsync();
        }

        await page.GetByTestId("control-tempo").FillAsync("2");
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded");

        await Expect(page.GetByTestId("sound-toggle")).ToHaveAttributeAsync("aria-pressed", "true");
        await page.WaitForFunctionAsync("() => window.__oscillators >= 3");
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task Play_WithSoundOff_MakesNoTones()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await page.AddInitScriptAsync(CountOscillatorsScript);

        await page.StartGameAsync(errors);
        foreach (var direction in _demoSolution)
        {
            await page.GetByTestId($"palette-{direction}").ClickAsync();
        }

        await page.GetByTestId("control-tempo").FillAsync("2");
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded");

        Assert.Equal(0, await page.EvaluateAsync<int>("() => window.__oscillators"));
        Assert.Empty(errors.Errors);
    }

    private static async Task AssertTargetSizeAsync(IPage page)
    {
        var box = await page.GetByTestId("sound-toggle").BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box.Width >= 44 && box.Height >= 44, $"Sound toggle is {box.Width}x{box.Height}.");
    }

}
