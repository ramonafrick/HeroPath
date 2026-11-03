using HeroPath.Shared.UI.Components;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Shared.UI.Graphics;

/// <summary>Base of the inline SVG graphics: localized title (<c>Graphic_&lt;TypeName&gt;</c>) and a per-instance id for patterns.</summary>
public abstract class GraphicBase : LocalizedComponentBase
{
    [Inject]
    private IStringLocalizer<SharedResources> Localizer { get; set; } = default!;

    protected string InstanceId { get; } = Guid.NewGuid().ToString("N");

    protected string Title => Localizer[$"Graphic_{GetType().Name}"];
}
