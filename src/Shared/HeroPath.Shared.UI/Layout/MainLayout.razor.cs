using HeroPath.Shared.UI.Localization;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Shared.UI.Layout;

public partial class MainLayout : IDisposable
{
    [Inject]
    private LanguageService Language { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResources> Localizer { get; set; } = default!;

    protected override void OnInitialized() => Language.Changed += HandleChanged;

    public void Dispose() => Language.Changed -= HandleChanged;

    private void HandleChanged() => _ = InvokeAsync(StateHasChanged);
}
