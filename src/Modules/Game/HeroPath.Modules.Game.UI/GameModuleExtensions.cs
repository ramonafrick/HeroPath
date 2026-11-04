using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI.Services;
using HeroPath.Modules.Levels.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HeroPath.Modules.Game.UI;

/// <summary>Registration of the Game module in the DI container.</summary>
public static class GameModuleExtensions
{
    /// <summary>Registers the Game module (and the Levels module it needs): hero choice, level progress, game session, execution runner, drag state.</summary>
    public static IServiceCollection AddGameModule(this IServiceCollection services)
    {
        services.AddLevelsModule();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<HeroChoice>();
        services.AddScoped<LevelProgress>();
        services.AddScoped<GameSession>();
        services.AddScoped<ExecutionRunner>();
        services.AddScoped<RunSounds>();
        services.AddScoped<ProgramDragState>();
        return services;
    }
}
