using HeroPath.E2E.Infrastructure;

namespace HeroPath.E2E;

public class PlayPageTests(AppFixture app)
{
    [Fact]
    public async Task PlayPage_Loads()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var browserErrors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("main.play-page", browserErrors);
        await Screenshots.CaptureAsync(page, "play-page.desktop.de");

        Assert.Empty(browserErrors.Errors);
    }
}
