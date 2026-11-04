using System.Diagnostics;

namespace HeroPath.E2E.Infrastructure;

/// <summary>Starts the web host once per test run (unless HEROPATH_BASE_URL is set) and provides a Chromium browser.</summary>
public sealed class AppFixture : IAsyncLifetime
{
    private const string LocalUrl = "http://127.0.0.1:5199";
    private static readonly TimeSpan _startupTimeout = TimeSpan.FromMinutes(3);

#if DEBUG
    private const string BuildConfiguration = "Debug";
#else
    private const string BuildConfiguration = "Release";
#endif

    private Process? _host;
    private IPlaywright? _playwright;

    public string BaseUrl { get; private set; } = LocalUrl;

    public IBrowser Browser { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        if (!E2EEnvironment.Enabled)
        {
            return;
        }

        // Fresh screenshots per run, so the visual-checker never judges stale images.
        if (Directory.Exists(E2EEnvironment.ScreenshotDirectory))
        {
            Directory.Delete(E2EEnvironment.ScreenshotDirectory, recursive: true);
        }

        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright browser install failed with exit code {exitCode}.");
        }

        if (E2EEnvironment.BaseUrl is { } externalUrl)
        {
            BaseUrl = externalUrl.TrimEnd('/');
        }
        else
        {
            await StartHostAsync();
        }

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_playwright is not null)
        {
            await Browser.DisposeAsync();
            _playwright.Dispose();
        }

        if (_host is { HasExited: false })
        {
            _host.Kill(entireProcessTree: true);
        }

        _host?.Dispose();
    }

    /// <summary>Desktop context matching the event setup (laptop with external screen), reduced motion for stable screenshots.</summary>
    public Task<IBrowserContext> NewContextAsync(string locale = "de-CH", int width = 1920, int height = 1080, bool reducedMotion = true) =>
        Browser.NewContextAsync(new()
        {
            BaseURL = BaseUrl,
            Locale = locale,
            ViewportSize = new() { Width = width, Height = height },
            ReducedMotion = reducedMotion ? ReducedMotion.Reduce : ReducedMotion.NoPreference,
        });

    private async Task StartHostAsync()
    {
        var project = Path.Combine(E2EEnvironment.RepositoryRoot, "src", "Hosts", "HeroPath.Web");
        _host = Process.Start(new ProcessStartInfo("dotnet", ["run", "--project", project, "-c", BuildConfiguration, "--no-launch-profile", "--urls", LocalUrl])
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("Could not start the web host.");
        _host.BeginOutputReadLine();
        _host.BeginErrorReadLine();

        using var http = new HttpClient();
        var deadline = DateTime.UtcNow + _startupTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_host.HasExited)
            {
                throw new InvalidOperationException($"Web host exited early with code {_host.ExitCode}.");
            }

            try
            {
                using var response = await http.GetAsync(LocalUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // not listening yet
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"Web host did not respond on {LocalUrl} within {_startupTimeout}.");
    }
}
