using System.Globalization;
using System.Text;
using HeroPath.Shared.Application;

namespace HeroPath.Modules.Levels.Domain;

/// <summary>
/// Pure editing rules of the level editor on the immutable <see cref="LevelDefinition"/>. Every operation returns a new
/// level (or the same instance when nothing changed) and never throws for user-reachable input.
/// </summary>
public static class LevelEditing
{
    /// <summary>The ending of exported level files.</summary>
    public const string ExportExtension = ".heropath.json";

    /// <summary>Longest base name of an exported file, without the extension.</summary>
    public const int MaxExportBaseLength = 40;

    private static readonly (int X, int Y)[] _offsets = [(0, -1), (0, 1), (-1, 0), (1, 0)];

    /// <summary>An empty 10x10 level with the start bottom-left and the goal top-right.</summary>
    public static LevelDefinition NewLevel(string id, string name) => new()
    {
        Id = id,
        Name = name,
        Width = LevelLimits.DefaultGridSize,
        Height = LevelLimits.DefaultGridSize,
        Start = new(0, LevelLimits.DefaultGridSize - 1),
        Goal = new(LevelLimits.DefaultGridSize - 1, 0),
    };

    /// <summary>A copy of <paramref name="template"/> with a new id and name; the template stays untouched.</summary>
    public static LevelDefinition FromTemplate(LevelDefinition template, string id, string name)
    {
        ArgumentNullException.ThrowIfNull(template);
        return template with { Id = id, Name = name };
    }

    /// <summary>Applies <paramref name="tool"/> to the cell at <paramref name="position"/>.</summary>
    public static EditResult Apply(LevelDefinition level, EditorTool tool, Position position)
    {
        ArgumentNullException.ThrowIfNull(level);
        if (!position.IsInside(level.Width, level.Height))
        {
            return new(level);
        }

        var onStart = position == level.Start;
        var onGoal = position == level.Goal;
        var obstacle = ObstacleAt(level, position);
        var item = ItemAt(level, position);

        switch (tool)
        {
            case EditorTool.Start:
                if (onGoal)
                {
                    return new(level, EditHint.StartOnGoal);
                }

                return onStart ? new(level) : Done(Cleared(level, position) with { Start = position });

            case EditorTool.Goal:
                if (onStart)
                {
                    return new(level, EditHint.GoalOnStart);
                }

                return onGoal ? new(level) : Done(Cleared(level, position) with { Goal = position });

            case EditorTool.Tree or EditorTool.Rock or EditorTool.Pond:
                if (onStart)
                {
                    return new(level, EditHint.ObstacleOnStart);
                }

                if (onGoal)
                {
                    return new(level, EditHint.ObstacleOnGoal);
                }

                var kind = tool switch
                {
                    EditorTool.Tree => ObstacleKind.Tree,
                    EditorTool.Rock => ObstacleKind.Rock,
                    _ => ObstacleKind.Pond,
                };
                var withoutObject = Cleared(level, position);
                return Done(obstacle == kind
                    ? withoutObject
                    : withoutObject with { Obstacles = [.. withoutObject.Obstacles, new LevelObstacle(position.X, position.Y, kind)] });

            case EditorTool.Strawberry or EditorTool.Pizza:
                if (onStart)
                {
                    return new(level, EditHint.ItemOnStart);
                }

                var itemKind = tool == EditorTool.Strawberry ? ItemKind.Strawberry : ItemKind.Pizza;
                if (item == itemKind)
                {
                    return Done(Cleared(level, position));
                }

                var count = level.Items.Count(i => i.Kind == itemKind);
                if (count >= (itemKind == ItemKind.Strawberry ? LevelLimits.MaxStrawberries : LevelLimits.MaxPizzas))
                {
                    return new(level, itemKind == ItemKind.Strawberry ? EditHint.TooManyStrawberries : EditHint.TooManyPizzas);
                }

                var cleared = Cleared(level, position);
                return Done(cleared with { Items = [.. cleared.Items, new LevelItem(position.X, position.Y, itemKind)] });

            case EditorTool.Eraser:
                return obstacle is null && item is null ? new(level) : Done(Cleared(level, position));

            default:
                throw new ArgumentOutOfRangeException(nameof(tool));
        }
    }

    /// <summary>Switches the "collect all strawberries" rule; turning it on needs at least one strawberry, otherwise the level is returned unchanged.</summary>
    public static LevelDefinition SetRequireAllItems(LevelDefinition level, bool value)
    {
        ArgumentNullException.ThrowIfNull(level);
        if (value && !HasStrawberry(level))
        {
            return level;
        }

        return level.RequireAllItems == value ? level : level with { RequireAllItems = value };
    }

    /// <summary>
    /// Changes the size (clamped to 3..20). Objects outside are removed and counted, start and goal move to the nearest
    /// border cell, a goal that collides with the start or an obstacle moves to the nearest free cell, and an object
    /// under the start is removed and counted.
    /// </summary>
    public static ResizeResult Resize(LevelDefinition level, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(level);
        width = Math.Clamp(width, LevelLimits.MinGridSize, LevelLimits.MaxGridSize);
        height = Math.Clamp(height, LevelLimits.MinGridSize, LevelLimits.MaxGridSize);

        var obstacles = level.Obstacles.Where(o => o.GetPosition().IsInside(width, height)).ToList();
        var items = level.Items.Where(i => i.GetPosition().IsInside(width, height)).ToList();
        var removed = level.Obstacles.Count - obstacles.Count + level.Items.Count - items.Count;

        var start = Clamp(level.Start, width, height);
        var goal = Clamp(level.Goal, width, height);
        removed += obstacles.RemoveAll(o => o.GetPosition() == start);
        removed += items.RemoveAll(i => i.GetPosition() == start);

        if (goal == start || obstacles.Any(o => o.GetPosition() == goal))
        {
            var candidates = NearestCells(goal, width, height).Where(cell => cell != start).ToList();
            var free = candidates.Where(cell => obstacles.All(o => o.GetPosition() != cell)).Cast<Position?>().FirstOrDefault();
            if (free is { } target)
            {
                goal = target;
            }
            else
            {
                goal = candidates[0];
                removed += obstacles.RemoveAll(o => o.GetPosition() == goal);
            }
        }

        var resized = level with { Width = width, Height = height, Start = start, Goal = goal, Obstacles = obstacles, Items = items };
        return new(Normalized(resized), removed);
    }

    /// <summary>Removes all obstacles and items and switches the strawberry rule off.</summary>
    public static LevelDefinition ClearObjects(LevelDefinition level)
    {
        ArgumentNullException.ThrowIfNull(level);
        return level with { Obstacles = [], Items = [], RequireAllItems = false };
    }

    /// <summary>The smallest N of 1 or more whose <paramref name="format"/>ted text is not among <paramref name="existingNames"/> (ordinal comparison).</summary>
    public static int NextDefaultNumber(IEnumerable<string?> existingNames, Func<int, string> format)
    {
        ArgumentNullException.ThrowIfNull(existingNames);
        ArgumentNullException.ThrowIfNull(format);
        var names = existingNames.ToHashSet(StringComparer.Ordinal);
        var number = 1;
        while (names.Contains(format(number)))
        {
            number++;
        }

        return number;
    }

    /// <summary>
    /// Cleans a typed level name (see <see cref="TextCleaner"/>) and cuts it to <see cref="LevelLimits.MaxNameLength"/>
    /// UTF-16 characters without splitting a surrogate pair, so the validator always accepts a non-empty result.
    /// </summary>
    public static string CleanName(string? raw)
    {
        var cleaned = TextCleaner.Clean(raw, LevelLimits.MaxNameLength);
        if (cleaned.Length > LevelLimits.MaxNameLength)
        {
            var length = char.IsHighSurrogate(cleaned[LevelLimits.MaxNameLength - 1]) ? LevelLimits.MaxNameLength - 1 : LevelLimits.MaxNameLength;
            cleaned = cleaned[..length].Trim();
        }

        return cleaned;
    }

    /// <summary>
    /// A safe file name for exporting a level: lowercase ASCII letters and digits separated by single hyphens (umlauts become ae, oe, ue,
    /// «ß» becomes ss, other accents are dropped), at most <see cref="MaxExportBaseLength"/> characters, «level» when nothing is left,
    /// plus <see cref="ExportExtension"/>. Never contains a path separator or markup.
    /// </summary>
    public static string ExportFileName(string? name)
    {
        var builder = new StringBuilder();
        var pendingHyphen = false;
        var text = string.Concat((name ?? string.Empty).EnumerateRunes()).ToLowerInvariant().Normalize(NormalizationForm.FormC);
        foreach (var rune in text.EnumerateRunes())
        {
            var mapped = rune.Value switch { 'ä' => "ae", 'ö' => "oe", 'ü' => "ue", 'ß' => "ss", _ => rune.ToString() };
            foreach (var letter in mapped.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(letter) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (letter is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                {
                    if (pendingHyphen && builder.Length > 0)
                    {
                        builder.Append('-');
                    }

                    pendingHyphen = false;
                    builder.Append(letter);
                }
                else
                {
                    pendingHyphen = true;
                }
            }
        }

        var baseName = builder.Length > MaxExportBaseLength ? builder.ToString(0, MaxExportBaseLength).TrimEnd('-') : builder.ToString();
        return (baseName.Length == 0 ? "level" : baseName) + ExportExtension;
    }

    private static EditResult Done(LevelDefinition level) => new(Normalized(level));

    private static LevelDefinition Normalized(LevelDefinition level) =>
        level.RequireAllItems && !HasStrawberry(level) ? level with { RequireAllItems = false } : level;

    private static bool HasStrawberry(LevelDefinition level) => level.Items.Any(i => i.Kind == ItemKind.Strawberry);

    private static LevelDefinition Cleared(LevelDefinition level, Position position) => level with
    {
        Obstacles = [.. level.Obstacles.Where(o => o.GetPosition() != position)],
        Items = [.. level.Items.Where(i => i.GetPosition() != position)],
    };

    private static ObstacleKind? ObstacleAt(LevelDefinition level, Position position) =>
        level.Obstacles.FirstOrDefault(o => o.GetPosition() == position)?.Kind;

    private static ItemKind? ItemAt(LevelDefinition level, Position position) =>
        level.Items.FirstOrDefault(i => i.GetPosition() == position)?.Kind;

    private static Position Clamp(Position position, int width, int height) =>
        new(Math.Clamp(position.X, 0, width - 1), Math.Clamp(position.Y, 0, height - 1));

    private static IEnumerable<Position> NearestCells(Position origin, int width, int height)
    {
        var seen = new HashSet<Position> { origin };
        var queue = new Queue<Position>([origin]);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;
            foreach (var (dx, dy) in _offsets)
            {
                var next = new Position(current.X + dx, current.Y + dy);
                if (next.IsInside(width, height) && seen.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }
    }
}
