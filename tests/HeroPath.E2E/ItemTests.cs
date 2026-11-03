using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class ItemTests(AppFixture app)
{
    private static readonly string[] _pizzaDetour =
    [
        "right", "right", "right", "right", "up", "up", "up", "left", "left", "right", "right", "right", "up", "up",
    ];

    private static readonly string[] _withoutStrawberries =
    [
        "right", "up", "up", "up", "up", "right", "right", "right", "right", "up",
    ];

    [Theory]
    [InlineData("de-CH", "de", "Erdbeeren 0 von 3", "Erdbeeren 1 von 3", "Pizza 0 von 1", "1 Pizza-Stück gefunden")]
    [InlineData("en", "en", "Strawberries 0 of 3", "Strawberries 1 of 3", "Pizza 0 of 1", "1 slice of pizza")]
    public async Task Strawberries_ShowCollectCountAndPizzaMessage(
        string locale, string language, string startText, string collectedText, string pizzaText, string successText)
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync(locale, 1366, 768);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.StartGameAsync(errors, "strawberries", "lisa", "Zoe");
        await page.GetByTestId($"language-{language}").ClickAsync();

        await Expect(page.Locator("[data-testid^=item-]:not([data-testid^=item-counter])")).ToHaveCountAsync(4);
        await Expect(page.GetByTestId("item-3-7")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("item-5-6")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("cell-5-6")).ToHaveAttributeAsync("data-item", "pizza");
        await Expect(page.GetByTestId("item-counter-strawberry")).ToContainTextAsync(startText);
        await Expect(page.GetByTestId("item-counter-strawberry")).ToHaveAttributeAsync("data-total", "3");
        await Expect(page.GetByTestId("item-counter-pizza")).ToContainTextAsync(pizzaText);
        await AssertFitsAsync(page);
        await Screenshots.CaptureAsync(page, $"items-start.laptop.{language}");

        await Click(page, "palette-right", 2);
        await page.GetByTestId("control-debug").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "paused");
        await page.GetByTestId("control-debug").ClickAsync();

        await Expect(page.GetByTestId("item-3-7")).ToHaveAttributeAsync("data-collected", "true");
        await Expect(page.GetByTestId("item-3-7")).ToBeHiddenAsync();
        await Expect(page.GetByTestId("item-3-4")).Not.ToHaveAttributeAsync("data-collected", "true");
        await Expect(page.GetByTestId("item-counter-strawberry")).ToHaveAttributeAsync("data-collected", "1");
        await Expect(page.GetByTestId("item-counter-strawberry")).ToContainTextAsync(collectedText);
        await AssertFitsAsync(page);
        await Screenshots.CaptureAsync(page, $"items-collected.laptop.{language}");

        await page.GetByTestId("control-reset").ClickAsync();
        await Expect(page.GetByTestId("item-3-7")).Not.ToHaveAttributeAsync("data-collected", "true");
        await Expect(page.GetByTestId("item-3-7")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("item-counter-strawberry")).ToHaveAttributeAsync("data-collected", "0");

        await page.GetByTestId("program-clear").ClickAsync();
        foreach (var direction in _pizzaDetour)
        {
            await page.GetByTestId($"palette-{direction}").ClickAsync();
        }

        await page.GetByTestId("control-tempo").FillAsync("2");
        await page.GetByTestId("control-play").ClickAsync();

        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded");
        await Expect(page.GetByTestId("item-counter-strawberry")).ToHaveAttributeAsync("data-collected", "3");
        await Expect(page.GetByTestId("item-counter-pizza")).ToHaveAttributeAsync("data-collected", "1");
        await Expect(page.GetByTestId("message")).ToHaveAttributeAsync("data-code", "Succeeded");
        await Expect(page.GetByTestId("message")).ToContainTextAsync("Zoe");
        await Expect(page.GetByTestId("message")).ToContainTextAsync(successText);
        await AssertFitsAsync(page);
        await Screenshots.CaptureAsync(page, $"items-success.laptop.{language}");

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task Goal_WithoutAllStrawberries_ShowsItemsMissing()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync("de-CH", 1920, 1080);
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.StartGameAsync(errors, "strawberries");
        foreach (var direction in _withoutStrawberries)
        {
            await page.GetByTestId($"palette-{direction}").ClickAsync();
        }

        await page.GetByTestId("control-tempo").FillAsync("2");
        await page.GetByTestId("control-play").ClickAsync();

        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "failed");
        await Expect(page.GetByTestId("message")).ToHaveAttributeAsync("data-code", "ItemsMissing");
        await Expect(page.GetByTestId("item-counter-strawberry")).ToHaveAttributeAsync("data-collected", "0");
        await Screenshots.CaptureAsync(page, "items-missing.desktop.de");

        Assert.Empty(errors.Errors);
    }

    private static async Task Click(IPage page, string testId, int times)
    {
        for (var i = 0; i < times; i++)
        {
            await page.GetByTestId(testId).ClickAsync();
        }
    }

    private static async Task AssertFitsAsync(IPage page)
    {
        var scrolls = await page.EvaluateAsync<bool>(
            "document.documentElement.scrollHeight > window.innerHeight || document.documentElement.scrollWidth > window.innerWidth");
        Assert.False(scrolls, "The play page must fit a 1366x768 screen without page scrolling.");

        var board = await page.GetByTestId("game-board").BoundingBoxAsync();
        Assert.NotNull(board);
        Assert.True(board.Y + board.Height <= 768, "The board must end inside the viewport.");
        var counter = await page.GetByTestId("item-counter-strawberry").BoundingBoxAsync();
        Assert.NotNull(counter);
        Assert.True(counter.X + counter.Width <= board.X + board.Width + 1, "The counter must stay above the board width.");
    }
}
