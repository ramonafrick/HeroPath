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
}
