using HeroPath.Shared.Application;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HeroPath.Shared.UI;

public static class SharedUIExtensions
{
    /// <summary>
    /// Registers cross-module UI services (layout, localization). Platform services are registered by the host;
    /// <see cref="IDocumentLanguage"/> is silent until the host registers its own after this call.
    /// </summary>
    public static IServiceCollection AddSharedUI(this IServiceCollection services)
    {
        services.AddLocalization();
        services.TryAddScoped<IDocumentLanguage, NullDocumentLanguage>();
        services.AddScoped<LanguageService>();
        services.AddScoped<SoundSettings>();
        return services;
    }
}
