using HeroPath.Shared.Application.Contracts;
using Microsoft.JSInterop;

namespace HeroPath.Web.Services;

/// <summary>
/// <see cref="ISoundPlayer"/> on Web Audio: the JS module <c>js/sound.js</c> (loaded on first use, relative to the base href)
/// generates short tones with oscillators, no audio files. Unavailable interop or audio stays silent;
/// a cancellation requested by the caller is not swallowed.
/// </summary>
public sealed class BrowserSoundPlayer(IJSRuntime js) : ISoundPlayer, IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async ValueTask PlayAsync(SoundEffect effect, CancellationToken cancellationToken)
    {
        try
        {
            _module ??= await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./js/sound.js");
            await _module.InvokeVoidAsync("play", cancellationToken, effect.ToString().ToLowerInvariant());
        }
        catch (Exception ex) when (JsInterop.IsUnavailable(ex, cancellationToken))
        {
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
}
