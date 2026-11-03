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

    /// <summary>Shortest number of moves that reach the goal after collecting every required strawberry, or <see langword="null"/>.</summary>
    public static int? ShortestSolution(Grid grid)
    {
        var strawberries = grid.RequireAllItems
            ? grid.Items.Where(item => item.Kind == ItemKind.Strawberry).Select(item => item.GetPosition()).ToList()
            : [];
        var full = (1 << strawberries.Count) - 1;
        var start = (grid.Start, 0);
        var distances = new Dictionary<(Position, int), int> { [start] = 0 };
        var queue = new Queue<(Position Position, int Mask)>([start]);
        while (queue.Count > 0)
        {
            var (position, mask) = queue.Dequeue();
            if (position == grid.Goal && mask == full)
            {
                return distances[(position, mask)];
            }

            foreach (var offset in _offsets)
            {
                var next = new Position(position.X + offset.X, position.Y + offset.Y);
                if (!grid.IsInside(next) || grid.GetObstacle(next) is not null)
                {
                    continue;
                }

                var nextMask = mask;
                var index = strawberries.IndexOf(next);
                if (index >= 0)
                {
                    nextMask |= 1 << index;
                }

                if (distances.TryAdd((next, nextMask), distances[(position, mask)] + 1))
                {
                    queue.Enqueue((next, nextMask));
                }
            }
        }

        return null;
    }

    /// <summary>The goal is reachable and, with <c>RequireAllItems</c>, after every strawberry.</summary>
    public static bool IsSolvable(Grid grid)
    {
        return ShortestSolution(grid) is not null;
    }
}
