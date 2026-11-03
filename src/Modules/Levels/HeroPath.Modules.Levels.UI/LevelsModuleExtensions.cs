using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Application.Contracts;
using HeroPath.Modules.Levels.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HeroPath.Modules.Levels.UI;

public static class LevelsModuleExtensions
{
    /// <summary>
    /// Registers the Levels module: the storage-backed custom level store and the scoped level repository (standard levels plus custom levels).
    /// Needs an <c>IAppStorage</c> from the host. Safe to call more than once.
    /// </summary>
    public static IServiceCollection AddLevelsModule(this IServiceCollection services)
    {
        services.TryAddScoped<ICustomLevelStore, StorageCustomLevelStore>();
        services.TryAddScoped(provider => new LevelRepository(
            provider.GetRequiredService<ICustomLevelStore>(),
            StandardLevels.All,
            LevelJson.Read));
        return services;
    }

    /// <summary>Loads the custom levels from storage. Call after <c>Build()</c> and before <c>RunAsync()</c>, so the first render already knows them.</summary>
    public static Task LoadCustomLevelsAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.GetRequiredService<LevelRepository>().LoadAsync(cancellationToken);
}
