using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Shared.UI.Localization;

public static class LanguageServiceExtensions
{
    /// <summary>Call after <c>Build()</c> and before <c>RunAsync()</c> so the first render already uses the stored language.</summary>
    public static Task ApplyStoredCultureAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.GetRequiredService<LanguageService>().ApplyStoredAsync(cancellationToken);
}
