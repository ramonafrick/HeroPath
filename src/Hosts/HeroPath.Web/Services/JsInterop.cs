using Microsoft.JSInterop;

namespace HeroPath.Web.Services;

internal static class JsInterop
{
    /// <summary>
    /// Whether an exception of a JS interop call means the platform is not available (JS error, circuit or runtime gone, interop timeout),
    /// so the caller degrades silently. A cancellation requested by the caller's own token is not "unavailable" and must propagate.
    /// </summary>
    public static bool IsUnavailable(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        JSException or JSDisconnectedException or InvalidOperationException => true,
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false,
    };
}
