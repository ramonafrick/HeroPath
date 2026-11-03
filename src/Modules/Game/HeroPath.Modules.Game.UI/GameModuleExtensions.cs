using HeroPath.Modules.Game.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HeroPath.Modules.Game.UI;

public static class GameModuleExtensions
{
    /// <summary>Registers the Game module: game session, execution runner.</summary>
    public static IServiceCollection AddGameModule(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<GameSession>();
        return services;
    }
}
