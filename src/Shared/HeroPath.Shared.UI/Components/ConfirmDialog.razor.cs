using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;

namespace HeroPath.Shared.UI.Components;

/// <summary>
/// A modal question with two buttons (own component, no <c>window.confirm</c>). Renders only while <see cref="Visible"/>; the parent owns
/// that flag and closes the dialog in its callbacks. When it opens, focus moves to the first button; Escape cancels; two focusable
/// sentinels around the dialog keep Tab inside it. The texts are plain strings, so user text such as a level name stays text.
/// </summary>
public partial class ConfirmDialog : LocalizedComponentBase
{
    private static int _nextId;

    private readonly string _titleId = $"confirm-title-{Interlocked.Increment(ref _nextId)}";
    private readonly string _messageId = $"confirm-message-{_nextId}";
    private ElementReference _firstButton;
    private ElementReference _lastButton;
    private bool _wasVisible;

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Message { get; set; }

    /// <summary>Text of the confirm button; «Ja» / «Yes» when <c>null</c>.</summary>
    [Parameter]
    public string? ConfirmLabel { get; set; }

    /// <summary>Text of the cancel button; «Abbrechen» / «Cancel» when <c>null</c>.</summary>
    [Parameter]
    public string? CancelLabel { get; set; }

    [Parameter]
    public EventCallback OnConfirm { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject]
    private IStringLocalizer<SharedResources> Localizer { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var opened = Visible && !_wasVisible;
        _wasVisible = Visible;
        if (opened)
        {
            await _firstButton.FocusAsync();
        }
    }

    private Task Confirm() => OnConfirm.InvokeAsync();

    private Task Cancel() => OnCancel.InvokeAsync();

    private Task HandleKeyDown(KeyboardEventArgs args) => args.Key == "Escape" ? Cancel() : Task.CompletedTask;

    private async Task FocusFirst() => await _firstButton.FocusAsync();

    private async Task FocusLast() => await _lastButton.FocusAsync();
}
