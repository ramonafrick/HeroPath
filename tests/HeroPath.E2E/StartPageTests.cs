using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class StartPageTests(AppFixture app)
{
    [Fact]
    public async Task Start_WithLisaNameAndLevel_ShowsThemOnThePlayPage()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("hero-max")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByTestId("hero-name")).ToHaveValueAsync("Max");
        await Expect(page.GetByTestId("level-card-free-field")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByTestId("level-preview-easy-2")).ToBeVisibleAsync();
        await Screenshots.CaptureAsync(page, "start-page.desktop.de");

        await page.GetByTestId("hero-lisa").ClickAsync();
        await Expect(page.GetByTestId("hero-lisa")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByTestId("hero-name")).ToHaveValueAsync("Lisa");
        await page.GetByTestId("hero-name").FillAsync("Zoe");
        await page.GetByTestId("level-card-easy-1").ClickAsync();
        await Expect(page.GetByTestId("level-card-easy-1")).ToHaveAttributeAsync("aria-pressed", "true");
        await Screenshots.CaptureAsync(page, "start-page-lisa.desktop.de");

        await page.GetByTestId("start-go").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/play$"));
        await Expect(page.GetByTestId("hero-badge-name")).ToHaveTextAsync("Zoe");
        await Expect(page.GetByTestId("level-name")).ToHaveTextAsync("Einfach 1");
        await Expect(page.GetByTestId("hero").GetByTestId("graphic-hero-lisa")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("hero-badge").GetByTestId("graphic-hero-lisa")).ToBeVisibleAsync();
        await Screenshots.CaptureAsync(page, "play-lisa.desktop.de");

        await page.GetByTestId("hero-change").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("hero-lisa")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByTestId("hero-name")).ToHaveValueAsync("Zoe");
        await Expect(page.GetByTestId("level-card-easy-1")).ToHaveAttributeAsync("aria-pressed", "true");

        Assert.Empty(errors.Errors);
    }

    [Theory]
    [InlineData("desktop", 1920, 1080)]
    [InlineData("laptop", 1366, 768)]
    public async Task StartPage_HasNoHorizontalScroll(string viewport, int width, int height)
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", width, height);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("level-card-castle-maze")).ToBeVisibleAsync();

        var scrollsHorizontally = await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > window.innerWidth");
        Assert.False(scrollsHorizontally, "The start page must not scroll horizontally.");
        await Screenshots.CaptureAsync(page, $"start-page.{viewport}.de");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task PlayPage_WithoutChoice_RedirectsToStartPage()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/play");

        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("game-board")).ToHaveCountAsync(0);
        Assert.False(new Uri(page.Url).AbsolutePath.EndsWith("/play", StringComparison.Ordinal));
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task LanguageSwitch_KeepsNameHeroAndLevel()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.GetByTestId("hero-lisa").ClickAsync();
        await page.GetByTestId("hero-name").FillAsync("Zoe");
        await page.GetByTestId("level-card-strawberries").ClickAsync();

        await page.GetByTestId("language-en").ClickAsync();

        await Expect(page.GetByTestId("start-go")).ToHaveTextAsync("Let's go!");
        await Expect(page.GetByTestId("hero-name")).ToHaveValueAsync("Zoe");
        await Expect(page.GetByTestId("hero-lisa")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByTestId("level-card-strawberries")).ToHaveAttributeAsync("aria-pressed", "true");
        await Screenshots.CaptureAsync(page, "start-page.desktop.en");

        Assert.Empty(errors.Errors);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    public async Task StartPage_WithLisaSelected_FitsLaptop(string language)
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.GetByTestId($"language-{language}").ClickAsync();
        await page.GetByTestId("hero-lisa").ClickAsync();
        await page.GetByTestId("hero-name").FillAsync("Zoe");
        await page.GetByTestId("level-card-easy-1").ClickAsync();
        await Expect(page.GetByTestId("hero-lisa")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.GetByTestId("start-go")).ToBeInViewportAsync();

        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > window.innerWidth"));
        await Screenshots.CaptureAsync(page, $"start-page-lisa.laptop.{language}");

        Assert.Empty(errors.Errors);
    }

    [Theory]
    [InlineData("laptop", 1366, 768)]
    [InlineData("desktop", 1920, 1080)]
    public async Task StartPage_Previews_HaveReadableCellsAndEvenLayout(string viewport, int width, int height)
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", width, height);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("level-card-castle-maze")).ToBeVisibleAsync();

        var cells = await page.EvaluateAsync<double[]>(@"() => [...document.querySelectorAll('[data-testid=board-preview]')]
            .map(b => b.getBoundingClientRect().width / parseInt(getComputedStyle(b).getPropertyValue('--cols')))");
        await Screenshots.CaptureAsync(page, $"start-page-previews.{viewport}.de");
        Assert.Equal(7, cells.Length);
        Assert.All(cells, cell => Assert.True(cell >= 14, $"A preview cell is only {cell} px."));

        var previewHeights = await page.EvaluateAsync<double[]>(@"() => [...document.querySelectorAll('[data-testid^=level-preview-]')]
            .map(p => p.getBoundingClientRect().height)");
        Assert.All(previewHeights, h => Assert.Equal(previewHeights[0], h));

        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > window.innerWidth"));
        Assert.Empty(errors.Errors);
    }
}
