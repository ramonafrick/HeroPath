using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class RepeatRunTests(AppFixture app)
{
    [Fact]
    public async Task Debug_ShowsRound_ThenPlaySucceeds()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var desktop = await app.NewContextAsync();
        await RunAsync(await desktop.NewPageAsync(), "desktop");

        await using var laptop = await app.NewContextAsync("de-CH", 1366, 768);
        await RunAsync(await laptop.NewPageAsync(), "laptop");
    }

    private static async Task RunAsync(IPage page, string viewport)
    {
        var errors = BrowserErrors.Track(page);
        await page.StartGameAsync(errors);

        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("palette-repeat").ClickAsync();
        await page.GetByTestId("program-select-3").ClickAsync();
        await page.GetByTestId("palette-right").ClickAsync();
        await page.GetByTestId("program-select-3").ClickAsync();
        await page.GetByTestId("palette-right").ClickAsync();
        await page.GetByTestId("palette-right").ClickAsync();
        await page.GetByTestId("palette-up").ClickAsync();
        await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(7);
        await Expect(page.GetByTestId("repeat-count-3")).ToHaveTextAsync("3");
        await page.GetByTestId("control-tempo").FillAsync("2");

        for (var step = 0; step < 4; step++)
        {
            await page.GetByTestId("control-debug").ClickAsync();
        }

        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "paused");
        var repeat = page.Locator("[data-testid=program-block][data-path='3']");
        await Expect(repeat).ToHaveAttributeAsync("data-active-repeat", "true");
        await Expect(page.GetByTestId("repeat-round-3")).ToHaveTextAsync("Runde 2 von 3");
        await Expect(page.Locator("[data-testid=program-block][data-path='3.1']")).ToHaveAttributeAsync("data-current", "true");
        await Expect(page.Locator("[data-testid=program-block][data-selected=true]")).ToHaveCountAsync(0);
        await Screenshots.CaptureAsync(page, $"run.repeat-round.{viewport}.de");

        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("repeat-round-3")).ToHaveTextAsync("Round 2 of 3");
        await Screenshots.CaptureAsync(page, $"run.repeat-round.{viewport}.en");
        await page.GetByTestId("language-de").ClickAsync();
        await Expect(page.GetByTestId("repeat-round-3")).ToHaveTextAsync("Runde 2 von 3");

        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded");
        await Expect(page.GetByTestId("hero")).ToHaveAttributeAsync("data-x", "6");
        await Expect(page.GetByTestId("hero")).ToHaveAttributeAsync("data-y", "3");

        Assert.Empty(errors.Errors);
    }
}
