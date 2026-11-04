using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

public class LanguageTests(AppFixture app)
{
    [Fact]
    public async Task Switch_ChangesTexts_AndPersists()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=app-title]", errors);
        await Expect(page.GetByTestId("app-title")).ToHaveTextAsync("HeldenWeg");
        await Expect(page.GetByTestId("language-de")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "de-CH");
        Assert.Equal("de-CH", await page.EvaluateAsync<string>("() => document.documentElement.lang"));
        await Screenshots.CaptureAsync(page, "language.start-page.desktop.de");

        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("app-title")).ToHaveTextAsync("Hero's Path");
        await Expect(page.GetByTestId("language-en")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(page).ToHaveTitleAsync("Hero's Path");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "en");
        await Screenshots.CaptureAsync(page, "language.start-page.desktop.en");

        await page.ReloadAsync();
        await page.WaitForRenderedAsync("[data-testid=app-title]", errors);
        await Expect(page.GetByTestId("app-title")).ToHaveTextAsync("Hero's Path");
        await Expect(page.GetByTestId("language-en")).ToHaveAttributeAsync("aria-pressed", "true");

        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "en");

        await page.GetByTestId("language-de").ClickAsync();
        await Expect(page.GetByTestId("app-title")).ToHaveTextAsync("HeldenWeg");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "de-CH");

        Assert.Empty(errors.Errors);
    }
}
