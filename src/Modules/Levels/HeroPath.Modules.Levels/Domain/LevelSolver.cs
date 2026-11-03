namespace HeroPath.Modules.Levels.Domain;

/// <summary>
/// Shortest solution of a grid (4 directions, obstacles block). Distances between the points that matter (start, goal and, with
/// <c>RequireAllItems</c>, every strawberry) come from one breadth-first search each; the cheapest order to visit the strawberries is a
/// dynamic programme over (set of strawberries, last one), so the cost is a few hundred thousand operations even on 20x20 with ten
/// strawberries. Pizzas never count.
/// </summary>
public static class LevelSolver
{
    /// <summary>
    /// Fewest moves that end on the goal, or <see langword="null"/> if impossible. With <c>RequireAllItems</c> every
    /// strawberry must be visited first; passing the goal earlier does not end the run (same rule as the interpreter).
    /// Returns <see langword="null"/> as soon as the goal or a needed strawberry is unreachable.
    /// </summary>
    public static int? ShortestSolution(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var width = grid.Width;
        var start = grid.Start.Y * width + grid.Start.X;
        var goal = grid.Goal.Y * width + grid.Goal.X;
        var neighbours = Neighbours(grid, out var counts);
        var fromStart = Distances(grid, start, neighbours, counts);
        if (fromStart[goal] < 0)
        {
            return null;
        }

        if (!grid.RequireAllItems)
        {
            return fromStart[goal];
        }

        var points = new List<int>();
        foreach (var item in grid.Items)
        {
            var cell = item.Y * width + item.X;
            if (item.Kind == ItemKind.Strawberry && cell != goal)
            {
                points.Add(cell);
            }
        }

        var count = points.Count;
        if (count == 0)
        {
            return fromStart[goal];
        }

        // between[i] holds the distances from strawberry i; the goal is the last entry of every row.
        var between = new int[count][];
        for (var i = 0; i < count; i++)
        {
            if (fromStart[points[i]] < 0)
            {
                return null;
            }

            between[i] = Distances(grid, points[i], neighbours, counts);
        }

        var masks = 1 << count;
        var best = new int[masks * count];
        Array.Fill(best, int.MaxValue);
        for (var i = 0; i < count; i++)
        {
            best[(1 << i) * count + i] = fromStart[points[i]];
        }

        for (var mask = 1; mask < masks; mask++)
        {
            for (var last = 0; last < count; last++)
            {
                var soFar = best[mask * count + last];
                if (soFar == int.MaxValue)
                {
                    continue;
                }

                for (var next = 0; next < count; next++)
                {
                    if ((mask & (1 << next)) != 0)
                    {
                        continue;
                    }

                    var index = (mask | (1 << next)) * count + next;
                    var cost = soFar + between[last][points[next]];
                    if (cost < best[index])
                    {
                        best[index] = cost;
                    }
                }
            }
        }

        var shortest = int.MaxValue;
        for (var last = 0; last < count; last++)
        {
            shortest = Math.Min(shortest, best[(masks - 1) * count + last] + between[last][goal]);
        }

        return shortest;
    }

    /// <summary>Whether the goal can be reached at all, ignoring items.</summary>
    public static bool CanReachGoal(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var neighbours = Neighbours(grid, out var counts);
        var start = grid.Start.Y * grid.Width + grid.Start.X;
        return Distances(grid, start, neighbours, counts)[grid.Goal.Y * grid.Width + grid.Goal.X] >= 0;
    }

    private static int[] Distances(Grid grid, int origin, int[] neighbours, int[] counts)
    {
        var cells = grid.Width * grid.Height;
        var distances = new int[cells];
        Array.Fill(distances, -1);
        var queue = new int[cells];
        var head = 0;
        var tail = 0;
        distances[origin] = 0;
        queue[tail++] = origin;
        while (head < tail)
        {
            var cell = queue[head++];
            for (var i = 0; i < counts[cell]; i++)
            {
                var next = neighbours[cell * 4 + i];
                if (distances[next] < 0)
                {
                    distances[next] = distances[cell] + 1;
                    queue[tail++] = next;
                }
            }
        }

        return distances;
    }

    private static int[] Neighbours(Grid grid, out int[] counts)
    {
        var width = grid.Width;
        var height = grid.Height;
        var neighbours = new int[width * height * 4];
        counts = new int[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var cell = y * width + x;
                var count = 0;
                if (y > 0 && !IsBlocked(grid, x, y - 1))
                {
                    neighbours[cell * 4 + count++] = cell - width;
                }

                if (y < height - 1 && !IsBlocked(grid, x, y + 1))
                {
                    neighbours[cell * 4 + count++] = cell + width;
                }

                if (x > 0 && !IsBlocked(grid, x - 1, y))
                {
                    neighbours[cell * 4 + count++] = cell - 1;
                }

                if (x < width - 1 && !IsBlocked(grid, x + 1, y))
                {
                    neighbours[cell * 4 + count++] = cell + 1;
                }

                counts[cell] = count;
            }
        }

        return neighbours;
    }

    private static bool IsBlocked(Grid grid, int x, int y) => grid.Obstacles.ContainsKey(new Position(x, y));
}
