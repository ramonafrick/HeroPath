using System.Collections.ObjectModel;

namespace HeroPath.Modules.Levels.Domain;

public sealed class Grid
{
    private readonly Dictionary<Position, ObstacleKind> _obstacles = [];
    private readonly Dictionary<Position, ItemKind> _items = [];

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

        foreach (var obstacle in obstacles)
        {
            var position = obstacle.GetPosition();
            if (!IsInside(position))
            {
                throw new ArgumentOutOfRangeException(nameof(obstacles), position, "Obstacle is outside the grid.");
            }

            if (position == start || position == goal)
            {
                throw new ArgumentException($"Obstacle at {position} blocks start or goal.", nameof(obstacles));
            }

            if (!_obstacles.TryAdd(position, obstacle.Kind))
            {
                throw new ArgumentException($"More than one obstacle at {position}.", nameof(obstacles));
            }
        }

        var itemList = new List<LevelItem>();
        foreach (var item in items)
        {
            var position = item.GetPosition();
            if (!IsInside(position))
            {
                throw new ArgumentOutOfRangeException(nameof(items), position, "Item is outside the grid.");
            }

            if (position == start)
            {
                throw new ArgumentException($"Item at {position} lies on the start.", nameof(items));
            }

            if (_obstacles.ContainsKey(position) || !_items.TryAdd(position, item.Kind))
            {
                throw new ArgumentException($"Cell {position} is already occupied.", nameof(items));
            }

            itemList.Add(item);
        }

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

    public IReadOnlyList<LevelItem> Items { get; }

    public IReadOnlyDictionary<Position, ObstacleKind> Obstacles => new ReadOnlyDictionary<Position, ObstacleKind>(_obstacles);

    public int RequiredItemCount { get; }

    public bool IsInside(Position position) =>
        position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;

    public ObstacleKind? GetObstacle(Position position) =>
        _obstacles.TryGetValue(position, out var kind) ? kind : null;

    public ItemKind? GetItem(Position position) =>
        _items.TryGetValue(position, out var kind) ? kind : null;
}
