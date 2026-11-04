using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Testing;

public sealed class FakeKeyboardShortcuts : IKeyboardShortcuts
{
    private Func<string, bool, Task>? _handler;

    public int SubscribeCount { get; private set; }

    public bool IsSubscribed => _handler is not null;

    /// <summary>The table of handled keys passed to the last <see cref="SubscribeAsync"/>.</summary>
    public IReadOnlyCollection<KeyChord> Handled { get; private set; } = [];

    public ValueTask<IAsyncDisposable> SubscribeAsync(IReadOnlyCollection<KeyChord> handled, Func<string, bool, Task> onKey, CancellationToken cancellationToken)
    {
        SubscribeCount++;
        Handled = handled;
        _handler = onKey;
        return ValueTask.FromResult<IAsyncDisposable>(new Subscription(this, onKey));
    }

    /// <summary>Delivers a key press like the platform does (keys outside <see cref="Handled"/> are filtered out); nothing happens without a subscription.</summary>
    public Task PressAsync(string key, bool shift = false) =>
        _handler is not null && Handled.Contains(new KeyChord(key, shift)) ? _handler(key, shift) : Task.CompletedTask;

    private sealed class Subscription(FakeKeyboardShortcuts owner, Func<string, bool, Task> handler) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            if (owner._handler == handler)
            {
                owner._handler = null;
            }

            return ValueTask.CompletedTask;
        }
    }
}
