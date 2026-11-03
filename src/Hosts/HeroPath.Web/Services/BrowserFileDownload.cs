using System.Text;
using HeroPath.Shared.Application.Contracts;
using Microsoft.JSInterop;

namespace HeroPath.Web.Services;

/// <summary>
/// <see cref="IFileDownload"/> through a browser download: the JS module <c>js/file-download.js</c> (loaded on first use, relative to
/// the base href) turns the streamed UTF-8 text into a blob and clicks a temporary link. Unavailable interop reports <c>false</c>;
/// a cancellation requested by the caller is not swallowed.
/// </summary>
public sealed class BrowserFileDownload(IJSRuntime js) : IFileDownload, IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async ValueTask<bool> SaveTextAsync(string fileName, string content, CancellationToken cancellationToken)
    {
        try
        {
            _module ??= await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./js/file-download.js");
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var reference = new DotNetStreamReference(stream);
            await _module.InvokeVoidAsync("saveStream", cancellationToken, fileName, reference);
            return true;
        }
        catch (Exception ex) when (IsUnavailable(ex, cancellationToken))
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not { } module)
        {
            return;
        }

        _module = null;
        try
        {
            await module.DisposeAsync();
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
        }
    }

    private static bool IsUnavailable(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        JSException or JSDisconnectedException or InvalidOperationException => true,
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false,
    };
}
