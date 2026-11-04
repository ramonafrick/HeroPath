using HeroPath.Shared.Application.Contracts;
using Microsoft.JSInterop;

namespace HeroPath.Web.Services;

/// <summary>
/// <see cref="IKeyboardShortcuts"/> on a <c>keydown</c> listener on the document: the JS module <c>js/keyboard.js</c> (loaded on first use,
/// relative to the base href) filters synchronously, because only there can <c>preventDefault</c> be decided per key. Unavailable interop
/// gives a silent subscription; a cancellation requested by the caller is not swallowed.
/// </summary>
public sealed class BrowserKeyboardShortcuts(IJSRuntime js) : IKeyboardShortcuts, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private Subscription? _current;

    public async ValueTask<IAsyncDisposable> SubscribeAsync(Func<string, bool, Task> onKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onKey);
        try
        {
            _module ??= await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./js/keyboard.js");
            var subscription = new Subscription(this, _module, onKey);
            var previous = _current;
            _current = subscription;
            previous?.Release();
            await _module.InvokeVoidAsync("start", cancellationToken, subscription.Reference);
            return subscription;
        }
        catch (Exception ex) when (IsUnavailable(ex, cancellationToken))
        {
            return new Silent();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _current?.Release();
        _current = null;
        if (_module is not { } module)
        {
            return;
        }

        _module = null;
        try
        {
            await module.InvokeVoidAsync("stop");
            await module.DisposeAsync();
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
        }
    }

    private bool Detach(Subscription subscription)
    {
        if (_current != subscription)
        {
            return false;
        }

        _current = null;
        return true;
    }

    private static bool IsUnavailable(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        JSException or JSDisconnectedException or InvalidOperationException => true,
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false,
    };

    private sealed class Callback(Func<string, bool, Task> onKey)
    {
        [JSInvokable]
        public Task OnKey(string key, bool shift) => onKey(key, shift);
    }

    private sealed class Subscription : IAsyncDisposable
    {
        private readonly BrowserKeyboardShortcuts _owner;
        private readonly IJSObjectReference _module;

        public Subscription(BrowserKeyboardShortcuts owner, IJSObjectReference module, Func<string, bool, Task> onKey)
        {
            _owner = owner;
            _module = module;
            Reference = DotNetObjectReference.Create(new Callback(onKey));
        }

        public DotNetObjectReference<Callback> Reference { get; }

        public void Release() => Reference.Dispose();

        public async ValueTask DisposeAsync()
        {
            if (!_owner.Detach(this))
            {
                return;
            }

            try
            {
                await _module.InvokeVoidAsync("stop");
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
            {
            }
            finally
            {
                Reference.Dispose();
            }
        }
    }

    private sealed class Silent : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
