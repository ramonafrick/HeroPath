using HeroPath.Shared.UI.Components;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Shared.UI.Layout;

/// <summary>App shell. It is a layout by convention: the router passes the page as <see cref="Body"/>.</summary>
public partial class MainLayout : LocalizedComponentBase
{
    [Parameter]
    public RenderFragment? Body { get; set; }

    [Inject]
    private IStringLocalizer<SharedResources> Localizer { get; set; } = default!;
}
