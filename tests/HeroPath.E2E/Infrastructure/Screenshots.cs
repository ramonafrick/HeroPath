namespace HeroPath.E2E.Infrastructure;

/// <summary>Writes screenshots to artifacts/screenshots for the visual-checker agent.</summary>
public static class Screenshots
{
    /// <summary>Saves a normal and a greyscale (achromatopsia) screenshot: start/goal etc. must stay distinguishable without colour.</summary>
    public static async Task CaptureAsync(IPage page, string name)
    {
        Directory.CreateDirectory(E2EEnvironment.ScreenshotDirectory);

        await page.ScreenshotAsync(new() { Path = PathFor(name), FullPage = true });

        var cdp = await page.Context.NewCDPSessionAsync(page);
        await cdp.SendAsync("Emulation.setEmulatedVisionDeficiency", new() { ["type"] = "achromatopsia" });
        await page.ScreenshotAsync(new() { Path = PathFor(name + ".greyscale"), FullPage = true });
        await cdp.SendAsync("Emulation.setEmulatedVisionDeficiency", new() { ["type"] = "none" });
    }

    private static string PathFor(string name) => Path.Combine(E2EEnvironment.ScreenshotDirectory, name + ".png");
}
