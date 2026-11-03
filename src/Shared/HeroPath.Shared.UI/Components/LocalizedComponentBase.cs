using HeroPath.Shared.UI.Localization;
using Microsoft.AspNetCore.Components;

namespace HeroPath.Shared.UI.Components;

/// <summary>
/// Base of every component that shows localized text: subscribes to <see cref="LanguageService.Changed"/> once in
/// <see cref="OnInitialized"/>, re-renders on a language switch and unsubscribes in <see cref="Dispose()"/>.
/// Derived classes override <see cref="OnInitialized"/> and call <c>base.OnInitialized()</c>.
/// </summary>
public abstract class LocalizedComponentBase : ComponentBase, IDisposable
{
    private bool _disposed;

    [Inject]
    protected LanguageService Language { get; set; } = default!;

    protected override void OnInitialized() => Language.Changed += HandleChanged;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Language.Changed -= HandleChanged;
        }
    }

    /// <summary>Service events arrive outside the render context, so they re-render through the dispatcher.</summary>
    protected void HandleChanged() => _ = InvokeAsync(StateHasChanged);
}
