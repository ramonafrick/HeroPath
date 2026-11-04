namespace HeroPath.E2E.Infrastructure;

/// <summary>E2E tests only run when HEROPATH_E2E=1, so the default test run stays fast and needs no browser.</summary>
public static class E2EEnvironment
{
    public static bool Enabled => Environment.GetEnvironmentVariable("HEROPATH_E2E") == "1";

    /// <summary>Optional: test an already running app (e.g. GitHub Pages) instead of starting one.</summary>
    public static string? BaseUrl => Environment.GetEnvironmentVariable("HEROPATH_BASE_URL");

    /// <summary>Opt-in for <c>PublishedSiteTests</c>: URL of a site produced by <c>deploy/finish-site.ps1 -BaseHref / -Pwa</c> and served on <c>http://localhost:port/</c>.</summary>
    public static string? PublishedUrl => Environment.GetEnvironmentVariable("HEROPATH_PUBLISHED_URL");

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string ScreenshotDirectory => Path.Combine(RepositoryRoot, "artifacts", "screenshots");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HeroPath.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("HeroPath.slnx not found above the test output directory.");
    }
}
