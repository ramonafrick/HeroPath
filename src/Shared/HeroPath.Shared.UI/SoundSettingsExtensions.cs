using HeroPath.Shared.Application;
using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Shared.UI;

public static class SoundSettingsExtensions
{
    /// <summary>Call after <c>Build()</c> and before <c>RunAsync()</c> so the first render already shows the stored sound setting.</summary>
    public static Task ApplyStoredSoundAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.GetRequiredService<SoundSettings>().ApplyStoredAsync(cancellationToken);
}
