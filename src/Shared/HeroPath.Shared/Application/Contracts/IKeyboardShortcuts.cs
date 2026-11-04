namespace HeroPath.Shared.Application.Contracts;

/// <summary>
/// Delivers key presses that may trigger a shortcut (Web: a <c>keydown</c> listener on the document, MAUI later: native key events).
/// The platform decides which presses count: only the keys the caller handles, not while text is being typed, not on repeats, not with
/// Ctrl, Alt or Meta, and not for keys that belong to a focused control. Implementations never throw because the platform lacks the
/// feature: the subscription is then simply silent. Only a cancellation requested through the caller's token is propagated.
/// </summary>
public interface IKeyboardShortcuts
{
    /// <summary>
    /// Starts delivering presses of <paramref name="handled"/> to <paramref name="onKey"/> (the <c>KeyboardEvent.key</c> value and whether
    /// Shift was held). The caller owns the table, so the platform has no second copy of it. A new subscription replaces the previous one.
    /// Disposing the result stops delivery. A cancellation that ends the call leaves nothing subscribed.
    /// </summary>
    ValueTask<IAsyncDisposable> SubscribeAsync(IReadOnlyCollection<KeyChord> handled, Func<string, bool, Task> onKey, CancellationToken cancellationToken);
}
