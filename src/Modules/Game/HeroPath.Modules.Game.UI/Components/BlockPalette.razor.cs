using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Shared.UI.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Components;

public partial class BlockPalette : IDisposable
{
    private static readonly Direction[] _directions = [Direction.Up, Direction.Down, Direction.Left, Direction.Right];

    [Inject]
    private GameSession Session { get; set; } = default!;

    [Inject]
    private LanguageService Language { get; set; } = default!;

    [Inject]
    private IStringLocalizer<GameResources> Localizer { get; set; } = default!;

    private bool CanAdd => Session.CanAdd && Session.IsEditable;

    protected override void OnInitialized()
    {
        Session.Changed += HandleChanged;
        Language.Changed += HandleChanged;
    }

    public void Dispose()
    {
        Session.Changed -= HandleChanged;
        Language.Changed -= HandleChanged;
    }

    private void HandleChanged() => _ = InvokeAsync(StateHasChanged);
}
