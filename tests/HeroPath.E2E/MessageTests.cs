using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class MessageTests(AppFixture app)
{
    [Fact]
    public async Task Messages_ShowForEveryOutcomeInGermanAndEnglish()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var desktop = await app.NewContextAsync();
        await RunAsync(await desktop.NewPageAsync(), "desktop");

        await using var laptop = await app.NewContextAsync("de-CH", 1366, 768);
        await RunAsync(await laptop.NewPageAsync(), "laptop");
    }

    [Fact]
    public async Task StepLimit_ShowsMessageInGermanAndEnglish()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var desktop = await app.NewContextAsync();
        await RunStepLimitAsync(await desktop.NewPageAsync(), "desktop");

        await using var laptop = await app.NewContextAsync("de-CH", 1366, 768);
        await RunStepLimitAsync(await laptop.NewPageAsync(), "laptop");
    }

    private static async Task RunStepLimitAsync(IPage page, string viewport)
    {
        var errors = BrowserErrors.Track(page);
        await page.Clock.InstallAsync();
        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
        await page.GetByTestId("control-tempo").FillAsync("2");

        await page.GetByTestId("palette-repeat").ClickAsync();
        await page.GetByTestId("program-select-1").ClickAsync();
        await page.GetByTestId("palette-repeat").ClickAsync();
        await page.GetByTestId("program-select-1.1").ClickAsync();
        foreach (var path in new[] { "1", "1.1" })
        {
            for (var index = 0; index < 17; index++)
            {
                await page.GetByTestId($"repeat-plus-{path}").ClickAsync();
            }

            await Expect(page.GetByTestId($"repeat-count-{path}")).ToHaveTextAsync("20");
        }

        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("palette-down").ClickAsync();
        await page.GetByTestId("palette-up").ClickAsync();
        await page.GetByTestId("palette-down").ClickAsync();
        await page.GetByTestId("control-play").ClickAsync();

        // The 1001st move needs about 200 s of tempo delays. The fake browser clock plays them back at once.
        var status = page.GetByTestId("status");
        for (var tick = 0; tick < 1500 && await status.GetAttributeAsync("data-state") == "running"; tick++)
        {
            await page.Clock.RunForAsync(250);
        }

        await Expect(status).ToHaveAttributeAsync("data-state", "failed");
        await Expect(status).ToHaveAttributeAsync("data-error", "StepLimitExceeded");
        var message = page.GetByTestId("message");
        await Expect(message).ToHaveAttributeAsync("data-code", "StepLimitExceeded");
        await Expect(message).ToHaveTextAsync("Puh! Dein Programm macht mehr als 1000 Schritte. Brauchst du wirklich so viele Wiederholungen?");
        await AssertFitsAsync(page, viewport);
        await Screenshots.CaptureAsync(page, $"message.step-limit.{viewport}.de");
        await SwitchToEnglishAsync(page);
        await Expect(message).ToHaveTextAsync("Phew! Your program takes more than 1000 steps. Do you really need that many repeats?");
        await Screenshots.CaptureAsync(page, $"message.step-limit.{viewport}.en");

        Assert.Empty(errors.Errors);
    }

    private static async Task RunAsync(IPage page, string viewport)
    {
        var errors = BrowserErrors.Track(page);
        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
        await page.GetByTestId("control-tempo").FillAsync("2");

        var message = page.GetByTestId("message");
        await Expect(message).ToHaveAttributeAsync("data-code", "EmptyProgram");
        await Expect(message).ToHaveTextAsync("Zieh Bausteine ins Programm oder klick sie an.");
        await Expect(message).ToHaveAttributeAsync("data-kind", "hint");

        await BuildAsync(page, "right", "right");
        await Expect(message).ToHaveCountAsync(0);
        await page.GetByTestId("control-play").ClickAsync();
        await ExpectFailedAsync(page, "Obstacle");
        await Expect(message).ToHaveAttributeAsync("data-kind", "error");
        await Expect(message).ToHaveTextAsync("Autsch! Da steht ein Baum im Weg. Schau dir Baustein 2 noch einmal an.");
        await Expect(page.Locator("[data-testid=program-block][data-path='2']")).ToHaveAttributeAsync("data-error", "true");
        await AssertFitsAsync(page, viewport);
        await Screenshots.CaptureAsync(page, $"message.obstacle.{viewport}.de");
        await SwitchToEnglishAsync(page);
        await Expect(message).ToHaveTextAsync("Ouch! There's a tree in the way. Take another look at block 2.");
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "failed");
        await AssertFitsAsync(page, viewport);
        await Screenshots.CaptureAsync(page, $"message.obstacle.{viewport}.en");
        await SwitchToGermanAsync(page);

        await ResetAndClearAsync(page);
        await BuildAsync(page, "left", "left");
        await page.GetByTestId("control-play").ClickAsync();
        await ExpectFailedAsync(page, "OutOfBounds");
        await Expect(message).ToHaveTextAsync("Hoppla! Hier ist die Welt zu Ende. Baustein 2 führt aus dem Spielfeld.");
        await AssertFitsAsync(page, viewport);
        await Screenshots.CaptureAsync(page, $"message.edge.{viewport}.de");
        await SwitchToEnglishAsync(page);
        await Expect(message).ToHaveTextAsync("Oops! The world ends here. Block 2 leads off the board.");
        await Screenshots.CaptureAsync(page, $"message.edge.{viewport}.en");
        await SwitchToGermanAsync(page);

        await ResetAndClearAsync(page);
        await BuildAsync(page, "up");
        await page.GetByTestId("control-play").ClickAsync();
        await ExpectFailedAsync(page, "GoalNotReached");
        await Expect(message).ToHaveTextAsync("Dein Programm ist fertig, aber Max ist noch nicht bei der Burg. Probier es nochmal!");
        await AssertFitsAsync(page, viewport);
        await Screenshots.CaptureAsync(page, $"message.goal-not-reached.{viewport}.de");
        await SwitchToEnglishAsync(page);
        await Expect(message).ToHaveTextAsync("Your program is done, but Max hasn't reached the castle yet. Try again!");
        await Screenshots.CaptureAsync(page, $"message.goal-not-reached.{viewport}.en");
        await SwitchToGermanAsync(page);

        await ResetAndClearAsync(page);
        await BuildAsync(page, "up");
        await page.GetByTestId("palette-repeat").ClickAsync();
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(message).ToHaveAttributeAsync("data-code", "EmptyRepeat");
        await Expect(message).ToHaveAttributeAsync("data-kind", "hint");
        await Expect(message).ToHaveTextAsync("Leg zuerst Bausteine in den Wiederhole-Block.");
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "ready");
        await Expect(page.Locator("[data-testid=program-block][data-path='2']")).ToHaveAttributeAsync("data-hint", "true");
        await Expect(page.GetByTestId("control-play")).ToBeEnabledAsync();
        await Expect(page.GetByTestId("palette-up")).ToBeEnabledAsync();
        await Expect(page.GetByTestId("hero")).ToHaveAttributeAsync("data-x", "1");
        await AssertFitsAsync(page, viewport);
        await Screenshots.CaptureAsync(page, $"message.empty-repeat.{viewport}.de");
        await SwitchToEnglishAsync(page);
        await Expect(message).ToHaveTextAsync("Put some blocks into the repeat block first.");
        await Screenshots.CaptureAsync(page, $"message.empty-repeat.{viewport}.en");
        await SwitchToGermanAsync(page);

        await page.GetByTestId("control-debug").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "ready");
        await Expect(message).ToHaveAttributeAsync("data-code", "EmptyRepeat");

        await page.GetByTestId("program-select-2").ClickAsync();
        await page.GetByTestId("palette-right").ClickAsync();
        await Expect(message).ToHaveCountAsync(0);
        await Expect(page.Locator("[data-hint=true]")).ToHaveCountAsync(0);
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).Not.ToHaveAttributeAsync("data-state", "ready");

        await ResetAndClearAsync(page);
        await BuildAsync(page, "up", "up", "right", "right", "right", "right", "right", "up");
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded");
        await Expect(message).ToHaveAttributeAsync("data-kind", "success");
        await Expect(message).ToHaveAttributeAsync("data-code", "Succeeded");
        await Expect(message).ToHaveTextAsync("Geschafft, Max! Du hast die Burg erreicht.");
        await AssertFitsAsync(page, viewport);
        await Screenshots.CaptureAsync(page, $"message.success.{viewport}.de");
        await SwitchToEnglishAsync(page);
        await Expect(message).ToHaveTextAsync("You did it, Max! You reached the castle.");
        await Screenshots.CaptureAsync(page, $"message.success.{viewport}.en");
        await SwitchToGermanAsync(page);

        await page.GetByTestId("control-reset").ClickAsync();
        await Expect(message).ToHaveCountAsync(0);
        await page.GetByTestId("program-clear").ClickAsync();

        await page.GetByTestId("palette-repeat").ClickAsync();
        await page.GetByTestId("program-select-1").ClickAsync();
        await page.GetByTestId("palette-repeat").ClickAsync();
        await Expect(message).ToHaveCountAsync(0);
        await page.GetByTestId("program-select-1.1").ClickAsync();
        await Expect(page.GetByTestId("palette-repeat")).ToBeDisabledAsync();
        await Expect(message).ToHaveAttributeAsync("data-code", "NestingTooDeep");
        await Expect(message).ToHaveTextAsync("Hier passt kein weiterer Wiederhole-Block hinein.");
        await AssertFitsAsync(page, viewport);
        await Screenshots.CaptureAsync(page, $"message.nesting-too-deep.{viewport}.de");
        await SwitchToEnglishAsync(page);
        await Expect(message).ToHaveTextAsync("There's no room for another repeat block in here.");
        await Screenshots.CaptureAsync(page, $"message.nesting-too-deep.{viewport}.en");
        await SwitchToGermanAsync(page);

        await page.GetByTestId("program-clear").ClickAsync();
        await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(0);
        await page.GetByTestId("palette-up").EvaluateAsync("button => { for (let i = 0; i < 200; i++) { button.click(); } }");
        await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(200);
        await Expect(message).ToHaveAttributeAsync("data-code", "ProgramFull");
        await Expect(message).ToHaveTextAsync("Dein Programm ist voll. Lösch zuerst einen Baustein.");
        await Expect(page.GetByTestId("palette-up")).ToBeDisabledAsync();
        await AssertFitsAsync(page, viewport);
        await Screenshots.CaptureAsync(page, $"message.program-full.{viewport}.de");
        await SwitchToEnglishAsync(page);
        await Expect(message).ToHaveTextAsync("Your program is full. Delete a block first.");
        await Screenshots.CaptureAsync(page, $"message.program-full.{viewport}.en");

        Assert.Empty(errors.Errors);
    }

    private static async Task BuildAsync(IPage page, params string[] directions)
    {
        foreach (var direction in directions)
        {
            await page.GetByTestId($"palette-{direction}").ClickAsync();
        }
    }

    private static async Task ExpectFailedAsync(IPage page, string code)
    {
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "failed");
        await Expect(page.GetByTestId("message")).ToHaveAttributeAsync("data-code", code);
    }

    private static async Task ResetAndClearAsync(IPage page)
    {
        await page.GetByTestId("control-reset").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "ready");
        await page.GetByTestId("program-clear").ClickAsync();
        await Expect(page.GetByTestId("program-block")).ToHaveCountAsync(0);
    }

    private static async Task SwitchToEnglishAsync(IPage page)
    {
        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("language-en")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    private static async Task SwitchToGermanAsync(IPage page)
    {
        await page.GetByTestId("language-de").ClickAsync();
        await Expect(page.GetByTestId("language-de")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    private static async Task AssertFitsAsync(IPage page, string viewport)
    {
        var clipped = await page.EvaluateAsync<string[]>(@"() => [...document.querySelectorAll('[data-testid=side-panel] button, [data-testid=program-block], [data-testid=message]')]
            .filter(e => e.scrollWidth > e.clientWidth + 1)
            .map(e => e.dataset.testid)");
        Assert.Empty(clipped);

        if (viewport == "laptop")
        {
            var scrolls = await page.EvaluateAsync<bool>(
                "document.documentElement.scrollHeight > window.innerHeight || document.documentElement.scrollWidth > window.innerWidth");
            Assert.False(scrolls, "Board, palette, controls, message and program must fit a 1366x768 screen without page scrolling.");
        }
    }
}
