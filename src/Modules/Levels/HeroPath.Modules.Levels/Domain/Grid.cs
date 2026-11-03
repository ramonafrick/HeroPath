using System.Collections.Frozen;

namespace HeroPath.Modules.Levels.Domain;

/// <summary>
/// Immutable playing field with start, goal, obstacles and items. The constructor throws on rule
/// violations as a defensive net; <c>LevelValidator</c> reports the same rules as values.
/// </summary>
public sealed class Grid
{
    private readonly FrozenDictionary<Position, ItemKind> _items;

    public Grid(
        int width,
        int height,
        Position start,
        Position goal,
        IEnumerable<LevelObstacle> obstacles,
        IEnumerable<LevelItem> items,
        bool requireAllItems)
    {
        ArgumentNullException.ThrowIfNull(obstacles);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, LevelLimits.MinGridSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, LevelLimits.MaxGridSize);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, LevelLimits.MinGridSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, LevelLimits.MaxGridSize);

        Width = width;
        Height = height;
        Start = start;
        Goal = goal;
        RequireAllItems = requireAllItems;

        if (!IsInside(start))
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Start is outside the grid.");
        }

        if (!IsInside(goal))
        {
            throw new ArgumentOutOfRangeException(nameof(goal), goal, "Goal is outside the grid.");
        }

        if (start == goal)
        {
            throw new ArgumentException("Start and goal must differ.", nameof(goal));
        }

        var occupied = new HashSet<Position>();
        var obstacleCells = new Dictionary<Position, ObstacleKind>();
        foreach (var obstacle in obstacles)
        {
            var position = obstacle.GetPosition();
            Place(position, "Obstacle", nameof(obstacles), occupied, blocksGoal: true);
            obstacleCells.Add(position, obstacle.Kind);
        }

        var itemList = new List<LevelItem>();
        var itemCells = new Dictionary<Position, ItemKind>();
        foreach (var item in items)
        {
            var position = item.GetPosition();
            Place(position, "Item", nameof(items), occupied, blocksGoal: false);
            itemCells.Add(position, item.Kind);
            itemList.Add(item);
        }

        Obstacles = obstacleCells.ToFrozenDictionary();
        _items = itemCells.ToFrozenDictionary();
        Items = itemList.AsReadOnly();
        RequiredItemCount = itemList.Count(i => i.Kind == ItemKind.Strawberry);

        if (requireAllItems && RequiredItemCount == 0)
        {
            throw new ArgumentException("RequireAllItems needs at least one strawberry.", nameof(requireAllItems));
        }
    }

    public int Width { get; }

    public int Height { get; }

    public Position Start { get; }

    public Position Goal { get; }

    public bool RequireAllItems { get; }

    /// <summary>Items in definition order.</summary>
    public IReadOnlyList<LevelItem> Items { get; }

    public IReadOnlyDictionary<Position, ObstacleKind> Obstacles { get; }

    /// <summary>Number of strawberries; pizzas are optional and not counted.</summary>
    public int RequiredItemCount { get; }

    public bool IsInside(Position position) => position.IsInside(Width, Height);

    public ObstacleKind? GetObstacle(Position position) =>
        Obstacles.TryGetValue(position, out var kind) ? kind : null;

    public ItemKind? GetItem(Position position) =>
        _items.TryGetValue(position, out var kind) ? kind : null;

    private void Place(Position position, string what, string parameterName, HashSet<Position> occupied, bool blocksGoal)
    {
        if (!IsInside(position))
        {
            throw new ArgumentOutOfRangeException(parameterName, position, $"{what} is outside the grid.");
        }

        if (position == Start || (blocksGoal && position == Goal))
        {
            throw new ArgumentException($"{what} at {position} blocks start or goal.", parameterName);
        }

        if (!occupied.Add(position))
        {
            throw new ArgumentException($"Cell {position} is already occupied.", parameterName);
        }
    }
}
