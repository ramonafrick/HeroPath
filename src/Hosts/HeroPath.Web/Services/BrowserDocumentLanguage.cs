using HeroPath.Shared.Application.Contracts;
using Microsoft.JSInterop;

namespace HeroPath.Web.Services;

/// <summary>
/// <see cref="IDocumentLanguage"/> on the <c>lang</c> attribute of the html element. Unavailable interop stays silent;
/// a cancellation requested by the caller is not swallowed.
/// </summary>
public sealed class BrowserDocumentLanguage(IJSRuntime js) : IDocumentLanguage
{
    public async ValueTask SetAsync(string cultureName, CancellationToken cancellationToken)
    {
        try
        {
            await js.InvokeVoidAsync("document.documentElement.setAttribute", cancellationToken, "lang", cultureName);
        }
        catch (Exception ex) when (IsUnavailable(ex, cancellationToken))
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
