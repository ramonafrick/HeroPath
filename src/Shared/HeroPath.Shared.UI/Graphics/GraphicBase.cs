using HeroPath.Shared.UI.Localization;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Shared.UI.Graphics;

/// <summary>Base of the inline SVG graphics: localized title (<c>Graphic_&lt;TypeName&gt;</c>) and a per-instance id for patterns.</summary>
public abstract class GraphicBase : ComponentBase, IDisposable
{
    [Inject]
    private LanguageService Language { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResources> Localizer { get; set; } = default!;

    protected string InstanceId { get; } = Guid.NewGuid().ToString("N");

    protected string Title => Localizer[$"Graphic_{GetType().Name}"];

    protected override void OnInitialized() => Language.Changed += HandleChanged;

    public void Dispose() => Language.Changed -= HandleChanged;

    private void HandleChanged() => _ = InvokeAsync(StateHasChanged);
}
