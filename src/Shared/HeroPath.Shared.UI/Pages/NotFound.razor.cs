using Microsoft.AspNetCore.Components;

namespace HeroPath.Shared.UI.Pages;

/// <summary>Unknown addresses lead back to the game instead of showing an error page.</summary>
public partial class NotFound
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized() => Navigation.NavigateTo("", replace: true);
}
