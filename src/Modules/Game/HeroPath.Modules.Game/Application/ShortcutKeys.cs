using HeroPath.Modules.Game.Domain;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// Pure mapping from a pressed key (<c>KeyboardEvent.key</c> value) to a <see cref="ShortcutAction"/>. It knows no rules of the game:
/// whether the action has an effect (locked during a run, 200 blocks, no selection) is decided by <see cref="GameSession"/> and
/// <see cref="ExecutionRunner"/>.
/// </summary>
public static class ShortcutKeys
{
    /// <summary>The action for <paramref name="key"/>, or <c>null</c> for keys without a shortcut. Letters ignore case.</summary>
    public static ShortcutAction? Resolve(string? key, bool shift) => key switch
    {
        "ArrowUp" => shift ? ShortcutAction.MoveSelectedUp : ShortcutAction.InsertUp,
        "ArrowDown" => shift ? ShortcutAction.MoveSelectedDown : ShortcutAction.InsertDown,
        "ArrowLeft" => shift ? null : ShortcutAction.InsertLeft,
        "ArrowRight" => shift ? null : ShortcutAction.InsertRight,
        "Delete" or "Backspace" => ShortcutAction.DeleteSelected,
        "Escape" => ShortcutAction.ClearSelection,
        " " => ShortcutAction.Play,
        "d" or "D" => ShortcutAction.Debug,
        "r" or "R" => ShortcutAction.Reset,
        _ => null,
    };

    /// <summary>The direction of the block an insert action adds, or <c>null</c> for the other actions.</summary>
    public static Direction? InsertDirection(ShortcutAction action) => action switch
    {
        ShortcutAction.InsertUp => Direction.Up,
        ShortcutAction.InsertDown => Direction.Down,
        ShortcutAction.InsertLeft => Direction.Left,
        ShortcutAction.InsertRight => Direction.Right,
        _ => null,
    };
}
