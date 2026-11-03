using HeroPath.Shared.UI.Localization;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Shared.UI.Components;

public partial class LanguageSwitcher : IDisposable
{
    [Inject]
    private LanguageService Language { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResources> Localizer { get; set; } = default!;

    protected override void OnInitialized() => Language.Changed += HandleChanged;

    public void Dispose() => Language.Changed -= HandleChanged;

    private string IsActive(string cultureName) =>
        (Language.Current == cultureName).ToString().ToLowerInvariant();

    private Task SelectAsync(string cultureName) => Language.SetAsync(cultureName, CancellationToken.None);

    private void HandleChanged() => _ = InvokeAsync(StateHasChanged);
}
