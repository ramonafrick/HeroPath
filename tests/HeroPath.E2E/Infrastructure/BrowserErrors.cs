namespace HeroPath.E2E.Infrastructure;

/// <summary>Collects console errors and uncaught page errors. A Blazor exception should fail the E2E test, not only show the error bar.</summary>
public sealed class BrowserErrors
{
    private readonly List<string> _errors = [];

    private BrowserErrors(IPage page)
    {
        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                _errors.Add($"console: {message.Text}");
            }
        };
        page.PageError += (_, error) => _errors.Add($"page: {error}");
    }

    public IReadOnlyList<string> Errors => _errors;

    public static BrowserErrors Track(IPage page) => new(page);
}
