using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Testing;

public sealed class FakeKeyboardShortcuts : IKeyboardShortcuts
{
    private Func<string, bool, Task>? _handler;

    public int SubscribeCount { get; private set; }

    public bool IsSubscribed => _handler is not null;

    public ValueTask<IAsyncDisposable> SubscribeAsync(Func<string, bool, Task> onKey, CancellationToken cancellationToken)
    {
        SubscribeCount++;
        _handler = onKey;
        return ValueTask.FromResult<IAsyncDisposable>(new Subscription(this, onKey));
    }

    /// <summary>Delivers a key press like the platform does; nothing happens without a subscription.</summary>
    public Task PressAsync(string key, bool shift = false) => _handler?.Invoke(key, shift) ?? Task.CompletedTask;

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
