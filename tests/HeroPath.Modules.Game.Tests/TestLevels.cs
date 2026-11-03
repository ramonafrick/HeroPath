using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.Infrastructure;

namespace HeroPath.Modules.Game.Tests;

internal static class TestLevels
{
    public static Grid DemoGrid() => StandardLevels.Get("demo").ToGrid();

    public static void Fill(GameSession session, params Direction[] directions)
    {
        foreach (var direction in directions)
        {
            session.Add(direction);
        }
    }

    /// <summary>The shortest solution of the demo level: Up, Up, Right x5, Up.</summary>
    public static Direction[] DemoSolution { get; } =
        [Direction.Up, Direction.Up, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Up];
}
