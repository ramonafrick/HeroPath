using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Shared.UI.Components;

public partial class LanguageSwitcher : LocalizedComponentBase
{
    [Inject]
    private IStringLocalizer<SharedResources> Localizer { get; set; } = default!;

    private string IsActive(string cultureName) =>
        (Language.Current == cultureName).ToString().ToLowerInvariant();

    private Task SelectAsync(string cultureName) => Language.SetAsync(cultureName, CancellationToken.None);
}
