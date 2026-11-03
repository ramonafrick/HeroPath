using HeroPath.Shared.Application.Contracts;
using Microsoft.JSInterop;

namespace HeroPath.Web.Services;

/// <summary>
/// <see cref="IAppStorage"/> on top of the browser localStorage. Blocked or full storage degrades silently:
/// reads return <c>null</c>, writes are dropped, so the game stays playable.
/// </summary>
public sealed class LocalStorageAppStorage(IJSRuntime js) : IAppStorage
{
    public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            return null;
        }
    }

    public async ValueTask SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", cancellationToken, key, value);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            // Storage blocked or full: the setting is simply not remembered.
        }
    }

    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.removeItem", cancellationToken, key);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            // Nothing to remove if storage is unavailable.
        }
    }
}
