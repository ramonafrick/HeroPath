using HeroPath.Shared.Application;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Shared.UI.Components;

public partial class SoundToggle : LocalizedComponentBase
{
    [Inject]
    private IStringLocalizer<SharedResources> Localizer { get; set; } = default!;

    [Inject]
    private SoundSettings Settings { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Settings.Changed += HandleChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Settings.Changed -= HandleChanged;
        }

        base.Dispose(disposing);
    }

    private Task ToggleAsync() => Settings.SetAsync(!Settings.Enabled, CancellationToken.None);
}
