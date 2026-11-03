using HeroPath.Shared.Application.Contracts;
using Microsoft.JSInterop;

namespace HeroPath.Web.Services;

/// <summary>
/// <see cref="IAppStorage"/> on top of the browser localStorage. Unavailable storage (blocked, full, circuit gone,
/// interop timeout) degrades silently: reads return <c>null</c>, writes return <c>false</c>, so the game stays playable.
/// A cancellation requested by the caller is not swallowed.
/// </summary>
public sealed class LocalStorageAppStorage(IJSRuntime js) : IAppStorage
{
    public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken) =>
        TryAsync(runtime => runtime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key), cancellationToken);

    public async ValueTask<bool> SetAsync(string key, string value, CancellationToken cancellationToken) =>
        await TryAsync(
            async runtime =>
            {
                await runtime.InvokeVoidAsync("localStorage.setItem", cancellationToken, key, value);
                return true;
            },
            cancellationToken);

    public async ValueTask<bool> RemoveAsync(string key, CancellationToken cancellationToken) =>
        await TryAsync(
            async runtime =>
            {
                await runtime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, key);
                return true;
            },
            cancellationToken);

    private async ValueTask<T?> TryAsync<T>(Func<IJSRuntime, ValueTask<T>> operation, CancellationToken cancellationToken)
    {
        try
        {
            return await operation(js);
        }
        catch (Exception ex) when (IsStorageUnavailable(ex, cancellationToken))
        {
            return default;
        }
    }

    private static bool IsStorageUnavailable(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        JSException or JSDisconnectedException or InvalidOperationException => true,
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false,
    };
}
