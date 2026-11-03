namespace HeroPath.E2E.Infrastructure;

/// <summary>Writes screenshots to artifacts/screenshots for the visual-checker agent.</summary>
public static class Screenshots
{
    /// <summary>
    /// Saves a normal and a greyscale (achromatopsia) screenshot: start/goal etc. must stay distinguishable without colour.
    /// The mouse is moved to the top-left corner first, so a cell the test pointer rested on does not show its hover state;
    /// <paramref name="keepMouse"/> keeps it where it is (needed while a drag is in progress).
    /// </summary>
    public static Task CaptureAsync(IPage page, string name, bool keepMouse = false) => CaptureAsync(page, name, fullPage: true, keepMouse);

    /// <summary>Same as <see cref="CaptureAsync(IPage, string, bool)"/> but only the viewport, i.e. what the child sees without scrolling.</summary>
    public static Task CaptureViewportAsync(IPage page, string name, bool keepMouse = false) => CaptureAsync(page, name, fullPage: false, keepMouse);

    private static async Task CaptureAsync(IPage page, string name, bool fullPage, bool keepMouse)
    {
        Directory.CreateDirectory(E2EEnvironment.ScreenshotDirectory);

        if (!keepMouse)
        {
            await page.Mouse.MoveAsync(0, 0);
            await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve(null))))");
        }

        await page.ScreenshotAsync(new() { Path = PathFor(name), FullPage = fullPage });

        var cdp = await page.Context.NewCDPSessionAsync(page);
        await cdp.SendAsync("Emulation.setEmulatedVisionDeficiency", new() { ["type"] = "achromatopsia" });
        await page.ScreenshotAsync(new() { Path = PathFor(name + ".greyscale"), FullPage = fullPage });
        await cdp.SendAsync("Emulation.setEmulatedVisionDeficiency", new() { ["type"] = "none" });
    }

    private static string PathFor(string name) => Path.Combine(E2EEnvironment.ScreenshotDirectory, name + ".png");
}
