namespace HeroPath.Modules.Levels.Domain;

/// <summary>
/// Breadth-first search over a grid (4 directions, obstacles block). State is the cell plus the set of collected
/// strawberries, stored in a flat <c>int[]</c> indexed by <c>cell * masks + mask</c>. Pizzas never count.
/// </summary>
public static class LevelSolver
{
    private static readonly (int X, int Y)[] _offsets = [(0, -1), (0, 1), (-1, 0), (1, 0)];

    /// <summary>
    /// Fewest moves that end on the goal, or <see langword="null"/> if impossible. With <c>RequireAllItems</c> every
    /// strawberry must be visited first; passing the goal earlier does not end the run (same rule as the interpreter).
    /// </summary>
    public static int? ShortestSolution(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        return Search(grid, grid.RequireAllItems);
    }

    /// <summary>Whether the goal can be reached at all, ignoring items.</summary>
    public static bool CanReachGoal(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        return Search(grid, requireStrawberries: false) is not null;
    }

    private static int? Search(Grid grid, bool requireStrawberries)
    {
        var width = grid.Width;
        var cells = width * grid.Height;
        var strawberryIndex = new int[cells];
        Array.Fill(strawberryIndex, -1);
        var strawberryCount = 0;
        if (requireStrawberries)
        {
            foreach (var item in grid.Items.Where(item => item.Kind == ItemKind.Strawberry))
            {
                strawberryIndex[item.Y * width + item.X] = strawberryCount++;
            }
        }

        var masks = 1 << strawberryCount;
        var full = masks - 1;
        var blocked = new bool[cells];
        foreach (var position in grid.Obstacles.Keys)
        {
            blocked[position.Y * width + position.X] = true;
        }

        var distances = new int[cells * masks];
        Array.Fill(distances, -1);
        var queue = new int[distances.Length];
        var head = 0;
        var tail = 0;
        var goal = grid.Goal.Y * width + grid.Goal.X;
        var startState = (grid.Start.Y * width + grid.Start.X) * masks;
        distances[startState] = 0;
        queue[tail++] = startState;

        while (head < tail)
        {
            var state = queue[head++];
            var cell = state / masks;
            var mask = state % masks;
            if (cell == goal && mask == full)
            {
                return distances[state];
            }

            var x = cell % width;
            var y = cell / width;
            foreach (var (dx, dy) in _offsets)
            {
                var nx = x + dx;
                var ny = y + dy;
                if (nx < 0 || nx >= width || ny < 0 || ny >= grid.Height)
                {
                    continue;
                }

                var next = ny * width + nx;
                if (blocked[next])
                {
                    continue;
                }

                var nextMask = strawberryIndex[next] >= 0 ? mask | (1 << strawberryIndex[next]) : mask;
                var nextState = next * masks + nextMask;
                if (distances[nextState] >= 0)
                {
                    continue;
                }

                distances[nextState] = distances[state] + 1;
                queue[tail++] = nextState;
            }
        }

        return null;
    }
}
