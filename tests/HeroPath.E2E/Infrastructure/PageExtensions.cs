namespace HeroPath.E2E.Infrastructure;

public static class PageExtensions
{
    /// <summary>
    /// Waits until Blazor has rendered <paramref name="selector"/>. On timeout a failure screenshot is saved
    /// and the collected browser errors become part of the exception message.
    /// </summary>
    public static async Task WaitForRenderedAsync(this IPage page, string selector, BrowserErrors errors)
    {
        try
        {
            await page.Locator(selector).WaitForAsync();
        }
        catch (TimeoutException exception)
        {
            Directory.CreateDirectory(E2EEnvironment.ScreenshotDirectory);
            await page.ScreenshotAsync(new() { Path = Path.Combine(E2EEnvironment.ScreenshotDirectory, "failure.png"), FullPage = true });
            var details = errors.Errors.Count == 0 ? "no browser errors" : string.Join(Environment.NewLine, errors.Errors);
            throw new TimeoutException($"'{selector}' was not rendered. Browser errors:{Environment.NewLine}{details}", exception);
        }
    }

    /// <summary>
    /// Opens the start page, optionally picks a hero (<c>max</c> or <c>lisa</c>) and a name, picks the level and clicks «Los!».
    /// Waits until the play page shows its board. Most E2E tests rely on the layout of <c>easy-2</c>.
    /// </summary>
    public static async Task StartGameAsync(
        this IPage page,
        BrowserErrors errors,
        string levelId = "easy-2",
        string? hero = null,
        string? name = null)
    {
        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        if (hero is not null)
        {
            await page.GetByTestId($"hero-{hero}").ClickAsync();
        }

        if (name is not null)
        {
            await page.GetByTestId("hero-name").FillAsync(name);
        }

        await page.GetByTestId($"level-card-{levelId}").ClickAsync();
        await page.GetByTestId("start-go").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
    }
}
