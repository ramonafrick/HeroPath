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

    protected override void OnInitialized() => Language.Changed += LanguageChanged;

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
            Language.Changed -= LanguageChanged;
        }
    }

    /// <summary>Called on a language switch before the component re-renders. Override to adapt state that depends on the language.</summary>
    protected virtual void OnLanguageChanged()
    {
    }

    private void LanguageChanged()
    {
        OnLanguageChanged();
        HandleChanged();
    }

    /// <summary>Service events arrive outside the render context, so they re-render through the dispatcher.</summary>
    protected void HandleChanged() => _ = InvokeAsync(StateHasChanged);
}
