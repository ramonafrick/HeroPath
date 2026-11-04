using System.Text.Json;
using HeroPath.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace HeroPath.E2E;

/// <summary>
/// Runs against a published site (CSP on, service worker on): <c>finish-site.ps1 -BaseHref / -Pwa</c>, served on
/// <c>http://localhost:port/</c> (service workers need a secure context, localhost counts). Opt-in through
/// HEROPATH_PUBLISHED_URL, plus HEROPATH_E2E=1 and HEROPATH_BASE_URL with the same URL so the fixture does not start the dev host.
/// </summary>
public class PublishedSiteTests(AppFixture app)
{
    private static readonly string[] _easy2Solution = ["up", "up", "right", "right", "right", "right", "right", "up"];

    private const string StoredLevels = """
        [
          {"schemaVersion":1,"id":"custom-e2epub1","name":"Kaese-Weg","width":5,"height":5,
           "start":{"x":0,"y":4},"goal":{"x":2,"y":4},"requireAllItems":false,
           "obstacles":[{"x":1,"y":4,"kind":"tree"}],"items":[{"x":0,"y":2,"kind":"strawberry"}]}
        ]
        """;

    // A CSP violation is reported as a console error by the browser; this also names the directive, and it catches
    // violations that the browser only reports as an event.
    private const string ReportViolationsScript = """
        document.addEventListener('securitypolicyviolation', e =>
            console.error('CSP violation: ' + e.effectiveDirective + ' blocked ' + e.blockedURI));
        """;

    private const string ActivatedScript = """
        async () => {
            const registration = await navigator.serviceWorker.ready;
            return registration.active !== null && registration.active.state === 'activated';
        }
        """;

    private static void SkipUnlessPublished() =>
        Assert.SkipUnless(
            E2EEnvironment.Enabled && E2EEnvironment.PublishedUrl is not null,
            "Set HEROPATH_E2E=1, HEROPATH_BASE_URL and HEROPATH_PUBLISHED_URL to run the published-site tests.");

    private async Task<IBrowserContext> NewContextAsync() =>
        await app.Browser.NewContextAsync(new()
        {
            BaseURL = E2EEnvironment.PublishedUrl,
            Locale = "de-CH",
            ViewportSize = new() { Width = 1366, Height = 768 },
            ReducedMotion = ReducedMotion.Reduce,
        });

    private static async Task SolveAsync(IPage page)
    {
        foreach (var direction in _easy2Solution)
        {
            await page.GetByTestId($"palette-{direction}").ClickAsync();
        }

        await page.GetByTestId("control-tempo").FillAsync("2");
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded");
    }

    [Fact]
    public async Task Site_RunsALevelEditorExportLanguageAndSound_WithoutCspViolations()
    {
        SkipUnlessPublished();

        await using var context = await NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await page.AddInitScriptAsync(ReportViolationsScript);
        await page.AddInitScriptAsync(
            $"if (localStorage.getItem('heropath.levels') === null) localStorage.setItem('heropath.levels', {JsonSerializer.Serialize(StoredLevels)});");

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        var html = await page.ContentAsync();
        Assert.Contains("http-equiv=\"Content-Security-Policy\"", html);
        Assert.Contains("name=\"heropath-pwa\"", html);
        var policy = await page.EvaluateAsync<string>("() => document.querySelector('meta[http-equiv=\"Content-Security-Policy\"]').content");
        var scriptSrc = policy.Split(';').Select(directive => directive.Trim()).Single(directive => directive.StartsWith("script-src ", StringComparison.Ordinal));
        Assert.DoesNotContain("unsafe-inline", scriptSrc);
        Assert.DoesNotContain("unsafe-eval", scriptSrc.Replace("wasm-unsafe-eval", string.Empty));

        await page.GetByTestId("sound-toggle").ClickAsync();
        await Expect(page.GetByTestId("sound-toggle")).ToHaveAttributeAsync("aria-pressed", "true");
        await page.StartGameAsync(errors);
        await SolveAsync(page);
        await Expect(page.GetByTestId("success-confetti")).ToBeVisibleAsync();

        await page.GetByTestId("language-en").ClickAsync();
        await Expect(page.GetByTestId("app-title")).ToHaveTextAsync("Hero's Path");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "en");
        await page.GetByTestId("language-de").ClickAsync();
        await Expect(page.GetByTestId("app-title")).ToHaveTextAsync("HeldenWeg");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "de-CH");

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.GetByTestId("start-build").ClickAsync();
        await page.WaitForRenderedAsync("[data-testid=editor-page]", errors);
        await Expect(page.GetByTestId("edit-cell-0-0")).ToBeVisibleAsync();
        await page.GetByTestId("editor-tool-rock").ClickAsync();
        await page.GetByTestId("edit-cell-5-5").ClickAsync();
        var download = await page.RunAndWaitForDownloadAsync(() => page.GetByTestId("custom-level-export-custom-e2epub1").ClickAsync());
        Assert.EndsWith(".heropath.json", download.SuggestedFilename);
        await Expect(page.GetByTestId("editor-export-failed")).ToHaveCountAsync(0);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task InjectedInlineScript_IsBlocked()
    {
        SkipUnlessPublished();

        await using var context = await NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);

        var result = await page.EvaluateAsync<JsonElement>("""
            () => new Promise(resolve => {
                document.addEventListener('securitypolicyviolation', e => resolve({ blocked: true, directive: e.effectiveDirective }));
                const script = document.createElement('script');
                script.textContent = 'window.__injected = true';
                document.head.appendChild(script);
                setTimeout(() => resolve({ blocked: false, directive: '' }), 3000);
            })
            """);
        Assert.True(result.GetProperty("blocked").GetBoolean(), "The inline script was not reported as blocked.");
        Assert.StartsWith("script-src", result.GetProperty("directive").GetString());
        Assert.False(await page.EvaluateAsync<bool>("() => window.__injected === true"), "The injected inline script ran.");


        await Expect(page.GetByTestId("start-page")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ManifestAndIcons_Load()
    {
        SkipUnlessPublished();

        await using var context = await NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await page.AddInitScriptAsync(ReportViolationsScript);
        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);

        var href = await page.Locator("link[rel=manifest]").GetAttributeAsync("href");
        Assert.Equal("manifest.webmanifest", href);
        var manifestResponse = await page.APIRequest.GetAsync(new Uri(new Uri(E2EEnvironment.PublishedUrl!), href!).ToString());
        Assert.True(manifestResponse.Ok);
        using var manifest = JsonDocument.Parse(await manifestResponse.TextAsync());
        var root = manifest.RootElement;
        Assert.Equal("HeldenWeg", root.GetProperty("name").GetString());
        Assert.Equal("standalone", root.GetProperty("display").GetString());
        Assert.Equal("./", root.GetProperty("start_url").GetString());
        Assert.Equal("./", root.GetProperty("scope").GetString());

        var sizes = new List<int>();
        foreach (var icon in root.GetProperty("icons").EnumerateArray())
        {
            var response = await page.APIRequest.GetAsync(new Uri(new Uri(E2EEnvironment.PublishedUrl!), icon.GetProperty("src").GetString()).ToString());
            Assert.True(response.Ok, $"Icon {icon.GetProperty("src")} did not load.");
            var bytes = await response.BodyAsync();
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes[..4]);
            var width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            var height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            Assert.Equal(width, height);
            sizes.Add(width);
        }

        Assert.Equal([192, 512], sizes.Order());
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task ServiceWorker_Activates_AndTheInstalledAppRunsOffline()
    {
        SkipUnlessPublished();

        await using var context = await NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);
        await page.AddInitScriptAsync(ReportViolationsScript);

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await page.WaitForFunctionAsync(ActivatedScript, null, new() { Timeout = 120_000 });

        var cacheNames = await page.EvaluateAsync<string[]>("() => caches.keys()");
        var cacheName = Assert.Single(cacheNames);
        Assert.StartsWith("heropath-cache-", cacheName);
        var cachedFiles = await page.EvaluateAsync<int>("async name => (await (await caches.open(name)).keys()).length", cacheName);
        var manifestFiles = await page.EvaluateAsync<int>("async () => { const text = await (await fetch('service-worker-assets.js')).text(); return (text.match(/\"url\":/g) || []).length; }");
        Assert.Equal(manifestFiles, cachedFiles);

        // The first page load is not controlled (the worker does not call clients.claim), the reload is.
        await page.ReloadAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        Assert.True(await page.EvaluateAsync<bool>("() => navigator.serviceWorker.controller !== null"));

        await context.SetOfflineAsync(true);

        await page.ReloadAsync();
        await page.WaitForRenderedAsync("[data-testid=start-page]", errors);
        await Expect(page.GetByTestId("app-title")).ToHaveTextAsync("HeldenWeg");
        await Screenshots.CaptureAsync(page, "published-offline.start-page.laptop.de");

        await page.GotoAsync("/editor");
        await page.WaitForRenderedAsync("[data-testid=editor-page]", errors);
        await Expect(page.GetByTestId("edit-cell-0-0")).ToBeVisibleAsync();
        await Screenshots.CaptureAsync(page, "published-offline.editor-deep-link.laptop.de");

        await page.StartGameAsync(errors);
        await SolveAsync(page);
        await Screenshots.CaptureAsync(page, "published-offline.success.laptop.de");

        await context.SetOfflineAsync(false);
        Assert.Empty(errors.Errors);
    }
}
