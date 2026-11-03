using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Levels.Tests.Infrastructure;

/// <summary>Test-only breadth-first search over a grid (4 directions, obstacles block).</summary>
internal static class LevelSolver
{
    private static readonly Position[] _offsets = [new(0, -1), new(0, 1), new(-1, 0), new(1, 0)];

    /// <summary>Shortest number of moves from <paramref name="from"/> to <paramref name="to"/>, or <see langword="null"/> if unreachable.</summary>
    public static int? ShortestPath(Grid grid, Position from, Position to)
    {
        var distances = new Dictionary<Position, int> { [from] = 0 };
        var queue = new Queue<Position>([from]);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == to)
            {
                return distances[current];
            }

            foreach (var offset in _offsets)
            {
                var next = new Position(current.X + offset.X, current.Y + offset.Y);
                if (grid.IsInside(next) && grid.GetObstacle(next) is null && distances.TryAdd(next, distances[current] + 1))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return null;
    }

    /// <summary>The goal is reachable and, with <c>RequireAllItems</c>, so is every strawberry.</summary>
    public static bool IsSolvable(Grid grid)
    {
        if (ShortestPath(grid, grid.Start, grid.Goal) is null)
        {
            return false;
        }

        return !grid.RequireAllItems
            || grid.Items.Where(item => item.Kind == ItemKind.Strawberry)
                .All(item => ShortestPath(grid, grid.Start, item.GetPosition()) is not null);
    }
}
