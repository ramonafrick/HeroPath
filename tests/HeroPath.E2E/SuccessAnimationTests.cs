using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class SuccessAnimationTests(AppFixture app)
{
    private const string Confetti = "success-confetti";

    private const string FreezeMidAnimation = """
        () => {
            const root = document.querySelector('[data-testid=success-confetti]');
            let frozen = 0;
            for (const animation of document.getAnimations()) {
                if (root && root.contains(animation.effect?.target)) {
                    animation.pause();
                    animation.currentTime = 900;
                    frozen++;
                } else {
                    animation.finish();
                }
            }
            return frozen;
        }
        """;

    [Fact]
    public async Task Solve_ShowsOverlay_NextLevelClickableWhileAnimating_ResetRemovesIt()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768, reducedMotion: false);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.StartGameAsync(errors, "easy-1");
        await Expect(page.GetByTestId(Confetti)).ToHaveCountAsync(0);
        await SolveEasy1Async(page);

        await Expect(page.GetByTestId(Confetti)).ToBeVisibleAsync();
        Assert.Equal("none", await page.GetByTestId(Confetti).EvaluateAsync<string>("e => getComputedStyle(e).pointerEvents"));
        Assert.True(await page.EvaluateAsync<int>("() => document.querySelectorAll('[data-testid=success-confetti] .piece').length") > 0);

        await page.GetByTestId(Confetti).EvaluateAsync("e => { e.dataset.probe = 'same'; e.expando = e; }");
        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("next-level")).ToHaveTextAsync("Next level");
        Assert.True(await page.GetByTestId(Confetti).EvaluateAsync<bool>("e => e.expando === e"));

        await page.GetByTestId("next-level").ClickAsync();
        await Expect(page.GetByTestId("level-name")).ToHaveTextAsync("Easy 2");
        await Expect(page.GetByTestId(Confetti)).ToHaveCountAsync(0);

        await page.GetByTestId("level-picker").SelectOptionAsync("easy-1");
        await SolveEasy1Async(page);
        await Expect(page.GetByTestId(Confetti)).ToBeVisibleAsync();
        await page.GetByTestId("control-reset").ClickAsync();
        await Expect(page.GetByTestId(Confetti)).ToHaveCountAsync(0);

        Assert.Empty(errors.Errors);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    public async Task Confetti_Screenshot_NormalMotion(string language)
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync(language == "en" ? "en" : "de-CH", 1366, 768, reducedMotion: false);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.StartGameAsync(errors, "easy-1");
        if (language == "en")
        {
            await page.GetByTestId("language-en").ClickAsync();
        }

        await SolveEasy1Async(page);
        await Expect(page.GetByTestId(Confetti)).ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<int>(FreezeMidAnimation) > 0);

        await Screenshots.CaptureAsync(page, $"success-confetti.laptop.{language}");
        Assert.Empty(errors.Errors);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    public async Task ReducedMotion_ShowsCalmAccentWithoutPieces(string language)
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync(language == "en" ? "en" : "de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.StartGameAsync(errors, "easy-1");
        if (language == "en")
        {
            await page.GetByTestId("language-en").ClickAsync();
        }

        await SolveEasy1Async(page);
        await Expect(page.GetByTestId(Confetti)).ToBeVisibleAsync();
        Assert.Equal("none", await page.EvaluateAsync<string>("() => getComputedStyle(document.querySelector('[data-testid=success-confetti] .piece')).display"));
        Assert.NotEqual("none", await page.GetByTestId(Confetti).EvaluateAsync<string>("e => getComputedStyle(e).boxShadow"));

        await Screenshots.CaptureAsync(page, $"success-reduced-motion.laptop.{language}");
        Assert.Empty(errors.Errors);
    }

    private static async Task SolveEasy1Async(IPage page)
    {
        foreach (var direction in new[] { "right", "right", "up", "right", "right", "right", "down" })
        {
            await page.GetByTestId($"palette-{direction}").ClickAsync();
        }

        await page.GetByTestId("control-tempo").FillAsync("2");
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded");
    }
}
