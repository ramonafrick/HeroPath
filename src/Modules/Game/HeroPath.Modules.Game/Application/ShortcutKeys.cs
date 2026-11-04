using HeroPath.Modules.Game.Domain;
using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// Pure mapping from a pressed key (<c>KeyboardEvent.key</c> value) to a <see cref="ShortcutAction"/>. It knows no rules of the game:
/// whether the action has an effect (locked during a run, 200 blocks, no selection) is decided by <see cref="GameSession"/> and
/// <see cref="ExecutionRunner"/>. The table <see cref="Handled"/> is the single source of the handled keys: <see cref="Resolve"/> reads it
/// and the platform filters on it.
/// </summary>
public static class ShortcutKeys
{
    private static readonly (string Key, bool Shift, ShortcutAction Action)[] _bindings =
    [
        ("ArrowUp", false, ShortcutAction.InsertUp),
        ("ArrowUp", true, ShortcutAction.MoveSelectedUp),
        ("ArrowDown", false, ShortcutAction.InsertDown),
        ("ArrowDown", true, ShortcutAction.MoveSelectedDown),
        ("ArrowLeft", false, ShortcutAction.InsertLeft),
        ("ArrowRight", false, ShortcutAction.InsertRight),
        ("Delete", false, ShortcutAction.DeleteSelected),
        ("Delete", true, ShortcutAction.DeleteSelected),
        ("Backspace", false, ShortcutAction.DeleteSelected),
        ("Backspace", true, ShortcutAction.DeleteSelected),
        ("Escape", false, ShortcutAction.ClearSelection),
        ("Escape", true, ShortcutAction.ClearSelection),
        (" ", false, ShortcutAction.Play),
        (" ", true, ShortcutAction.Play),
        ("d", false, ShortcutAction.Debug),
        ("d", true, ShortcutAction.Debug),
        ("D", false, ShortcutAction.Debug),
        ("D", true, ShortcutAction.Debug),
        ("r", false, ShortcutAction.Reset),
        ("r", true, ShortcutAction.Reset),
        ("R", false, ShortcutAction.Reset),
        ("R", true, ShortcutAction.Reset),
    ];

    /// <summary>Every key and Shift state that has an action; everything else (also Shift+ArrowLeft/Right) is not handled.</summary>
    public static IReadOnlyList<KeyChord> Handled { get; } = [.. _bindings.Select(binding => new KeyChord(binding.Key, binding.Shift))];

    /// <summary>The action for <paramref name="key"/>, or <c>null</c> for keys without a shortcut. Letters ignore case.</summary>
    public static ShortcutAction? Resolve(string? key, bool shift)
    {
        foreach (var binding in _bindings)
        {
            if (binding.Key == key && binding.Shift == shift)
            {
                return binding.Action;
            }
        }

        return null;
    }

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
