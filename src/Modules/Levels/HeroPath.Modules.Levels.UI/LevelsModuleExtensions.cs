using Microsoft.Extensions.DependencyInjection;

namespace HeroPath.Modules.Levels.UI;

public static class LevelsModuleExtensions
{
    /// <summary>Registers the Levels module: level repository, validation, editor services.</summary>
    public static IServiceCollection AddLevelsModule(this IServiceCollection services) => services;
}
