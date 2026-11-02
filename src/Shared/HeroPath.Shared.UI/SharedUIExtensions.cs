using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Shared.UI;

public static class SharedUIExtensions
{
    /// <summary>Registers cross-module UI services (layout, localization). Platform services are registered by the host.</summary>
    public static IServiceCollection AddSharedUI(this IServiceCollection services)
    {
        services.AddLocalization();
        return services;
    }
}
